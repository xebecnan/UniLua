
// #define DEBUG_D_PRE_CALL
// #define DEBUG_D_POS_CALL

namespace UniLua
{
	using Debug = UniLua.Tools.Debug;
	using InstructionPtr = Pointer<Instruction>;
	using Exception = System.Exception;

	public class LuaRuntimeException : Exception
	{
		public ThreadStatus ErrCode { get; private set; }

		public LuaRuntimeException( ThreadStatus errCode )
		{
			ErrCode = errCode;
		}
	}

	public partial class LuaState
	{
		internal void D_Throw( ThreadStatus errCode )
		{
			throw new LuaRuntimeException( errCode );
		}

		private ThreadStatus D_RawRunProtected( PFuncDelegate func, object ud )
		{
			int oldNumCSharpCalls = NumCSharpCalls;
			ThreadStatus res = ThreadStatus.LUA_OK;
			try
			{
				func( ud );
			}
			catch( LuaRuntimeException e )
			{
				NumCSharpCalls = oldNumCSharpCalls;
				res = e.ErrCode;
			}
			NumCSharpCalls = oldNumCSharpCalls;
			return res;
		}

		private void SetErrorObj( ThreadStatus errCode, StkId oldTop )
		{
			switch( errCode )
			{
				case ThreadStatus.LUA_ERRMEM: // memory error?
					oldTop.Value = new LuaString( "not enough memory" );
					break;

				case ThreadStatus.LUA_ERRERR:
					oldTop.Value = new LuaString( "error in error handling" );
					break;

				default:
					oldTop.Value = (Top-1).Value; // error message on current top
					break;
			}
			Top = oldTop + 1;
		}

		private ThreadStatus D_PCall( PFuncDelegate func, object ud,
			StkId oldTop, int errFunc )
		{
			CallInfo oldCI 			= CI;
			bool oldAllowHook 		= AllowHook;
			int oldNumNonYieldable	= NumNonYieldable;
			int oldErrFunc			= ErrFunc;

			ErrFunc = errFunc;
			ThreadStatus status = D_RawRunProtected( func, ud );
			if( status != ThreadStatus.LUA_OK ) // an error occurred?
			{
				F_Close( oldTop );
				SetErrorObj( status, oldTop );
				CI = oldCI;
				AllowHook = oldAllowHook;
				NumNonYieldable = oldNumNonYieldable;
			}
			ErrFunc = oldErrFunc;
			return status;
		}

		private void D_Call( StkId func, int nResults, bool allowYield )
		{
			if( ++NumCSharpCalls >= LuaLimits.LUAI_MAXCCALLS )
			{
				if( NumCSharpCalls == LuaLimits.LUAI_MAXCCALLS )
					G_RunError( "CSharp Stack Overflow" );
				else if( NumCSharpCalls >=
						(LuaLimits.LUAI_MAXCCALLS + (LuaLimits.LUAI_MAXCCALLS>>3))
					)
					D_Throw( ThreadStatus.LUA_ERRERR );
			}
			if( !allowYield )
				NumNonYieldable++;
			if( !D_PreCall( func, nResults ) ) // is a Lua function ?
				V_Execute();
			if( !allowYield )
				NumNonYieldable--;
			NumCSharpCalls--;
		}

		/// <summary>
		/// return true if function has been executed
		/// </summary>
		private bool D_PreCall( StkId func, int nResults )
		{
			// prepare for Lua call

#if DEBUG_D_PRE_CALL
			Debug.Log( "============================ D_PreCall func:" + func );
#endif

			var cl = func.Value as LuaLClosure;
			if( cl != null )
			{
				var p = cl.Proto;

				// 补全参数
				int n = (Top.Index - func.Index) - 1;
				for( ; n<p.NumParams; ++n )
				{
					Top.Value = new LuaNil();
					++Top.Index;
				}

				StkId stackBase = (!p.IsVarArg) ? (func + 1) : AdjustVarargs( p, n );
				
				CI = ExtendCI();
				CI.NumResults = nResults;
				CI.Func = func;
				CI.Base = stackBase;
				CI.Top  = stackBase + p.MaxStackSize;
				CI.SavedPc = new InstructionPtr( p.Code, 0 );
				CI.CallStatus = CallStatus.CIST_LUA;

				Top = CI.Top;

				return false;
			}

			var cscl = func.Value as LuaCSharpClosure;
			if( cscl != null )
			{
				CI = ExtendCI();
				CI.NumResults = nResults;
				CI.Func = func;
				CI.Top = Top + LuaDef.LUA_MINSTACK;
				CI.CallStatus = CallStatus.CIST_NONE;

				// do the actual call
				int n = cscl.F( this );
				
				// poscall
				D_PosCall( Top-n );

				return true;
			}

			// not a function
			// retry with `function' tag method
			func = tryFuncTM( func );

			// now it must be a function
			return D_PreCall( func, nResults );
		}

		private int D_PosCall( StkId firstResult )
		{

			CallInfo ci = CI;
			StkId res = ci.Func;
			int wanted = ci.NumResults;

#if DEBUG_D_POS_CALL
			Debug.Log( "[D] ==== PosCall enter" );
			Debug.Log( "[D] ==== PosCall res:" + res.Value );
			Debug.Log( "[D] ==== PosCall wanted:" + wanted );
#endif

			CI = ci.Previous;
			int i = wanted;
			for( ; i!=0 && firstResult.Index < Top.Index; --i )
			{
#if DEBUG_D_POS_CALL
				Debug.Log( "[D] ==== PosCall assign lhs res:" + res );
				Debug.Log( "[D] ==== PosCall assign rhs firstResult:" + firstResult );
#endif
				res.ValueInc = firstResult.ValueInc;
			}
			while( i-- > 0 )
			{
#if DEBUG_D_POS_CALL
				Debug.Log( "[D] ==== PosCall new LuaNil()" );
#endif
				res.ValueInc = new LuaNil();
			}
			Top = res;
#if DEBUG_D_POS_CALL
			Debug.Log( "[D] ==== PosCall return " + (wanted - LuaDef.LUA_MULTRET) );
#endif
			return (wanted - LuaDef.LUA_MULTRET);
		}

		private CallInfo ExtendCI()
		{
			CallInfo ci = new CallInfo();
			CI.Next = ci;
			ci.Previous = CI;
			ci.Next = null;
			return ci;
		}

		private StkId AdjustVarargs( LuaProto p, int actual )
		{
			// 有 `...' 的情况
			// 调用前: func (base)fixed-p1 fixed-p2 var-p1 var-p2 top
			// 调用后: func nil            nil      var-p1 var-p2 (base)fixed-p1 fixed-p2 (reserved...) top
			//
			// 没有 `...' 的情况
			// func (base)fixed-p1 fixed-p2 (reserved...) top

			int NumFixArgs = p.NumParams;
			Utl.Assert( actual >= NumFixArgs, "AdjustVarargs (actual >= NumFixArgs) is false" );

			StkId fixedArg = Top - actual; 		// first fixed argument
			StkId stackBase = Top;				// final position of first argument
			for( int i=0; i<NumFixArgs; ++i )
			{
				Top.ValueInc 		= fixedArg.Value;
				fixedArg.ValueInc 	= new LuaNil();
			}
			return stackBase;
		}

		private StkId tryFuncTM( StkId func )
		{
			var tmObj = T_GetTMByObj( func.Value, TMS.TM_CALL );
			if( !tmObj.IsFunction )
				G_TypeError( func, "call" );

			// open a hole inside the stack at `func'
			StkId q1 = Top-1;
			StkId q2 = Top;
			while( q2.Index > func.Index )
			{
				q2.Value = q1.Value;
				q1.Index--;
				q2.Index--;
			}
			IncrTop();
			func.Value = tmObj;
			return func;
		}

	}

}



// #define DEBUG_D_PRE_CALL
// #define DEBUG_D_POS_CALL

namespace UniLua
{
	using ULDebug = UniLua.Tools.ULDebug;
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

		private ThreadStatus D_RawRunProtected<T>( PFuncDelegate<T> func, ref T ud )
		{
			int oldNumCSharpCalls = NumCSharpCalls;
			ThreadStatus res = ThreadStatus.LUA_OK;
			try
			{
				func(ref ud);
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
					oldTop.V.SetSValue("not enough memory");
					break;

				case ThreadStatus.LUA_ERRERR:
					oldTop.V.SetSValue("error in error handling");
					break;

				default: // error message on current top
					oldTop.V.SetObj(ref Stack[Top.Index-1].V);
					break;
			}
			Top = Stack[oldTop.Index+1];
		}

		private ThreadStatus D_PCall<T>( PFuncDelegate<T> func, ref T ud,
			int oldTopIndex, int errFunc )
		{
			int oldCIIndex 			= CI.Index;
			bool oldAllowHook 		= AllowHook;
			int oldNumNonYieldable	= NumNonYieldable;
			int oldErrFunc			= ErrFunc;

			ErrFunc = errFunc;
			ThreadStatus status = D_RawRunProtected<T>( func, ref ud );
			if( status != ThreadStatus.LUA_OK ) // an error occurred?
			{
				F_Close( Stack[oldTopIndex] );
				SetErrorObj( status, Stack[oldTopIndex] );
				CI = BaseCI[oldCIIndex];
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
			ULDebug.Log( "============================ D_PreCall func:" + func );
#endif

			int funcIndex = func.Index;
			if(!func.V.TtIsFunction()) {
				// not a function
				// retry with `function' tag method
				func = tryFuncTM( func );

				// now it must be a function
				return D_PreCall( func, nResults );
			}

			if(func.V.ClIsLuaClosure()) {
				var cl = func.V.ClLValue();
				Utl.Assert(cl != null);
				var p = cl.Proto;

				D_CheckStack(p.MaxStackSize + p.NumParams);
				func = Stack[funcIndex];

				// 补全参数
				int n = (Top.Index - func.Index) - 1;
				for( ; n<p.NumParams; ++n )
					{ StkId.inc(ref Top).V.SetNilValue(); }

				int stackBase = (!p.IsVarArg) ? (func.Index + 1) : AdjustVarargs( p, n );
				
				CI = ExtendCI();
				CI.NumResults = nResults;
				CI.FuncIndex = func.Index;
				CI.BaseIndex = stackBase;
				CI.TopIndex  = stackBase + p.MaxStackSize;
				Utl.Assert(CI.TopIndex <= StackLast);
				CI.SavedPc = new InstructionPtr( p.Code, 0 );
				CI.CallStatus = CallStatus.CIST_LUA;

				Top = Stack[CI.TopIndex];

				return false;
			}

			if(func.V.ClIsCsClosure()) {
				var cscl = func.V.ClCsValue();
				Utl.Assert(cscl != null);

				D_CheckStack(LuaDef.LUA_MINSTACK);
				func = Stack[funcIndex];

				CI = ExtendCI();
				CI.NumResults = nResults;
				CI.FuncIndex = func.Index;
				CI.TopIndex = Top.Index + LuaDef.LUA_MINSTACK;
				CI.CallStatus = CallStatus.CIST_NONE;

				// do the actual call
				int n = cscl.F( this );
				
				// poscall
				D_PosCall( Top.Index-n );

				return true;
			}

			throw new System.NotImplementedException();
		}

		private int D_PosCall( int firstResultIndex )
		{
			// TODO: hook
			// be careful: CI may be changed after hook

			int resIndex = CI.FuncIndex;
			int wanted = CI.NumResults;

#if DEBUG_D_POS_CALL
			ULDebug.Log( "[D] ==== PosCall enter" );
			ULDebug.Log( "[D] ==== PosCall res:" + res );
			ULDebug.Log( "[D] ==== PosCall wanted:" + wanted );
#endif

			CI = BaseCI[CI.Index-1];

			int i = wanted;
			for( ; i!=0 && firstResultIndex < Top.Index; --i )
			{
#if DEBUG_D_POS_CALL
				ULDebug.Log( "[D] ==== PosCall assign lhs res:" + res );
				ULDebug.Log( "[D] ==== PosCall assign rhs firstResult:" + firstResult );
#endif
				Stack[resIndex++].V.SetObj(ref Stack[firstResultIndex++].V);
			}
			while( i-- > 0 )
			{
#if DEBUG_D_POS_CALL
				ULDebug.Log( "[D] ==== PosCall new LuaNil()" );
#endif
				Stack[resIndex++].V.SetNilValue();
			}
			Top = Stack[resIndex];
#if DEBUG_D_POS_CALL
			ULDebug.Log( "[D] ==== PosCall return " + (wanted - LuaDef.LUA_MULTRET) );
#endif
			return (wanted - LuaDef.LUA_MULTRET);
		}

		private CallInfo ExtendCI()
		{
			int newIndex = CI.Index + 1;
			if(newIndex >= BaseCI.Length) {
				int newLength = BaseCI.Length*2;
				var newBaseCI = new CallInfo[newLength];
				int i = 0;
				while(i < BaseCI.Length) {
					newBaseCI[i] = BaseCI[i];
					newBaseCI[i].List = newBaseCI;
					++i;
				}
				while(i < newLength) {
					var newCI = new CallInfo();
					newBaseCI[i] = newCI;
					newCI.List = newBaseCI;
					newCI.Index = i;
					++i;
				}
				BaseCI = newBaseCI;
				CI = newBaseCI[CI.Index];
				return newBaseCI[newIndex];
			}
			else {
				return BaseCI[newIndex];
			}
		}

		private int AdjustVarargs( LuaProto p, int actual )
		{
			// 有 `...' 的情况
			// 调用前: func (base)fixed-p1 fixed-p2 var-p1 var-p2 top
			// 调用后: func nil            nil      var-p1 var-p2 (base)fixed-p1 fixed-p2 (reserved...) top
			//
			// 没有 `...' 的情况
			// func (base)fixed-p1 fixed-p2 (reserved...) top

			int NumFixArgs = p.NumParams;
			Utl.Assert( actual >= NumFixArgs, "AdjustVarargs (actual >= NumFixArgs) is false" );

			int fixedArg = Top.Index - actual; 	// first fixed argument
			int stackBase = Top.Index;		// final position of first argument
			for( int i=stackBase; i<stackBase+NumFixArgs; ++i )
			{
				Stack[i].V.SetObj(ref Stack[fixedArg].V);
				Stack[fixedArg++].V.SetNilValue();
			}
			Top = Stack[stackBase+NumFixArgs];
			return stackBase;
		}

		private StkId tryFuncTM( StkId func )
		{
			var tmObj = T_GetTMByObj( ref func.V, TMS.TM_CALL );
			if(!tmObj.V.TtIsFunction())
				G_TypeError( func, "call" );

			// open a hole inside the stack at `func'
			for(int i=Top.Index; i>func.Index; --i)
				{ Stack[i].V.SetObj(ref Stack[i-1].V); }

			IncrTop();
			func.V.SetObj(ref tmObj.V);
			return func;
		}

		private void D_CheckStack(int n)
		{
			if(StackLast - Top.Index <= n)
				D_GrowStack(n);
			// TODO: FOR DEBUGGING
			// else
			// 	CondMoveStack();
		}

		// some space for error handling
		private const int ERRORSTACKSIZE = LuaConf.LUAI_MAXSTACK + 200;

		private void D_GrowStack(int n)
		{
			int size = Stack.Length;
			if(size > LuaConf.LUAI_MAXSTACK)
				D_Throw(ThreadStatus.LUA_ERRERR);

			int needed = Top.Index + n + LuaDef.EXTRA_STACK;
			int newsize = 2 * size;
			if(newsize > LuaConf.LUAI_MAXSTACK)
				{ newsize = LuaConf.LUAI_MAXSTACK; }
			if(newsize < needed)
				{ newsize = needed; }
			if(newsize > LuaConf.LUAI_MAXSTACK)
			{
				D_ReallocStack(ERRORSTACKSIZE);
				G_RunError("stack overflow");
			}
			else
			{
				D_ReallocStack(newsize);
			}
		}

		private void D_ReallocStack(int size)
		{
			Utl.Assert(size <= LuaConf.LUAI_MAXSTACK || size == ERRORSTACKSIZE);
			var newStack = new StkId[size];
			int i = 0;
			for( ; i<Stack.Length; ++i) {
				newStack[i] = Stack[i];
				newStack[i].SetList(newStack);
			}
			for( ; i<size; ++i) {
				newStack[i] = new StkId();
				newStack[i].SetList(newStack);
				newStack[i].SetIndex(i);
				newStack[i].V.SetNilValue();
			}
			Top = newStack[Top.Index];
			Stack = newStack;
			StackLast = size - LuaDef.EXTRA_STACK;
		}
	}

}


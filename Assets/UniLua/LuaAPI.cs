using System;

// #define DEBUG_RECORD_INS

namespace UniLua
{
	public interface ILoadInfo
	{
		int ReadByte();
		int PeekByte();
	}

	public delegate int CSharpFunctionDelegate(ILuaState state);
	public interface ILuaAPI
	{
		LuaState NewThread();

		ThreadStatus Load( ILoadInfo loadinfo, string name, string mode );
		// ThreadStatus LoadBinary( byte[] bytes );
		DumpStatus Dump( LuaWriter writeFunc );

		ThreadStatus GetContext( out int context );
		void Call( int numArgs, int numResults );
		void CallK( int numArgs, int numResults,
			int context, CSharpFunctionDelegate continueFunc );
		ThreadStatus PCall( int numArgs, int numResults, int errFunc);
		ThreadStatus PCallK( int numArgs, int numResults, int errFunc,
			int context, CSharpFunctionDelegate continueFunc );

		ThreadStatus Resume( ILuaState from, int numArgs );
		int Yield( int numResults );
		int YieldK( int numResults,
			int context, CSharpFunctionDelegate continueFunc );

		int  AbsIndex( int index );
		int  GetTop();
		void SetTop( int top );

		void Remove( int index );
		void Insert( int index );
		void Replace( int index );
		void Copy( int fromIndex, int toIndex );
		void XMove( ILuaState to, int n );

		bool GetStack( int level, LuaDebug ar );
		int  Error();

		int  UpvalueIndex( int i );
		string GetUpvalue( int funcIndex, int n );
		string SetUpvalue( int funcIndex, int n );

		void CreateTable( int narray, int nrec );
		void NewTable();
		bool Next( int index );
		void RawGetI( int index, int n );
		void RawSetI( int index, int n );
		void RawGet( int index );
		void RawSet( int index );
		void GetField( int index, string key );
		void SetField( int index, string key );

		void Concat( int n );

		LuaType Type( int index );
		string TypeName( LuaType t );
		bool IsNil( int index );
		bool IsNone( int index );
		bool IsNoneOrNil( int index );
		bool IsString( int index );
		bool IsTable( int index );
		bool IsFunction( int index );

		bool Compare( int index1, int index2, LuaEq op );
		bool RawEqual( int index1, int index2 );
		int  RawLen( int index );
		void Len( int index );

		void PushNil();
		void PushBoolean( bool b );
		void PushNumber( double n );
		void PushInteger( int n );
		void PushUnsigned( uint n );
		string PushString( string s );
		void PushCSharpFunction( CSharpFunctionDelegate f );
		void PushCSharpClosure( CSharpFunctionDelegate f, int n );
		void PushValue( int index );
		void PushGlobalTable();
		void PushLightUserData( object o );
		void PushUInt64( UInt64 o );
		bool PushThread();

		void Pop( int n );

		bool GetMetaTable( int index );
		bool SetMetaTable( int index );

		void GetGlobal( string name );
		void SetGlobal( string name );

		string 	ToString( int index );
		double 	ToNumberX( int index, out bool isnum );
		double 	ToNumber( int index );
		int		ToIntegerX( int index, out bool isnum );
		int    	ToInteger( int index );
		uint	ToUnsignedX( int index, out bool isnum );
		uint	ToUnsigned( int index );
		bool   	ToBoolean( int index );
		object 	ToObject( int index );
		object  ToUserData( int index );
		ILuaState	ToThread( int index );

		ThreadStatus	Status { get; }

		// void 	DebugRawGetI( int index, int n );
		string 	DebugGetInstructionHistory();
	}

	public interface ILuaState : ILuaAPI, ILuaAuxLib
	{
	}

	public static class LuaAPI
	{
		public static ILuaState NewState()
		{
			return new LuaState();
		}
	}

	public partial class LuaState : ILuaState
	{
		LuaState ILuaAPI.NewThread()
		{
			LuaState newLua = new LuaState(G);
			Top.Value = newLua;
			ApiIncrTop();

			newLua.HookMask = HookMask;
			newLua.BaseHookCount = BaseHookCount;
			newLua.Hook = Hook;
			newLua.ResetHookCount();

			return newLua;
		}

		// private void F_LoadBinary( object ud )
		// {
		// 	byte[] bytes = (byte[])ud;

		// 	var reader = new BinaryBytesReader( bytes );
		// 	Undump u = new Undump( reader );

		// 	LuaProto proto = Undump.LoadBinary( this, loadinfo, "" );

		// 	var cl = new LuaLClosure( proto );
		// 	for( int i=0; i<proto.Upvalues.Count; ++i )
		// 	{
		// 		cl.Upvals[i] = new LuaUpvalue();
		// 	}

		// 	Top.Value = cl;
		// 	IncrTop();
		// }

		// ThreadStatus ILuaAPI.LoadBinary( byte[] bytes )
		// {
		// 	var status = D_PCall( F_LoadBinary, bytes, Top, ErrFunc );

		// 	if( status == ThreadStatus.LUA_OK )
		// 	{
		// 		var cl = (Top-1).Value as LuaLClosure;
		// 		if( cl.Upvals.Count == 1 )
		// 		{
		// 			cl.Upvals[0].V.Value = G.Registry.GetInt( LuaDef.LUA_RIDX_GLOBALS );
		// 		}
		// 	}

		// 	return status;
		// }

		struct LoadParameter
		{
			public ILoadInfo 	LoadInfo;
			public string 		Name;
			public string 		Mode;

			public LoadParameter( ILoadInfo loadinfo, string name, string mode )
			{
				LoadInfo 	= loadinfo;
				Name		= name;
				Mode		= mode;
			}
		}

		private void CheckMode( string given, string expected )
		{
			if( given != null && given != expected )
			{
				O_PushString( string.Format(
					"attempt to load a {0} chunk (mode is '{1}')",
					expected, given ) );
				D_Throw( ThreadStatus.LUA_ERRSYNTAX );
			}
		}

		private void F_Load( object ud )
		{
			var param = (LoadParameter)ud;

			LuaProto proto;
			var c = param.LoadInfo.PeekByte();
			if( c == LuaConf.LUA_SIGNATURE[0] )
			{
				CheckMode( param.Mode, "binary" );
				proto = Undump.LoadBinary( this, param.LoadInfo, param.Name );
			}
			else
			{
				CheckMode( param.Mode, "text" );
				proto = Parser.Parse( this, param.LoadInfo, param.Name );
			}

			var cl = new LuaLClosure( proto );
			Utl.Assert( cl.Upvals.Count == cl.Proto.Upvalues.Count );

			// initialize upvalues
			for( int i=0; i<proto.Upvalues.Count; ++i )
			{
				cl.Upvals[i] = new LuaUpvalue();
			}

			Top.Value = cl;
			IncrTop();
		}

		ThreadStatus ILuaAPI.Load( ILoadInfo loadinfo, string name, string mode )
		{
			var param  = new LoadParameter( loadinfo, name, mode );
			var status = D_PCall( F_Load, param, Top, ErrFunc );

			if( status == ThreadStatus.LUA_OK )
			{
				var cl = (Top-1).Value as LuaLClosure;
				if( cl.Upvals.Count == 1 )
				{
					cl.Upvals[0].V.Value = G.Registry.GetInt(
						LuaDef.LUA_RIDX_GLOBALS );
				}
			}

			return status;
		}

		DumpStatus ILuaAPI.Dump( LuaWriter writeFunc )
		{
			Utl.ApiCheckNumElems( this, 1 );
			var o = (Top-1).Value as LuaLClosure;
			var status = (o == null) ? DumpStatus.ERROR :
				DumpState.Dump( o.Proto, writeFunc, false );
			return status;
		}

		ThreadStatus ILuaAPI.GetContext( out int context )
		{
			if( (CI.CallStatus & CallStatus.CIST_YIELDED) != 0 )
			{
				context = CI.Context;
				return CI.Status;
			}
			else
			{
				context = default(int);
				return ThreadStatus.LUA_OK;
			}
		}

		void ILuaAPI.Call( int numArgs, int numResults )
		{
			// StkId func = Top - (numArgs + 1);
			// if( !D_PreCall( func, numResults ) )
			// {
			// 	V_Execute();
			// }

			API.CallK( numArgs, numResults, 0, null );
		}

		void ILuaAPI.CallK( int numArgs, int numResults,
			int context, CSharpFunctionDelegate continueFunc )
		{
			Utl.ApiCheck( continueFunc == null || !CI.IsLua,
				"cannot use continuations inside hooks" );
			Utl.ApiCheckNumElems( this, numArgs + 1 );
			Utl.ApiCheck( Status == ThreadStatus.LUA_OK,
				"cannot do calls on non-normal thread" );
			CheckResults( numArgs, numResults );
			var func = Top - (numArgs+1);

			// need to prepare continuation?
			if( continueFunc != null && NumNonYieldable == 0 )
			{
				CI.ContinueFunc = continueFunc;
				CI.Context		= context;
				D_Call( func, numResults, true );
			}
			// no continuation or no yieldable
			else
			{
				D_Call( func, numResults, false );
			}
			AdjustResults( numResults );
		}

		private struct CallS
		{
			public StkId Func;
			public int NumResults;
		}

		private delegate void PFuncDelegate( object ud );

		private void F_Call( object ud )
		{
			CallS c = (CallS)ud;
			D_Call( c.Func, c.NumResults, false );
		}

		private void CheckResults( int numArgs, int numResults )
		{
			Utl.ApiCheck( numResults == LuaDef.LUA_MULTRET ||
				CI.Top.Index - Top.Index >= numResults - numArgs,
				"results from function overflow current stack size" );
		}

		private void AdjustResults( int numResults )
		{
			if( numResults == LuaDef.LUA_MULTRET &&
				CI.Top.Index < Top.Index )
			{
				CI.Top = Top;
			}
		}

		ThreadStatus ILuaAPI.PCall( int numArgs, int numResults, int errFunc)
		{
			return API.PCallK( numArgs, numResults, errFunc, 0, null );
		}

		ThreadStatus ILuaAPI.PCallK( int numArgs, int numResults, int errFunc,
			int context, CSharpFunctionDelegate continueFunc )
		{
			Utl.ApiCheck( continueFunc == null || !CI.IsLua,
				"cannot use continuations inside hooks" );
			Utl.ApiCheckNumElems( this, numArgs + 1 );
			Utl.ApiCheck( Status == ThreadStatus.LUA_OK,
				"cannot do calls on non-normal thread" );
			CheckResults( numArgs, numResults );

			int func;
			if( errFunc == 0 )
				func = 0;
			else
			{
				StkId addr;
				if( !Index2Addr( errFunc, out addr ) )
					Utl.InvalidIndex();

				func = addr.Index;
			}

			ThreadStatus status;
			CallS c = new CallS();
			c.Func = Top - (numArgs + 1);
			if( continueFunc == null || NumNonYieldable > 0 ) // no continuation or no yieldable?
			{
				c.NumResults = numResults;
				status = D_PCall( F_Call, c, c.Func, func );
			}
			else
			{
				CallInfo ci = CI;
				ci.ContinueFunc = continueFunc;
				ci.Context		= context;
				ci.Extra		= c.Func;
				ci.OldAllowHook	= AllowHook;
				ci.OldErrFunc	= ErrFunc;
				ErrFunc = func;
				ci.CallStatus |= CallStatus.CIST_YPCALL;
				D_Call( c.Func, numResults, true );
				ci.CallStatus &= ~CallStatus.CIST_YPCALL;
				ErrFunc = ci.OldErrFunc;
				status = ThreadStatus.LUA_OK;
			}
			AdjustResults( numResults );
			return status;
		}

		private void FinishCSharpCall()
		{
			CallInfo ci = CI;
			Utl.Assert( ci.ContinueFunc != null ); // must have a continuation
			Utl.Assert( NumNonYieldable == 0 );
			// finish `CallK'
			AdjustResults( ci.NumResults );
			// call continuation function
			if( (ci.CallStatus & CallStatus.CIST_STAT) == 0 ) // no call status?
			{
				ci.Status = ThreadStatus.LUA_YIELD; // `default' status
			}
			Utl.Assert( ci.Status != ThreadStatus.LUA_OK );
			ci.CallStatus = ( ci.CallStatus
							& ~( CallStatus.CIST_YPCALL | CallStatus.CIST_STAT))
							| CallStatus.CIST_YIELDED;

			int n = ci.ContinueFunc( this ); // call
			Utl.ApiCheckNumElems( this, n );
			// finish `D_PreCall'
			D_PosCall( Top-n );
		}

		private void Unroll( object ud )
		{
			while( true )
			{
				if( CI == BaseCI ) // stack is empty?
					return; // coroutine finished normally
				if( !CI.IsLua ) // C# function?
					FinishCSharpCall();
				else // Lua function
				{
					V_FinishOp(); // finish interrupted instruction
					V_Execute(); // execute down to higher C# `boundary'
				}
			}
		}

		private void ResumeError( string msg, StkId firstArg )
		{
			Top = firstArg;
			Top.Value = new LuaString( msg );
			IncrTop();
			D_Throw( ThreadStatus.LUA_RESUME_ERROR );
		}

		// check whether thread has suspended protected call
		private CallInfo FindPCall()
		{
			CallInfo ci;
			for( ci = CI; ci != null; ci = ci.Previous ) // search for a pcall
			{
				if( (ci.CallStatus & CallStatus.CIST_YPCALL) != 0 )
					return ci;
			}
			return null; // no pending pcall
		}

		private bool Recover( ThreadStatus status )
		{
			CallInfo ci = FindPCall();
			if( ci == null ) // no recover point
				return false;

			StkId oldTop = ci.Extra;
			F_Close( oldTop );
			SetErrorObj( status, oldTop );
			CI = ci;
			AllowHook = ci.OldAllowHook;
			NumNonYieldable = 0;
			ErrFunc = ci.OldErrFunc;
			ci.CallStatus |= CallStatus.CIST_STAT;
			ci.Status = status;
			return true;
		}

		// do the work for `lua_resume' in protected mode
		private void Resume( object ud )
		{
			int numCSharpCalls = NumCSharpCalls;
			StkId firstArg = (StkId)ud;
			CallInfo ci = CI;
			if( numCSharpCalls >= LuaLimits.LUAI_MAXCCALLS )
				ResumeError( "C stack overflow", firstArg );
			if( Status == ThreadStatus.LUA_OK ) // may be starting a coroutine
			{
				if( ci != BaseCI ) // not in base level
				{
					ResumeError( "cannot resume non-suspended coroutine", firstArg );
				}
				if( !D_PreCall( firstArg-1, LuaDef.LUA_MULTRET ) ) // Lua function?
				{
					V_Execute(); // call it
				}
			}
			else if( Status != ThreadStatus.LUA_YIELD )
			{
				ResumeError( "cannot resume dead coroutine", firstArg );
			}
			else // resume from previous yield
			{
				Status = ThreadStatus.LUA_OK;
				ci.Func = ci.Extra;
				if( ci.IsLua ) // yielded inside a hook?
				{
					V_Execute(); // just continue running Lua code
				}
				else // `common' yield
				{
					if( ci.ContinueFunc != null )
					{
						ci.Status = ThreadStatus.LUA_YIELD; // `default' status
						ci.CallStatus |= CallStatus.CIST_YIELDED;
						int n = ci.ContinueFunc( this ); // call continuation
						Utl.ApiCheckNumElems( this, n );
						firstArg = Top - n; // yield results come from continuation
					}
					D_PosCall( firstArg );
				}
				Unroll( null );
			}
			Utl.Assert( numCSharpCalls == NumCSharpCalls );
		}

		ThreadStatus ILuaAPI.Resume( ILuaState from, int numArgs )
		{
			LuaState fromState = from as LuaState;
			NumCSharpCalls = (fromState != null) ? fromState.NumCSharpCalls + 1 : 1;
			NumNonYieldable = 0; // allow yields

			Utl.ApiCheckNumElems( this, (Status == ThreadStatus.LUA_OK) ? numArgs + 1 : numArgs );
			ThreadStatus status = D_RawRunProtected( Resume, Top-numArgs );
			if( status == ThreadStatus.LUA_RESUME_ERROR ) // error calling `lua_resume'?
			{
				status = ThreadStatus.LUA_ERRRUN;
			}
			else // yield or regular error
			{
				while( status != ThreadStatus.LUA_OK && status != ThreadStatus.LUA_YIELD ) // error?
				{
					if( Recover( status ) ) // recover point?
					{
						status = D_RawRunProtected( Unroll, null );
					}
					else // unrecoverable error
					{
						Status = status; // mark thread as `dead'
						SetErrorObj( status, Top );
						CI.Top = Top;
						break;
					}
				}
				Utl.Assert( status == Status );
			}

			NumNonYieldable = 1; // do not allow yields
			NumCSharpCalls--;
			Utl.Assert( NumCSharpCalls == ((fromState != null) ? fromState.NumCSharpCalls : 0) );
			return status;
		}

		int ILuaAPI.Yield( int numResults )
		{
			return API.YieldK( numResults, 0, null );
		}

		int ILuaAPI.YieldK( int numResults,
			int context, CSharpFunctionDelegate continueFunc )
		{
			CallInfo ci = CI;
			Utl.ApiCheckNumElems( this, numResults );

			if( NumNonYieldable > 0 )
			{
				if( this != G.MainThread )
					G_RunError( "attempt to yield across metamethod/C-call boundary" );
				else
					G_RunError( "attempt to yield from outside a coroutine" );
			}
			Status = ThreadStatus.LUA_YIELD;
			ci.Extra = ci.Func; // save current `func'
			if( ci.IsLua ) // inside a hook
			{
				Utl.ApiCheck( continueFunc == null, "hooks cannot continue after yielding" );
			}
			else
			{
				ci.ContinueFunc = continueFunc;
				if( ci.ContinueFunc != null ) // is there a continuation
				{
					ci.Context = context;
				}
				ci.Func = Top - (numResults + 1);
				D_Throw( ThreadStatus.LUA_YIELD );
			}
			Utl.Assert( (ci.CallStatus & CallStatus.CIST_HOOKED) != 0 ); // must be inside a hook
			return 0;
		}
		

		int ILuaAPI.AbsIndex( int index )
		{
			return (index > 0 || index <= LuaDef.LUA_REGISTRYINDEX)
				 ? index
				 : Top.Index - CI.Func.Index + index;
		}

		int ILuaAPI.GetTop()
		{
			return Top.Index - (CI.Func.Index + 1);
		}

		void ILuaAPI.SetTop( int index )
		{
			StkId func = CI.Func;
			if( index >= 0 )
			{
				while( Top.Index < (func.Index + 1) + index )
					Top.ValueInc = new LuaNil();
				Top = func + 1 + index;
			}
			else
			{
				Utl.ApiCheck( -(index+1) <= (Top.Index - (func.Index + 1)), "invalid new top" );
				Top += index+1;
			}
		}

		void ILuaAPI.Remove( int index )
		{
			StkId addr1;
			if( !Index2Addr( index, out addr1 ) )
				Utl.InvalidIndex();

			StkId addr2 = addr1 + 1;
			while( addr2.Index < Top.Index )
				addr1.ValueInc = addr2.ValueInc;
			Top.Index--;
		}

		void ILuaAPI.Insert( int index )
		{
			StkId p;
			if( !Index2Addr( index, out p ) )
				Utl.InvalidIndex();

			StkId q1 = Top-1;
			StkId q2 = Top;
			while( q2.Index > p.Index )
			{
				q2.Value = q1.Value;
				q1.Index--;
				q2.Index--;
			}
			p.Value = Top.Value;
		}

		private void MoveTo( StkId fr, int index )
		{
			StkId to;
			if( !Index2Addr( index, out to ) )
				Utl.InvalidIndex();

			to.Value = fr.Value;
		}

		void ILuaAPI.Replace( int index )
		{
			Utl.ApiCheckNumElems( this, 1 );
			MoveTo( Top-1, index );
			Top.Index--;
		}
		
		void ILuaAPI.Copy( int fromIndex, int toIndex )
		{
			StkId fr;
			if( !Index2Addr( fromIndex, out fr ) )
				Utl.InvalidIndex();
			MoveTo( fr, toIndex );
		}

		void ILuaAPI.XMove( ILuaState to, int n )
		{
			var toLua = to as LuaState;
			if( (LuaState)this == toLua )
				return;

			Utl.ApiCheckNumElems( this, n );
			Utl.ApiCheck( G == toLua.G, "moving among independent states" );
			Utl.ApiCheck( toLua.CI.Top.Index - toLua.Top.Index >= n, "not enough elements to move" );
			Top.Index -= n;

			var src = Top;
			for( int i=0; i<n; ++i )
				toLua.Top.ValueInc = src.ValueInc;
		}

		int ILuaAPI.Error()
		{
			Utl.ApiCheckNumElems( this, 1 );
			G_ErrorMsg();
			return 0;
		}

		int ILuaAPI.UpvalueIndex( int i )
		{
			return LuaDef.LUA_REGISTRYINDEX - i;
		}

		string ILuaAPI.GetUpvalue( int funcIndex, int n )
		{
			StkId addr;
			if( !Index2Addr( funcIndex, out addr ) )
				return null;

			LuaClosure cl = addr.Value as LuaClosure;
			if( cl == null )
				return null;

			LuaObject val;
			string name = cl.GetUpvalue( n, out val );
			if( name == null )
				return null;

			Top.Value = val;
			ApiIncrTop();
			return name;
		}

		string ILuaAPI.SetUpvalue( int funcIndex, int n )
		{
			StkId addr;
			if( !Index2Addr( funcIndex, out addr ) )
				return null;

			LuaClosure cl = addr.Value as LuaClosure;
			if( cl == null )
				return null;

			Utl.ApiCheckNumElems( this, 1 );

			string name = cl.SetUpvalue( n, Top.Value );
			if( name == null )
				return null;

			Top.Index--;
			return name;
		}

		void ILuaAPI.CreateTable( int narray, int nrec )
		{
			Top.Value = new LuaTable();
			ApiIncrTop();
		}

		void ILuaAPI.NewTable()
		{
			API.CreateTable( 0, 0 );
		}

		bool ILuaAPI.Next( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				throw new System.Exception("table expected");

			var tbl = addr.Value as LuaTable;
			if( tbl == null )
				throw new System.Exception("table expected");

			var key = Top - 1;
			if( tbl.Next( this, key ) )
			{
				ApiIncrTop();
				return true;
			}
			else
			{
				Top -= 1;
				return false;
			}
		}

		void ILuaAPI.RawGetI( int index, int n )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.ApiCheck( false, "table expected" );

			var tbl = addr.Value as LuaTable;
			Utl.ApiCheck( tbl != null, "table expected" );

			Top.Value = tbl.GetInt( n );
			ApiIncrTop();
		}

		// void ILuaAPI.DebugRawGetI( int index, int n )
		// {
		// 	StkId addr;
		// 	if( !Index2Addr( index, out addr ) )
		// 		Utl.ApiCheck( false, "table expected" );

		// 	var tbl = addr.Value as LuaTable;
		// 	Utl.ApiCheck( tbl != null, "table expected" );

		// 	var key = new LuaNumber( n );
		// 	LuaObject outKey, outValue;
		// 	tbl.DebugGet( key, out outKey, out outValue );
		// 	Top.Value = outKey;
		// 	ApiIncrTop();
		// 	Top.Value = outValue;
		// 	ApiIncrTop();
		// }

		string ILuaAPI.DebugGetInstructionHistory()
		{
#if DEBUG_RECORD_INS
			var sb = new System.Text.StringBuilder();
			foreach( var i in InstructionHistory ) {
				sb.AppendLine( i.ToString() );
			}
			return sb.ToString();
#else
			return "DEBUG_RECORD_INS not defined";
#endif
		}

		void ILuaAPI.RawGet( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				throw new System.Exception("table expected");

			var tbl = addr.Value as LuaTable;
			if( tbl == null )
				throw new System.Exception("table expected");

			var below = Top-1;
			below.Value = tbl.Get( below.Value );
		}

		void ILuaAPI.RawSetI( int index, int n )
		{
			Utl.ApiCheckNumElems( this, 1 );
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();
			Utl.ApiCheck( addr.Value.IsTable, "table expected" );
			var tbl = addr.Value as LuaTable;
			tbl.SetInt( n, (Top-1).Value );
			Top.Index--;
		}

		void ILuaAPI.RawSet( int index )
		{
			Utl.ApiCheckNumElems( this, 2 );
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();
			Utl.ApiCheck( addr.Value.IsTable, "table expected" );
			var tbl = addr.Value as LuaTable;
			tbl.Set( (Top-2).Value, (Top-1).Value );
			Top.Index -= 2;
		}

		void ILuaAPI.GetField( int index, string key )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			Top.Value = new LuaString( key );
			var below = Top;
			ApiIncrTop();
			V_GetTable( addr.Value, below.Value, below );
		}

		void ILuaAPI.SetField( int index, string key )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			Top.ValueInc = new LuaString( key );
			V_SetTable( addr.Value, (Top-1).Value, Top-2 );
			Top.Index -= 2;
		}

		void ILuaAPI.Concat( int n )
		{
			Utl.ApiCheckNumElems( this, n );
			if( n >= 2 )
			{
				V_Concat( n );
			}
			else if( n == 0 )
			{
				Top.Value = new LuaString("");
				ApiIncrTop();
			}
		}

		LuaType ILuaAPI.Type( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return LuaType.LUA_TNONE;

			return addr.Value.LuaType;
		}

		string ILuaAPI.TypeName( LuaType t )
		{
			switch( t )
			{
				case LuaType.LUA_TNIL:
					return "nil";

				case LuaType.LUA_TBOOLEAN:
					return "boolean";

				case LuaType.LUA_TLIGHTUSERDATA:
					return "userdata";

				case LuaType.LUA_TUINT64:
					return "userdata";

				case LuaType.LUA_TNUMBER:
					return "number";

				case LuaType.LUA_TSTRING:
					return "string";

				case LuaType.LUA_TTABLE:
					return "table";

				case LuaType.LUA_TFUNCTION:
					return "function";

				case LuaType.LUA_TUSERDATA:
					return "userdata";

				case LuaType.LUA_TTHREAD:
					return "thread";

				case LuaType.LUA_TPROTO:
					return "proto";

				case LuaType.LUA_TUPVAL:
					return "upval";

				default:
					return "no value";
			}
		}

		internal string ObjTypeName( LuaObject o )
		{
			return API.TypeName( o.LuaType );
		}

		// 用于内部使用 不会因为 ApiIncrTop() 检查 Top 超过 CI.Top 报错
		internal void O_PushString( string s )
		{
			Top.Value = new LuaString( s );
			IncrTop();
		}

		bool ILuaAPI.IsNil( int index )
		{
			return API.Type( index ) == LuaType.LUA_TNIL;
		}

		bool ILuaAPI.IsNone( int index )
		{
			return API.Type( index ) == LuaType.LUA_TNONE;
		}

		bool ILuaAPI.IsNoneOrNil( int index )
		{
			LuaType t = API.Type( index );
			return t == LuaType.LUA_TNONE ||
				t == LuaType.LUA_TNIL;
		}

		bool ILuaAPI.IsString( int index )
		{
			LuaType t = API.Type( index );
			return( t == LuaType.LUA_TSTRING || t == LuaType.LUA_TNUMBER );
		}

		bool ILuaAPI.IsTable( int index )
		{
			return API.Type( index ) == LuaType.LUA_TTABLE;
		}

		bool ILuaAPI.IsFunction( int index )
		{
			return API.Type( index ) == LuaType.LUA_TFUNCTION;
		}

		bool ILuaAPI.Compare( int index1, int index2, LuaEq op )
		{
			StkId addr1;
			if( !Index2Addr( index1, out addr1 ) )
				Utl.InvalidIndex();

			StkId addr2;
			if( !Index2Addr( index2, out addr2 ) )
				Utl.InvalidIndex();

			switch( op )
			{
				case LuaEq.LUA_OPEQ: return EqualObj( addr1.Value, addr2.Value, false );
				case LuaEq.LUA_OPLT: return V_LessThan( addr1, addr2 );
				case LuaEq.LUA_OPLE: return V_LessEqual( addr1, addr2 );
				default: Utl.ApiCheck( false, "invalid option" ); return false;
			}
		}

		bool ILuaAPI.RawEqual( int index1, int index2 )
		{
			StkId addr1;
			if( !Index2Addr( index1, out addr1 ) )
				return false;

			StkId addr2;
			if( !Index2Addr( index2, out addr2 ) )
				return false;

			return V_RawEqualObj( addr1.Value, addr2.Value );
		}

		int ILuaAPI.RawLen( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();
			LuaType t = addr.Value.LuaType;
			switch( t )
			{
				case LuaType.LUA_TSTRING:
				{
					LuaString s = addr.Value as LuaString;
					return s.Value.Length;
				}
				case LuaType.LUA_TUSERDATA:
				{
					LuaUserData ud = addr.Value as LuaUserData;
					return ud.Length;
				}
				case LuaType.LUA_TTABLE:
				{
					LuaTable tbl = addr.Value as LuaTable;
					return tbl.Length;
				}
				default:
					return 0;
			}
		}

		void ILuaAPI.Len( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			V_ObjLen( Top, addr );

			ApiIncrTop();
		}

		void ILuaAPI.PushNil()
		{
			Top.Value = new LuaNil();
			ApiIncrTop();
		}

		void ILuaAPI.PushBoolean( bool b )
		{
			Top.Value = new LuaBoolean( b );
			ApiIncrTop();
		}

		void ILuaAPI.PushNumber( double n )
		{
			Top.Value = new LuaNumber( n );
			ApiIncrTop();
		}

		void ILuaAPI.PushInteger( int n )
		{
			Top.Value = new LuaNumber( n );
			ApiIncrTop();
		}

		void ILuaAPI.PushUnsigned( uint n )
		{
			Top.Value = new LuaNumber( (double)n );
			ApiIncrTop();
		}

		string ILuaAPI.PushString( string s )
		{
			if( s == null )
			{
				API.PushNil();
				return null;
			}
			else
			{
				Top.Value = new LuaString( s );
				ApiIncrTop();
				return s;
			}
		}

		void ILuaAPI.PushCSharpFunction( CSharpFunctionDelegate f )
		{
			API.PushCSharpClosure( f, 0 );
		}
		
		void ILuaAPI.PushCSharpClosure( CSharpFunctionDelegate f, int n )
		{
			if( n == 0 )
			{
				Top.Value = new LuaCSharpClosure( f );
			}
			else
			{
				// 带 UpValue 的 C# function
				Utl.ApiCheckNumElems( this, n );
				Utl.ApiCheck( n <= LuaLimits.MAXUPVAL, "upvalue index too large" );
				LuaCSharpClosure cscl = new LuaCSharpClosure( f, n );
				var src = Top - n;
				while( src.Index < Top.Index )
					cscl.Upvals.Add( src.ValueInc );
				Top.Index -= n;
				Top.Value = cscl;
			}
			ApiIncrTop();
		}

		void ILuaAPI.PushValue( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			Top.Value = addr.Value;
			ApiIncrTop();
		}

		void ILuaAPI.PushGlobalTable()
		{
			API.RawGetI( LuaDef.LUA_REGISTRYINDEX, LuaDef.LUA_RIDX_GLOBALS );
		}

		void ILuaAPI.PushLightUserData( object o )
		{
			Top.Value = new LuaLightUserData( o );
			ApiIncrTop();
		}

		void ILuaAPI.PushUInt64( UInt64 o )
		{
			Top.Value = new LuaUInt64( o );
			ApiIncrTop();
		}

		bool ILuaAPI.PushThread()
		{
			Top.Value = this;
			ApiIncrTop();
			return G.MainThread == (LuaState)this;
		}

		void ILuaAPI.Pop( int n )
		{
			API.SetTop( -n-1 );
		}

		bool ILuaAPI.GetMetaTable( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			LuaObject mt;
			switch( addr.Value.LuaType )
			{
				case LuaType.LUA_TTABLE:
				{
					LuaTable tbl = (addr.Value as LuaTable);
					mt = tbl.MetaTable;
					break;
				}
				case LuaType.LUA_TUSERDATA:
				{
					LuaUserData ud = (addr.Value as LuaUserData);
					mt = ud.MetaTable;
					break;
				}
				default:
				{
					mt = G.MetaTables[(int)addr.Value.LuaType];
					break;
				}
			}
			if( mt == null )
				return false;
			else
			{
				Top.Value = mt;
				ApiIncrTop();
				return true;
			}
		}

		bool ILuaAPI.SetMetaTable( int index )
		{
			Utl.ApiCheckNumElems( this, 1 );

			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			var below = Top - 1;
			LuaTable mt;
			if( below.Value.IsNil )
				mt = null;
			else
			{
				Utl.ApiCheck( below.Value.IsTable, "table expected" );
				mt = below.Value as LuaTable;
			}

			switch( addr.Value.LuaType )
			{
				case LuaType.LUA_TTABLE:
				{
					LuaTable tbl = addr.Value as LuaTable;
					tbl.MetaTable = mt;
					break;
				}
				case LuaType.LUA_TUSERDATA:
				{
					LuaUserData ud = addr.Value as LuaUserData;
					ud.MetaTable = mt;
					break;
				}
				default:
				{
					G.MetaTables[(int)addr.Value.LuaType] = mt;
					break;
				}
			}
			Top.Index -= 1;
			return true;
		}

		void ILuaAPI.GetGlobal( string name )
		{
			var gt = G.Registry.GetInt( LuaDef.LUA_RIDX_GLOBALS );
			var s = new LuaString( name );
			var below = Top;
			Top.ValueInc = s;
			V_GetTable( gt, s, below );
		}

		void ILuaAPI.SetGlobal( string name )
		{
			var gt = G.Registry.GetInt( LuaDef.LUA_RIDX_GLOBALS );
			var s = new LuaString( name );
			Top.ValueInc = s;
			V_SetTable( gt, s, (Top-2) );
			Top -= 2;
		}

		string ILuaAPI.ToString( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;

			LuaString s = addr.Value as LuaString;
			if( s == null )
				return addr.Value.ToLiteral();
			else
				return s.Value;
		}

		double ILuaAPI.ToNumberX( int index, out bool isnum )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
			{
				isnum = false;
				return 0.0;
			}

			return addr.Value.ToNumber(out isnum);
		}

		double ILuaAPI.ToNumber( int index )
		{
			bool isnum;
			return API.ToNumberX( index, out isnum );
		}

		int ILuaAPI.ToIntegerX( int index, out bool isnum )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
			{
				isnum = false;
				return 0;
			}

			return (int)addr.Value.ToNumber( out isnum );
		}

		int ILuaAPI.ToInteger( int index )
		{
			bool isnum;
			return API.ToIntegerX( index, out isnum );
		}

		uint ILuaAPI.ToUnsignedX( int index, out bool isnum )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
			{
				isnum = false;
				return 0;
			}

			return (uint)addr.Value.ToNumber( out isnum );
		}

		uint ILuaAPI.ToUnsigned( int index )
		{
			bool isnum;
			return API.ToUnsignedX( index, out isnum );
		}

		bool ILuaAPI.ToBoolean( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return false;

			if( addr.Value as LuaNil != null )
				return false;

			var b = addr.Value as LuaBoolean;
			return ( b == null ) || b.Value;
		}

		object ILuaAPI.ToObject( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;

			return (object)addr.Value;
		}

		object ILuaAPI.ToUserData( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;

			switch( addr.Value.LuaType ) {
				case LuaType.LUA_TUSERDATA:
					throw new System.NotImplementedException();
				case LuaType.LUA_TLIGHTUSERDATA: {
					var ud = addr.Value as LuaLightUserData;
					return (ud != null) ? ud.Value : null;
				}
				case LuaType.LUA_TUINT64: {
					var ud = addr.Value as LuaUInt64;
					return (ud != null) ? (object)ud.Value : null;
				}
				default:
					return null;
			}
		}

		ILuaState ILuaAPI.ToThread( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;
			return addr.Value.IsThread ? addr.Value as ILuaState : null;
		}

		private bool Index2Addr( int index, out StkId addr )
		{
			CallInfo ci = CI;
			if( index > 0 )
			{
				addr = ci.Func + index;
				Utl.ApiCheck( index <= ci.Top.Index - (ci.Func.Index + 1), "unacceptable index" );
				if( addr.Index >= Top.Index )
					return false;
				else
					return true;
			}
			else if( index > LuaDef.LUA_REGISTRYINDEX )
			{
				Utl.ApiCheck( index != 0 && -index <= Top.Index - (ci.Func.Index + 1), "invalid index" );
				addr = Top + index;
				return true;
			}
			else if( index == LuaDef.LUA_REGISTRYINDEX )
			{
				addr = new StkId( G.Registry );
				return true;
			}
			// upvalues
			else
			{
				index = LuaDef.LUA_REGISTRYINDEX - index;
				Utl.ApiCheck( index <= LuaLimits.MAXUPVAL + 1, "upvalue index too large" );
				LuaCSharpClosure ccl = ci.Func.Value as LuaCSharpClosure;
				if( ccl != null && (index <= ccl.Upvals.Count) )
				{
					addr = new StkId( ccl.Upvals[index-1] );
					return true;
				}
				else
				{
					addr = default(StkId);
					return false;
				}
			}
		}

		// private void RegisterGlobalFunc( string name, CSharpFunctionDelegate f )
		// {
		// 	API.PushCSharpFunction( f );
		// 	API.SetGlobal( name );
		// }

	}

}


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

		bool CheckStack( int size );
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
		void GetTable( int index );
		void SetTable( int index );

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
		int		ToInteger( int index );
		uint	ToUnsignedX( int index, out bool isnum );
		uint	ToUnsigned( int index );
		bool   	ToBoolean( int index );
		UInt64	ToUInt64( int index );
		UInt64	ToUInt64X( int index, out bool isnum );
		object 	ToObject( int index );
		object  ToUserData( int index );
		ILuaState	ToThread( int index );

		ThreadStatus	Status { get; }

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

	internal delegate void PFuncDelegate<T>(ref T ud);

	public partial class LuaState : ILuaState
	{
		LuaState ILuaAPI.NewThread()
		{
			LuaState newLua = new LuaState(G);
			Top.V.SetThValue(newLua);
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
			public LuaState		L;
			public ILoadInfo 	LoadInfo;
			public string 		Name;
			public string 		Mode;

			public LoadParameter(LuaState l, ILoadInfo loadinfo, string name, string mode)
			{
				L			= l;
				LoadInfo 	= loadinfo;
				Name		= name;
				Mode		= mode;
			}
		}

		private void CheckMode( string given, string expected )
		{
			if( given != null && given.IndexOf(expected[0]) == -1 )
			{
				O_PushString( string.Format(
					"attempt to load a {0} chunk (mode is '{1}')",
					expected, given ) );
				D_Throw( ThreadStatus.LUA_ERRSYNTAX );
			}
		}

		private static void F_Load(ref LoadParameter param)
		{
			var L = param.L;

			LuaProto proto;
			var c = param.LoadInfo.PeekByte();
			if( c == LuaConf.LUA_SIGNATURE[0] )
			{
				L.CheckMode( param.Mode, "binary" );
				proto = Undump.LoadBinary(L, param.LoadInfo, param.Name);
			}
			else
			{
				L.CheckMode( param.Mode, "text" );
				proto = Parser.Parse(L, param.LoadInfo, param.Name);
			}

			var cl = new LuaLClosureValue( proto );
			Utl.Assert(cl.Upvals.Length == cl.Proto.Upvalues.Count);

			L.Top.V.SetClLValue(cl);
			L.IncrTop();
		}
		private static PFuncDelegate<LoadParameter> DG_F_Load = F_Load;

		ThreadStatus ILuaAPI.Load( ILoadInfo loadinfo, string name, string mode )
		{
			var param  = new LoadParameter(this, loadinfo, name, mode);
			var status = D_PCall( DG_F_Load, ref param, Top.Index, ErrFunc );

			if( status == ThreadStatus.LUA_OK ) {
				var below = Stack[Top.Index-1];
				Utl.Assert(below.V.TtIsFunction() && below.V.ClIsLuaClosure());
				var cl = below.V.ClLValue();
				if(cl.Upvals.Length == 1) {
					var gt = G.Registry.V.HValue().GetInt(LuaDef.LUA_RIDX_GLOBALS);
					cl.Upvals[0].V.V.SetObj(ref gt.V);
				}
			}

			return status;
		}

		DumpStatus ILuaAPI.Dump( LuaWriter writeFunc )
		{
			Utl.ApiCheckNumElems( this, 1 );

			var below = Stack[Top.Index-1];
			if(!below.V.TtIsFunction() || !below.V.ClIsLuaClosure())
				return DumpStatus.ERROR;

			var o = below.V.ClLValue();
			if(o == null)
				return DumpStatus.ERROR;

			return DumpState.Dump(o.Proto, writeFunc, false);
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
			var func = Stack[Top.Index - (numArgs+1)];

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
			public LuaState L;
			public int FuncIndex;
			public int NumResults;
		}

		private static void F_Call(ref CallS ud)
		{
			CallS c = (CallS)ud;
			c.L.D_Call(c.L.Stack[c.FuncIndex], c.NumResults, false);
		}
		private static PFuncDelegate<CallS> DG_F_Call = F_Call;

		private void CheckResults( int numArgs, int numResults )
		{
			Utl.ApiCheck( numResults == LuaDef.LUA_MULTRET ||
				CI.TopIndex - Top.Index >= numResults - numArgs,
				"results from function overflow current stack size" );
		}

		private void AdjustResults( int numResults )
		{
			if( numResults == LuaDef.LUA_MULTRET &&
				CI.TopIndex < Top.Index )
			{
				CI.TopIndex = Top.Index;
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
			c.L = this;
			c.FuncIndex = Top.Index - (numArgs + 1);
			if( continueFunc == null || NumNonYieldable > 0 ) // no continuation or no yieldable?
			{
				c.NumResults = numResults;
				status = D_PCall( DG_F_Call, ref c, c.FuncIndex, func );
			}
			else
			{
				int ciIndex = CI.Index;
				CI.ContinueFunc = continueFunc;
				CI.Context		= context;
				CI.ExtraIndex	= c.FuncIndex;
				CI.OldAllowHook	= AllowHook;
				CI.OldErrFunc	= ErrFunc;
				ErrFunc = func;
				CI.CallStatus |= CallStatus.CIST_YPCALL;

				D_Call( Stack[c.FuncIndex], numResults, true );

				CallInfo ci = BaseCI[ciIndex];
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
			D_PosCall( Top.Index-n );
		}

		private void Unroll()
		{
			while( true )
			{
				if( CI.Index == 0 ) // stack is empty?
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
		struct UnrollParam
		{
			public LuaState L;
		}
		private static void UnrollWrap(ref UnrollParam param)
		{
			param.L.Unroll();
		}
		private static PFuncDelegate<UnrollParam> DG_Unroll = UnrollWrap;

		private void ResumeError( string msg, int firstArg )
		{
			Top = Stack[firstArg];
			Top.V.SetSValue(msg);
			IncrTop();
			D_Throw( ThreadStatus.LUA_RESUME_ERROR );
		}

		// check whether thread has suspended protected call
		private CallInfo FindPCall()
		{
			for(int i=CI.Index; i>=0; --i) {
				var ci = BaseCI[i];
				if( (ci.CallStatus & CallStatus.CIST_YPCALL) != 0 )
					{ return ci; }
			}
			return null; // no pending pcall
		}

		private bool Recover( ThreadStatus status )
		{
			CallInfo ci = FindPCall();
			if( ci == null ) // no recover point
				return false;

			StkId oldTop = Stack[ci.ExtraIndex];
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
		private void Resume(int firstArg)
		{
			int numCSharpCalls = NumCSharpCalls;
			CallInfo ci = CI;
			if( numCSharpCalls >= LuaLimits.LUAI_MAXCCALLS )
				ResumeError( "C stack overflow", firstArg );
			if( Status == ThreadStatus.LUA_OK ) // may be starting a coroutine
			{
				if( ci.Index > 0 ) // not in base level
				{
					ResumeError( "cannot resume non-suspended coroutine", firstArg );
				}
				if( !D_PreCall( Stack[firstArg-1], LuaDef.LUA_MULTRET ) ) // Lua function?
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
				ci.FuncIndex = ci.ExtraIndex;
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
						firstArg = Top.Index - n; // yield results come from continuation
					}
					D_PosCall(firstArg);
				}
				Unroll();
			}
			Utl.Assert( numCSharpCalls == NumCSharpCalls );
		}
		struct ResumeParam
		{
			public LuaState L;
			public int firstArg;
		}
		private static void ResumeWrap(ref ResumeParam param)
		{
			param.L.Resume(param.firstArg);
		}
		private static PFuncDelegate<ResumeParam> DG_Resume = ResumeWrap;

		ThreadStatus ILuaAPI.Resume( ILuaState from, int numArgs )
		{
			LuaState fromState = from as LuaState;
			NumCSharpCalls = (fromState != null) ? fromState.NumCSharpCalls + 1 : 1;
			NumNonYieldable = 0; // allow yields

			Utl.ApiCheckNumElems( this, (Status == ThreadStatus.LUA_OK) ? numArgs + 1 : numArgs );

			var resumeParam = new ResumeParam();
			resumeParam.L = this;
			resumeParam.firstArg = Top.Index-numArgs;
			ThreadStatus status = D_RawRunProtected( DG_Resume, ref resumeParam );
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
						var unrollParam = new UnrollParam();
						unrollParam.L = this;
						status = D_RawRunProtected( DG_Unroll, ref unrollParam );
					}
					else // unrecoverable error
					{
						Status = status; // mark thread as `dead'
						SetErrorObj( status, Top );
						CI.TopIndex = Top.Index;
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
			ci.ExtraIndex = ci.FuncIndex; // save current `func'
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
				ci.FuncIndex = Top.Index - (numResults + 1);
				D_Throw( ThreadStatus.LUA_YIELD );
			}
			Utl.Assert( (ci.CallStatus & CallStatus.CIST_HOOKED) != 0 ); // must be inside a hook
			return 0;
		}
		

		int ILuaAPI.AbsIndex( int index )
		{
			return (index > 0 || index <= LuaDef.LUA_REGISTRYINDEX)
				 ? index
				 : Top.Index - CI.FuncIndex + index;
		}

		int ILuaAPI.GetTop()
		{
			return Top.Index - (CI.FuncIndex + 1);
		}

		void ILuaAPI.SetTop( int index )
		{
			if( index >= 0 )
			{
				Utl.ApiCheck(index <= StackLast-(CI.FuncIndex+1), "new top too large");
				int newTop = CI.FuncIndex+1+index;
				for(int i=Top.Index; i<newTop; ++i)
					{ Stack[i].V.SetNilValue(); }
				Top = Stack[newTop];
			}
			else
			{
				Utl.ApiCheck( -(index+1) <= (Top.Index - (CI.FuncIndex + 1)), "invalid new top" );
				Top = Stack[Top.Index + index + 1];
			}
		}

		void ILuaAPI.Remove( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			for(int i=addr.Index+1; i<Top.Index; ++i)
				{ Stack[i-1].V.SetObj(ref Stack[i].V); }

			Top = Stack[Top.Index-1];
		}

		void ILuaAPI.Insert( int index )
		{
			StkId p;
			if( !Index2Addr( index, out p ) )
				Utl.InvalidIndex();

			int i = Top.Index;
			while(i > p.Index) {
				Stack[i].V.SetObj( ref Stack[i-1].V );
				i--;
			}
			p.V.SetObj(ref Top.V);
		}

		private void MoveTo( StkId fr, int index )
		{
			StkId to;
			if( !Index2Addr( index, out to ) )
				Utl.InvalidIndex();

			to.V.SetObj(ref fr.V);
		}

		void ILuaAPI.Replace( int index )
		{
			Utl.ApiCheckNumElems( this, 1 );
			MoveTo( Stack[Top.Index-1], index );
			Top = Stack[Top.Index-1];
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
			Utl.ApiCheck( toLua.CI.TopIndex - toLua.Top.Index >= n, "not enough elements to move" );

			int index = Top.Index-n;
			Top = Stack[index];
			for(int i=0; i<n; ++i)
				{ StkId.inc(ref toLua.Top).V.SetObj(ref Stack[index+i].V); }
		}

		private void GrowStack(int size)
		{
			D_GrowStack(size);
		}
		struct GrowStackParam
		{
			public LuaState L;
			public int size;
		}
		private static void GrowStackWrap(ref GrowStackParam param)
		{
			param.L.GrowStack(param.size);
		}
		private static PFuncDelegate<GrowStackParam> DG_GrowStack = GrowStackWrap;

		bool ILuaAPI.CheckStack(int size)
		{
			bool res = false;

			if(StackLast - Top.Index > size)
				{ res = true; }
			// need to grow stack
			else {
				int inuse = Top.Index + LuaDef.EXTRA_STACK;
				if(inuse > LuaConf.LUAI_MAXSTACK - size)
					res = false;
				else {
					var param = new GrowStackParam();
					param.L = this;
					param.size = size;
					res = D_RawRunProtected(DG_GrowStack, ref param)==ThreadStatus.LUA_OK;
				}
			}

			if(res && CI.TopIndex < Top.Index + size)
				CI.TopIndex = Top.Index + size; // adjust frame top

			return res;
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

		private string AuxUpvalue(StkId addr, int n, out StkId val)
		{
			val = null;

			if(!addr.V.TtIsFunction())
				return null;

			if(addr.V.ClIsLuaClosure()) {
				var f = addr.V.ClLValue();
				var p = f.Proto;
				if(!(1 <= n && n <= p.Upvalues.Count))
					return null;
				val = f.Upvals[n-1].V;
				var name = p.Upvalues[n-1].Name;
				return (name == null) ? "" : name;
			}
			else if(addr.V.ClIsCsClosure()) {
				var f = addr.V.ClCsValue();
				if(!(1 <= n && n <= f.Upvals.Length))
					return null;
				val = f.Upvals[n-1];
				return "";
			}
			else return null;
		}

		string ILuaAPI.GetUpvalue( int funcIndex, int n )
		{
			StkId addr;
			if( !Index2Addr( funcIndex, out addr ) )
				return null;

			StkId val;
			var name = AuxUpvalue(addr, n, out val);
			if(name == null)
				return null;

			Top.V.SetObj(ref val.V);
			ApiIncrTop();
			return name;
		}

		string ILuaAPI.SetUpvalue( int funcIndex, int n )
		{
			StkId addr;
			if( !Index2Addr( funcIndex, out addr ) )
				return null;

			Utl.ApiCheckNumElems( this, 1 );

			StkId val;
			var name = AuxUpvalue(addr, n, out val);
			if(name == null)
				return null;

			Top = Stack[Top.Index-1];
			val.V.SetObj(ref Top.V);
			return name;
		}

		void ILuaAPI.CreateTable( int narray, int nrec )
		{
			var tbl = new LuaTable(this);
			Top.V.SetHValue(tbl);
			ApiIncrTop();
			if(narray > 0 || nrec > 0)
				{ tbl.Resize(narray, nrec); }
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

			var tbl = addr.V.HValue();
			if( tbl == null )
				throw new System.Exception("table expected");

			var key = Stack[Top.Index-1];
			if( tbl.Next( key, Top ) )
			{
				ApiIncrTop();
				return true;
			}
			else
			{
				Top = Stack[Top.Index-1];
				return false;
			}
		}

		void ILuaAPI.RawGetI( int index, int n )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.ApiCheck( false, "table expected" );

			var tbl = addr.V.HValue();
			Utl.ApiCheck( tbl != null, "table expected" );

			Top.V.SetObj(ref tbl.GetInt(n).V);
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

			if(!addr.V.TtIsTable())
				throw new System.Exception("table expected");

			var tbl = addr.V.HValue();
			var below = Stack[Top.Index-1];
			below.V.SetObj( ref tbl.Get( ref below.V ).V );
		}

		void ILuaAPI.RawSetI( int index, int n )
		{
			Utl.ApiCheckNumElems( this, 1 );
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();
			Utl.ApiCheck( addr.V.TtIsTable(), "table expected" );
			var tbl = addr.V.HValue();
			tbl.SetInt( n, ref Stack[Top.Index-1].V );
			Top = Stack[Top.Index-1];
		}

		void ILuaAPI.RawSet( int index )
		{
			Utl.ApiCheckNumElems( this, 2 );
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();
			Utl.ApiCheck( addr.V.TtIsTable(), "table expected" );
			var tbl = addr.V.HValue();
			tbl.Set( ref Stack[Top.Index-2].V, ref Stack[Top.Index-1].V );
			Top = Stack[Top.Index-2];
		}

		void ILuaAPI.GetField( int index, string key )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			Top.V.SetSValue(key);
			var below = Top;
			ApiIncrTop();
			V_GetTable( addr, below, below );
		}

		void ILuaAPI.SetField( int index, string key )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			StkId.inc(ref Top).V.SetSValue( key );
			V_SetTable( addr, Stack[Top.Index-1], Stack[Top.Index-2] );
			Top = Stack[Top.Index-2];
		}

		void ILuaAPI.GetTable( int index )
		{
			StkId addr;
			if(! Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			var below = Stack[Top.Index - 1];
			V_GetTable( addr, below, below );
		}

		void ILuaAPI.SetTable( int index )
		{
			StkId addr;
			Utl.ApiCheckNumElems( this, 2 );
			if(! Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			var key = Stack[Top.Index - 2];
			var val = Stack[Top.Index - 1];
			V_SetTable( addr, key, val);
			Top = Stack[Top.Index-2];
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
				Top.V.SetSValue("");
				ApiIncrTop();
			}
		}

		LuaType ILuaAPI.Type( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return LuaType.LUA_TNONE;

			return (LuaType)addr.V.Tt;
		}

		internal static string TypeName( LuaType t )
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
					return "UInt64";

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

		string ILuaAPI.TypeName( LuaType t )
		{
			return TypeName(t);
		}

		internal string ObjTypeName( ref TValue v )
		{
			return TypeName((LuaType)v.Tt);
		}

		// 用于内部使用 不会因为 ApiIncrTop() 检查 Top 超过 CI.Top 报错
		internal void O_PushString( string s )
		{
			Top.V.SetSValue(s);
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
				case LuaEq.LUA_OPEQ: return EqualObj( ref addr1.V, ref addr2.V, false );
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

			return V_RawEqualObj( ref addr1.V, ref addr2.V );
		}

		int ILuaAPI.RawLen( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			switch( addr.V.Tt )
			{
				case (int)LuaType.LUA_TSTRING:
				{
					var s = addr.V.SValue();
					return s == null ? 0 : s.Length;
				}
				case (int)LuaType.LUA_TUSERDATA:
				{
					return addr.V.RawUValue().Length;
				}
				case (int)LuaType.LUA_TTABLE:
				{
					return addr.V.HValue().Length;
				}
				default: return 0;
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
			Top.V.SetNilValue();
			ApiIncrTop();
		}

		void ILuaAPI.PushBoolean( bool b )
		{
			Top.V.SetBValue( b );
			ApiIncrTop();
		}

		void ILuaAPI.PushNumber( double n )
		{
			Top.V.SetNValue( n );
			ApiIncrTop();
		}

		void ILuaAPI.PushInteger( int n )
		{
			Top.V.SetNValue( (double)n );
			ApiIncrTop();
		}

		void ILuaAPI.PushUnsigned( uint n )
		{
			Top.V.SetNValue( (double)n );
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
				Top.V.SetSValue(s);
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
				Top.V.SetClCsValue( new LuaCsClosureValue( f ) );
			}
			else
			{
				// 带 UpValue 的 C# function
				Utl.ApiCheckNumElems( this, n );
				Utl.ApiCheck( n <= LuaLimits.MAXUPVAL, "upvalue index too large" );

				LuaCsClosureValue cscl = new LuaCsClosureValue( f, n );
				int index = Top.Index - n;
				Top = Stack[index];
				for(int i=0; i<n; ++i)
					{ cscl.Upvals[i].V.SetObj( ref Stack[index+i].V ); }

				Top.V.SetClCsValue( cscl );
			}
			ApiIncrTop();
		}

		void ILuaAPI.PushValue( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				Utl.InvalidIndex();

			Top.V.SetObj(ref addr.V);
			ApiIncrTop();
		}

		void ILuaAPI.PushGlobalTable()
		{
			API.RawGetI( LuaDef.LUA_REGISTRYINDEX, LuaDef.LUA_RIDX_GLOBALS );
		}

		void ILuaAPI.PushLightUserData( object o )
		{
			Top.V.SetPValue( o );
			ApiIncrTop();
		}

		void ILuaAPI.PushUInt64( UInt64 o )
		{
			Top.V.SetUInt64Value( o );
			ApiIncrTop();
		}

		bool ILuaAPI.PushThread()
		{
			Top.V.SetThValue(this);
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

			LuaTable mt;
			switch( addr.V.Tt )
			{
				case (int)LuaType.LUA_TTABLE:
				{
					var tbl = addr.V.HValue();
					mt = tbl.MetaTable;
					break;
				}
				case (int)LuaType.LUA_TUSERDATA:
				{
					var ud = addr.V.RawUValue();
					mt = ud.MetaTable;
					break;
				}
				default:
				{
					mt = G.MetaTables[addr.V.Tt];
					break;
				}
			}
			if( mt == null )
				return false;
			else
			{
				Top.V.SetHValue( mt );
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

			var below = Stack[Top.Index - 1];
			LuaTable mt;
			if( below.V.TtIsNil() )
				mt = null;
			else
			{
				Utl.ApiCheck( below.V.TtIsTable(), "table expected" );
				mt = below.V.HValue();
			}

			switch( addr.V.Tt )
			{
				case (int)LuaType.LUA_TTABLE:
				{
					var tbl = addr.V.HValue();
					tbl.MetaTable = mt;
					break;
				}
				case (int)LuaType.LUA_TUSERDATA:
				{
					var ud = addr.V.RawUValue();
					ud.MetaTable = mt;
					break;
				}
				default:
				{
					G.MetaTables[addr.V.Tt] = mt;
					break;
				}
			}
			Top = Stack[Top.Index - 1];
			return true;
		}

		void ILuaAPI.GetGlobal( string name )
		{
			var gt = G.Registry.V.HValue().GetInt( LuaDef.LUA_RIDX_GLOBALS );
			StkId.inc(ref Top).V.SetSValue(name);
			V_GetTable(gt, Stack[Top.Index-1], Stack[Top.Index-1]);
		}

		void ILuaAPI.SetGlobal( string name )
		{
			var gt = G.Registry.V.HValue().GetInt( LuaDef.LUA_RIDX_GLOBALS );
			StkId.inc(ref Top).V.SetSValue(name);
			V_SetTable(gt, Stack[Top.Index-1], Stack[Top.Index-2]);
			Top = Stack[Top.Index-2];
		}

		string ILuaAPI.ToString( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;

			if(addr.V.TtIsString()) {
				return addr.V.OValue as string;
			}

			if(!V_ToString(ref addr.V)) {
				return null;
			}

			if( !Index2Addr( index, out addr ) )
				return null;

			Utl.Assert(addr.V.TtIsString());
			return addr.V.OValue as string;
		}

		double ILuaAPI.ToNumberX( int index, out bool isnum )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
			{
				isnum = false;
				return 0.0;
			}

			if(addr.V.TtIsNumber()) {
				isnum = true;
				return addr.V.NValue;
			}

			if(addr.V.TtIsString()) {
				var n = new TValue();
				if(V_ToNumber(addr, ref n)) {
					isnum = true;
					return n.NValue;
				}
			}

			isnum = false;
			return 0;
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

			if(addr.V.TtIsNumber()) {
				isnum = true;
				return (int)addr.V.NValue;
			}

			if(addr.V.TtIsString()) {
				var n = new TValue();
				if(V_ToNumber(addr, ref n)) {
					isnum = true;
					return (int)n.NValue;
				}
			}

			isnum = false;
			return 0;
		}

		int ILuaAPI.ToInteger( int index )
		{
			bool isnum;
			return API.ToIntegerX( index, out isnum );
		}

		uint ILuaAPI.ToUnsignedX( int index, out bool isnum )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) ) {
				isnum = false;
				return 0;
			}

			if( addr.V.TtIsNumber() ) {
				isnum = true;
				return (uint)addr.V.NValue;
			}

			if( addr.V.TtIsString() ) {
				var n = new TValue();
				if(V_ToNumber(addr, ref n)) {
					isnum = true;
					return (uint)n.NValue;
				}
			}

			isnum = false;
			return 0;
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

			return !IsFalse(ref addr.V);
		}

		UInt64 ILuaAPI.ToUInt64X( int index, out bool isnum )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) ) {
				isnum = false;
				return 0;
			}

			if( !addr.V.TtIsUInt64() ) {
				isnum = false;
				return 0;
			}

			isnum = true;
			return addr.V.UInt64Value;
		}

		UInt64 ILuaAPI.ToUInt64( int index )
		{
			bool isnum;
			return API.ToUInt64X( index, out isnum );
		}

		object ILuaAPI.ToObject( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;

			return addr.V.OValue;
		}

		object ILuaAPI.ToUserData( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;

			switch(addr.V.Tt) {
				case (int)LuaType.LUA_TUSERDATA:
					throw new System.NotImplementedException();
				case (int)LuaType.LUA_TLIGHTUSERDATA: { return addr.V.OValue; }
				case (int)LuaType.LUA_TUINT64: { return addr.V.UInt64Value; }
				default: return null;
			}
		}

		ILuaState ILuaAPI.ToThread( int index )
		{
			StkId addr;
			if( !Index2Addr( index, out addr ) )
				return null;
			return addr.V.TtIsThread() ? addr.V.OValue as ILuaState : null;
		}

		private bool Index2Addr( int index, out StkId addr )
		{
			CallInfo ci = CI;
			if( index > 0 )
			{
				var addrIndex = ci.FuncIndex + index;
				Utl.ApiCheck( index <= ci.TopIndex - (ci.FuncIndex + 1), "unacceptable index" );
				if( addrIndex >= Top.Index ) {
					addr = default(StkId);
					return false;
				}

				addr = Stack[addrIndex];
				return true;
			}
			else if( index > LuaDef.LUA_REGISTRYINDEX )
			{
				Utl.ApiCheck( index != 0 && -index <= Top.Index - (ci.FuncIndex + 1), "invalid index" );
				addr = Stack[Top.Index + index];
				return true;
			}
			else if( index == LuaDef.LUA_REGISTRYINDEX )
			{
				addr = G.Registry;
				return true;
			}
			// upvalues
			else
			{
				index = LuaDef.LUA_REGISTRYINDEX - index;
				Utl.ApiCheck( index <= LuaLimits.MAXUPVAL + 1, "upvalue index too large" );
				var func = Stack[ci.FuncIndex];
				Utl.Assert(func.V.TtIsFunction());

				if(func.V.ClIsLcsClosure()) {
					addr = default(StkId);
					return false;
				}

				Utl.Assert(func.V.ClIsCsClosure());
				var clcs = func.V.ClCsValue();
				if(index > clcs.Upvals.Length) {
					addr = default(StkId);
					return false;
				}

				addr = clcs.Upvals[index-1];
				return true;
			}
		}

		// private void RegisterGlobalFunc( string name, CSharpFunctionDelegate f )
		// {
		// 	API.PushCSharpFunction( f );
		// 	API.SetGlobal( name );
		// }

	}

}


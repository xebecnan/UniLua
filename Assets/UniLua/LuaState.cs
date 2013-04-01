
// #define ENABLE_DUMP_STACK

// #define DEBUG_RECORD_INS

using System.Collections.Generic;

namespace UniLua
{
	using InstructionPtr = Pointer<Instruction>;
	using Debug = UniLua.Tools.Debug;

	public struct Pointer<T>
	{
		private List<T> 	List;
		public  int 		Index { get; set; }

		public  T			Value
		{
			get
			{
				return List[Index];
			}
			set
			{
				List[Index] = value;
			}
		}

		public T			ValueInc
		{
			get
			{
				return List[Index++];
			}
			set
			{
				List[Index++] = value;
			}
		}

		public Pointer( List<T> list, int index )
            : this()
		{
			List = list;
		    Index = index;
		}

		public Pointer( Pointer<T> other )
            : this()
		{
			List = other.List;
			Index = other.Index;
		}

		public static Pointer<T> operator +( Pointer<T> lhs, int rhs )
		{
			return new Pointer<T>( lhs.List, lhs.Index + rhs );
		}

		public static Pointer<T> operator -( Pointer<T> lhs, int rhs )
		{
			return new Pointer<T>( lhs.List, lhs.Index - rhs );
		}

		// private /////////////////////////////////////////
	}

	public struct StkId
	{
		private LuaObject			IsolateValue;

		private List<LuaObject> 	List;
		public  int 				Index { get; set; }

		public  LuaObject			Value
		{
			get
			{
				if( IsolateValue != null )
					return IsolateValue;

				EnsureStack();
				return List[Index];
			}
			set
			{
				if( IsolateValue != null )
					throw new System.NotImplementedException();

				EnsureStack();
				List[Index] = value;
			}
		}

		public  LuaObject			ValueInc
		{
			get
			{
				if( IsolateValue != null )
					throw new System.NotImplementedException();

				EnsureStack();
				return List[Index++];
			}
			set
			{
				if( IsolateValue != null )
					throw new System.NotImplementedException();

				EnsureStack();
				List[Index++] = value;
			}
		}

		public bool IsNull
		{
			get { return List == null && IsolateValue == null; }
		}

		public StkId( List<LuaObject> list, int index )
            : this()
		{
			List = list;
			Index = index;
		}

		public StkId( StkId other ) : this( other.List, other.Index ) { }

		public StkId( LuaObject val ) : this( null, 0 ) {
			IsolateValue = val;
		}

		public static StkId operator +( StkId lhs, int rhs )
		{
			if( lhs.IsolateValue != null )
				throw new System.NotImplementedException();

			return new StkId( lhs.List, lhs.Index + rhs );
		}

		public static StkId operator -( StkId lhs, int rhs )
		{
			if( lhs.IsolateValue != null )
				throw new System.NotImplementedException();

			return new StkId( lhs.List, lhs.Index - rhs );
		}

		public static bool operator ==( StkId lhs, StkId rhs )
		{
			return lhs.Equals( rhs );
		}

		public static bool operator !=( StkId lhs, StkId rhs )
		{
			return !lhs.Equals( rhs );
		}

		public override bool Equals( object o )
		{
			if( !(o is StkId) )
				return false;

			return Equals((StkId)o);
		}

		public bool Equals( StkId o )
		{
			return (this.IsolateValue 	== o.IsolateValue)
				&& (this.List			== o.List)
				&& (this.Index			== o.Index);
		}

		public override int GetHashCode()
		{
			return ((IsolateValue != null) ? IsolateValue.GetHashCode() : 0)
				 ^ ((List != null) ? List.GetHashCode() : 0)
				 ^ (Index.GetHashCode());
		}

		public override string ToString()
		{
			if( IsolateValue != null )
				return string.Format( "[StkId(Isolate/{0})]", IsolateValue );
			else
				return string.Format( "[StkId({0}/{1})]", Index, Value );
		}

		// private /////////////////////////////////////////

		private void EnsureStack()
		{
			while( Index >= List.Count )
			{
				List.Add( new LuaNil() );
			}
		}
	}


	public enum CallStatus
	{
		CIST_NONE		= 0,

		CIST_LUA		= (1<<0),	/* call is running a Lua function */
		CIST_HOOKED		= (1<<1),	/* call is running a debug hook */
		CIST_REENTRY	= (1<<2),	/* call is running on same invocation of
		                           	   luaV_execute of previous call */
		CIST_YIELDED	= (1<<3),	/* call reentered after suspension */
		CIST_YPCALL		= (1<<4),	/* call is a yieldable protected call */
		CIST_STAT		= (1<<5),	/* call has an error status (pcall) */
		CIST_TAIL		= (1<<6),	/* call was tail called */
	}

	public class CallInfo
	{
		public StkId Func;
		public StkId Top;

		public CallInfo Previous;
		public CallInfo Next;

		public int NumResults;
		public CallStatus CallStatus;

		public CSharpFunctionDelegate ContinueFunc;
		public int Context;
		public StkId Extra;
		public bool OldAllowHook;
		public int OldErrFunc;
		public ThreadStatus Status;

		// for Lua functions
		public StkId Base;
		public InstructionPtr SavedPc;

		public bool IsLua
		{
			get { return (CallStatus & CallStatus.CIST_LUA) != 0; }
		}

		public LuaLClosure CurrentLuaFunc
		{
			get
			{
				if(IsLua)
				{
					return Func.Value as LuaLClosure;
				}
				else return null;
			}
		}

		public int CurrentLine
		{
			get
			{
				var lcl = Func.Value as LuaLClosure;
				return lcl.Proto.GetFuncLine( CurrentPc );
			}
		}

		public int CurrentPc
		{
			get
			{
				Utl.Assert( IsLua );
				return SavedPc.Index - 1;
			}
		}
	}

	public class GlobalState
	{
		public LuaTable		Registry;
		public LuaUpvalue 	UpvalHead;
		public LuaTable[] 	MetaTables;
		public LuaState		MainThread;

		public GlobalState( LuaState state )
		{
			MainThread	= state;
			Registry 	= new LuaTable();
			UpvalHead 	= new LuaUpvalue();
			MetaTables 	= new LuaTable[(int)LuaType.LUA_NUMTAGS];
		}
	}

	public delegate void LuaHookDelegate(ILuaState lua, LuaDebug ar);

	public partial class LuaState : LuaObject
	{
		public StkId 			Top;
		public CallInfo 		CI;
		public CallInfo 		BaseCI;
		public GlobalState		G;
		public int				NumNonYieldable;
		public int				NumCSharpCalls;
		public int				ErrFunc;
		public ThreadStatus		Status { get; set; }
		public bool				AllowHook;
		public byte				HookMask;
		public int				BaseHookCount;
		public int				HookCount;
		public LuaHookDelegate	Hook;

		public LinkedList<LuaUpvalue>	OpenUpval;

#if DEBUG_RECORD_INS
		private Queue<Instruction> InstructionHistory;
#endif


		private ILuaAPI 	API;

		public LuaState( GlobalState g=null )
		{
			API = (ILuaAPI)this;

			NumNonYieldable = 1;
			NumCSharpCalls  = 0;
			Hook			= null;
			HookMask		= 0;
			BaseHookCount	= 0;
			AllowHook		= true;
			ResetHookCount();
			Status			= ThreadStatus.LUA_OK;

			if( g == null )
			{
				G = new GlobalState(this);
				InitRegistry();
			}
			else
			{
				G = g;
			}
			OpenUpval = new LinkedList<LuaUpvalue>();
			ErrFunc   = 0;

#if DEBUG_RECORD_INS
			InstructionHistory = new Queue<Instruction>();
#endif

			InitStack();
		}

		private List<LuaObject> StateStack;

		private void IncrTop()
		{
			++Top.Index;
		}

		private StkId RestoreStack( int index )
		{
			return new StkId( StateStack, index );
		}

		private void ApiIncrTop()
		{
			++Top.Index;
			// Debug.Log( "[ApiIncrTop] ==== Top.Index:" + Top.Index );
			// Debug.Log( "[ApiIncrTop] ==== CI.Top.Index:" + CI.Top.Index );
			Utl.ApiCheck( Top.Index <= CI.Top.Index, "stack overflow" );
		}

		private void InitStack()
		{
			StateStack = new List<LuaObject>();
			Top = new StkId( StateStack, 0 );

			BaseCI = new CallInfo();
			BaseCI.Next = null;
			BaseCI.Previous = null;
			BaseCI.Func = Top;
			Top.ValueInc = new LuaNil(); // `function' entry for this `ci'
			BaseCI.Top = Top + LuaDef.LUA_MINSTACK;
			CI = BaseCI;
		}

		private void InitRegistry()
		{
			G.Registry.SetInt( LuaDef.LUA_RIDX_MAINTHREAD, this );
			G.Registry.SetInt( LuaDef.LUA_RIDX_GLOBALS, new LuaTable() );
		}

		private string DumpStackToString( int baseIndex, string tag="" )
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append( string.Format( "===================================================================== DumpStack: {0}", tag) ).Append("\n");;
			for( int i=0; i<StateStack.Count || i <= Top.Index; ++i )
			{
				bool isTop = Top.Index == i;
				bool isBase = baseIndex == i;
				bool inStack = i < StateStack.Count;

				string postfix = ( isTop || isBase )
					? string.Format( "<--------------------- {0}{1}"
						, isBase ? "[BASE]" : ""
						, isTop  ? "[TOP]"  : ""
						)
					: "";
				string body = string.Format("======== {0}/{1} > {2} {3}"
					, i-baseIndex
					, i
					, inStack ? StateStack[i].ToString() : ""
					, postfix
				);

				sb.Append( body	).Append("\n");
			}
			return sb.ToString();
		}

		private void DumpStack( int baseIndex, string tag="" )
		{
#if ENABLE_DUMP_STACK
			Debug.Log( string.Format( "===================================================================== DumpStack: {0}", tag) );
			for( int i=0; i<StateStack.Count || i <= Top.Index; ++i )
			{
				bool isTop = Top.Index == i;
				bool isBase = baseIndex == i;
				bool inStack = i < StateStack.Count;

				string postfix = ( isTop || isBase )
					? string.Format( "<--------------------- {0}{1}"
						, isBase ? "[BASE]" : ""
						, isTop  ? "[TOP]"  : ""
						)
					: "";
				string body = string.Format("======== {0}/{1} > {2} {3}"
					, i-baseIndex
					, i
					, inStack ? StateStack[i].ToString() : ""
					, postfix
				);

				Debug.Log( body	);
			}
#endif
		}

		private void ResetHookCount()
		{
			HookCount = BaseHookCount;
		}

	}

}


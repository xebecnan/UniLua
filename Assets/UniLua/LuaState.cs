
// #define ENABLE_DUMP_STACK

// #define DEBUG_RECORD_INS

using System.Collections.Generic;

namespace UniLua
{
	using InstructionPtr = Pointer<Instruction>;
	using ULDebug = UniLua.Tools.ULDebug;

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

		public Pointer( List<T> list, int index ) : this()
		{
			List = list;
			Index = index;
		}

		public Pointer( Pointer<T> other ) : this()
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
		public CallInfo[] List;
		public int Index;

		public int FuncIndex;
		public int TopIndex;

		public int NumResults;
		public CallStatus CallStatus;

		public CSharpFunctionDelegate ContinueFunc;
		public int Context;
		public int ExtraIndex;
		public bool OldAllowHook;
		public int OldErrFunc;
		public ThreadStatus Status;

		// for Lua functions
		public int BaseIndex;
		public InstructionPtr SavedPc;

		public bool IsLua
		{
			get { return (CallStatus & CallStatus.CIST_LUA) != 0; }
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
		public StkId		Registry;
		public LuaUpvalue 	UpvalHead;
		public LuaTable[] 	MetaTables;
		public LuaState		MainThread;

		public GlobalState( LuaState state )
		{
			MainThread	= state;
			Registry 	= new StkId();
			UpvalHead 	= new LuaUpvalue();
			MetaTables 	= new LuaTable[(int)LuaType.LUA_NUMTAGS];
		}
	}

	public delegate void LuaHookDelegate(ILuaState lua, LuaDebug ar);

	public partial class LuaState
	{
		public StkId[]			Stack;
		public StkId			Top;
		public int				StackSize;
		public int				StackLast;
		public CallInfo 		CI;
		public CallInfo[] 		BaseCI;
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

		static LuaState()
		{
			TheNilValue = new StkId();
			TheNilValue.V.SetNilValue();
		}

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

		private void IncrTop()
		{
			StkId.inc(ref Top);
			D_CheckStack(0);
		}

		private StkId RestoreStack( int index )
		{
			return Stack[index];
		}

		private void ApiIncrTop()
		{
			StkId.inc(ref Top);
			// ULDebug.Log( "[ApiIncrTop] ==== Top.Index:" + Top.Index );
			// ULDebug.Log( "[ApiIncrTop] ==== CI.Top.Index:" + CI.Top.Index );
			Utl.ApiCheck( Top.Index <= CI.TopIndex, "stack overflow" );
		}

		private void InitStack()
		{
			Stack = new StkId[LuaDef.BASIC_STACK_SIZE];
			StackSize = LuaDef.BASIC_STACK_SIZE;
			StackLast = LuaDef.BASIC_STACK_SIZE - LuaDef.EXTRA_STACK;
			for(int i=0; i<LuaDef.BASIC_STACK_SIZE; ++i) {
				var newItem = new StkId();
				Stack[i] = newItem;
				newItem.SetList(Stack);
				newItem.SetIndex(i);
				newItem.V.SetNilValue();
			}
			Top = Stack[0];

			BaseCI = new CallInfo[LuaDef.BASE_CI_SIZE];
			for(int i=0; i<LuaDef.BASE_CI_SIZE; ++i) {
				var newCI = new CallInfo();
				BaseCI[i] = newCI;
				newCI.List = BaseCI;
				newCI.Index = i;
			}
			CI = BaseCI[0];
			CI.FuncIndex = Top.Index;
			StkId.inc(ref Top).V.SetNilValue(); // `function' entry for this `ci'
			CI.TopIndex = Top.Index + LuaDef.LUA_MINSTACK;
		}

		private void InitRegistry()
		{
			var mt = new TValue();

			G.Registry.V.SetHValue(new LuaTable(this));

			mt.SetThValue(this);
			G.Registry.V.HValue().SetInt(LuaDef.LUA_RIDX_MAINTHREAD, ref mt);

			mt.SetHValue(new LuaTable(this));
			G.Registry.V.HValue().SetInt(LuaDef.LUA_RIDX_GLOBALS, ref mt);
		}

		private string DumpStackToString( int baseIndex, string tag="" )
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append( string.Format( "===================================================================== DumpStack: {0}", tag) ).Append("\n");
			sb.Append( string.Format( "== BaseIndex: {0}", baseIndex) ).Append("\n");
			sb.Append( string.Format( "== Top.Index: {0}", Top.Index) ).Append("\n");
			sb.Append( string.Format( "== CI.Index: {0}", CI.Index) ).Append("\n");
			sb.Append( string.Format( "== CI.TopIndex: {0}", CI.TopIndex) ).Append("\n");
			sb.Append( string.Format( "== CI.Func.Index: {0}", CI.FuncIndex) ).Append("\n");
			for( int i=0; i<Stack.Length || i <= Top.Index; ++i )
			{
				bool isTop = Top.Index == i;
				bool isBase = baseIndex == i;
				bool inStack = i < Stack.Length;

				string postfix = ( isTop || isBase )
					? string.Format( "<--------------------- {0}{1}"
						, isBase ? "[BASE]" : ""
						, isTop  ? "[TOP]"  : ""
						)
					: "";
				string body = string.Format("======== {0}/{1} > {2} {3}"
					, i-baseIndex
					, i
					, inStack ? Stack[i].ToString() : ""
					, postfix
				);

				sb.Append( body	).Append("\n");
			}
			return sb.ToString();
		}

		public void DumpStack( int baseIndex, string tag="" )
		{
#if ENABLE_DUMP_STACK
			ULDebug.Log(DumpStackToString(baseIndex, tag));
#endif
		}

		private void ResetHookCount()
		{
			HookCount = BaseHookCount;
		}

	}

}


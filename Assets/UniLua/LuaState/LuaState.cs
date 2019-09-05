
// #define ENABLE_DUMP_STACK

// #define DEBUG_RECORD_INS

using System.Collections.Generic;

namespace UniLua {
  public partial class LuaState {
    public StkId[] Stack;
    public StkId Top;
    public int StackSize;
    public int StackLast;
    public CallInfo CI;
    public CallInfo[] BaseCI;
    public GlobalState G;
    public int NumNonYieldable;
    public int NumCSharpCalls;
    public int ErrFunc;
    public ThreadStatus Status { get; set; }
    public bool AllowHook;
    public byte HookMask;
    public int BaseHookCount;
    public int HookCount;
    public LuaHookDelegate Hook;

    public LinkedList<LuaUpvalue> OpenUpval;

#if DEBUG_RECORD_INS
		private Queue<Instruction> InstructionHistory;
#endif

    private ILuaAPI API;

    static LuaState() {
      TheNilValue = new StkId();
      TheNilValue.V.SetNilValue();
    }

    public LuaState(GlobalState g = null) {
      API = (ILuaAPI) this;

      NumNonYieldable = 1;
      NumCSharpCalls = 0;
      Hook = null;
      HookMask = 0;
      BaseHookCount = 0;
      AllowHook = true;
      ResetHookCount();
      Status = ThreadStatus.LUA_OK;

      if (g == null) {
        G = new GlobalState(this);
        InitRegistry();
      }
      else {
        G = g;
      }

      OpenUpval = new LinkedList<LuaUpvalue>();
      ErrFunc = 0;

#if DEBUG_RECORD_INS
			InstructionHistory = new Queue<Instruction>();
#endif

      InitStack();
    }

    private void IncrTop() {
      StkId.inc(ref Top);
      D_CheckStack(0);
    }

    private StkId RestoreStack(int index) {
      return Stack[index];
    }

    public void ApiIncrTop() {
      StkId.inc(ref Top);
      // ULDebug.Log( "[ApiIncrTop] ==== Top.Index:" + Top.Index );
      // ULDebug.Log( "[ApiIncrTop] ==== CI.Top.Index:" + CI.Top.Index );
      Utl.ApiCheck(Top.Index <= CI.TopIndex, "stack overflow");
    }

    private void InitStack() {
      Stack = new StkId[LuaDef.BASIC_STACK_SIZE];
      StackSize = LuaDef.BASIC_STACK_SIZE;
      StackLast = LuaDef.BASIC_STACK_SIZE - LuaDef.EXTRA_STACK;
      for (int i = 0; i < LuaDef.BASIC_STACK_SIZE; ++i) {
        var newItem = new StkId();
        Stack[i] = newItem;
        newItem.SetList(Stack);
        newItem.SetIndex(i);
        newItem.V.SetNilValue();
      }

      Top = Stack[0];

      BaseCI = new CallInfo[LuaDef.BASE_CI_SIZE];
      for (int i = 0; i < LuaDef.BASE_CI_SIZE; ++i) {
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

    private void InitRegistry() {
      var mt = new TValue();

      G.Registry.V.SetHValue(new LuaTable(this));

      mt.SetThValue(this);
      G.Registry.V.HValue().SetInt(LuaDef.LUA_RIDX_MAINTHREAD, ref mt);

      mt.SetHValue(new LuaTable(this));
      G.Registry.V.HValue().SetInt(LuaDef.LUA_RIDX_GLOBALS, ref mt);
    }

    private string DumpStackToString(int baseIndex, string tag = "") {
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      sb.Append(string.Format("===================================================================== DumpStack: {0}",
        tag)).Append("\n");
      sb.Append(string.Format("== BaseIndex: {0}", baseIndex)).Append("\n");
      sb.Append(string.Format("== Top.Index: {0}", Top.Index)).Append("\n");
      sb.Append(string.Format("== CI.Index: {0}", CI.Index)).Append("\n");
      sb.Append(string.Format("== CI.TopIndex: {0}", CI.TopIndex)).Append("\n");
      sb.Append(string.Format("== CI.Func.Index: {0}", CI.FuncIndex)).Append("\n");
      for (int i = 0; i < Stack.Length || i <= Top.Index; ++i) {
        bool isTop = Top.Index == i;
        bool isBase = baseIndex == i;
        bool inStack = i < Stack.Length;

        string postfix = (isTop || isBase)
          ? string.Format("<--------------------- {0}{1}"
            , isBase ? "[BASE]" : ""
            , isTop ? "[TOP]" : ""
          )
          : "";
        string body = string.Format("======== {0}/{1} > {2} {3}"
          , i - baseIndex
          , i
          , inStack ? Stack[i].ToString() : ""
          , postfix
        );

        sb.Append(body).Append("\n");
      }

      return sb.ToString();
    }

    public void DumpStack(int baseIndex, string tag = "") {
#if ENABLE_DUMP_STACK
			ULDebug.Log(DumpStackToString(baseIndex, tag));
#endif
    }

    private void ResetHookCount() {
      HookCount = BaseHookCount;
    }

  }
}

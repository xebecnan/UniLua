namespace UniLua {
  public class CallInfo {
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
    public Pointer<Instruction> SavedPc;

    public bool IsLua {
      get { return (CallStatus & CallStatus.CIST_LUA) != 0; }
    }

    public int CurrentPc {
      get {
        Utl.Assert(IsLua);
        return SavedPc.Index - 1;
      }
    }
  }
}
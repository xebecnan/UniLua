namespace UniLua {
  public class GlobalState {
    public StkId Registry;
    public LuaUpvalue UpvalHead;
    public LuaTable[] MetaTables;
    public LuaState MainThread;

    public GlobalState(LuaState state) {
      MainThread = state;
      Registry = new StkId();
      UpvalHead = new LuaUpvalue();
      MetaTables = new LuaTable[(int) LuaType.LUA_NUMTAGS];
    }
  }
}
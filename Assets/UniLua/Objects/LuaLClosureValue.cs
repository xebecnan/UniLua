namespace UniLua {
  public class LuaLClosureValue {
    public LuaProto Proto;
    public LuaUpvalue[] Upvals;

    public LuaLClosureValue(LuaProto p) {
      Proto = p;

      Upvals = new LuaUpvalue[p.Upvalues.Count];
      for (int i = 0; i < p.Upvalues.Count; ++i) {
        Upvals[i] = new LuaUpvalue();
      }
    }
  }
}
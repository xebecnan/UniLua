namespace UniLua {
  public class LuaUpvalue {
    public StkId V;
    public StkId Value;

    public LuaUpvalue() {
      Value = new StkId();
      Value.V.SetNilValue();

      V = Value;
    }
  }
}

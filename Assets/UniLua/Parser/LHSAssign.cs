namespace UniLua {
  public class LHSAssign {
    public LHSAssign Prev;
    public ExpDesc Exp;

    public LHSAssign() {
      Prev = null;
      Exp = new ExpDesc();
    }
  }
}

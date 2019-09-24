namespace UniLua {
  public class NumberToken : TypedToken {
    public double SemInfo;

    public NumberToken(double seminfo) : base(TK.NUMBER) {
      SemInfo = seminfo;
    }

    public override string ToString() {
      return string.Format("NumberToken: {0}", SemInfo);
    }
  }
}

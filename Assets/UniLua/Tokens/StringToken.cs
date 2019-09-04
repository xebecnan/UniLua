namespace UniLua {
  public class StringToken : TypedToken {
    public string SemInfo;

    public StringToken(string seminfo) : base(TK.STRING) {
      SemInfo = seminfo;
    }

    public override string ToString() {
      return string.Format("StringToken: {0}", SemInfo);
    }
  }
}

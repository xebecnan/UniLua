namespace UniLua {
  public class NameToken : TypedToken {
    public string SemInfo;

    public NameToken(string seminfo) : base(TK.NAME) {
      SemInfo = seminfo;
    }

    public override string ToString() {
      return string.Format("NameToken: {0}", SemInfo);
    }
  }
}

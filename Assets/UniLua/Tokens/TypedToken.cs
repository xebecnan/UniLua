namespace UniLua {
  public class TypedToken : Token {
    private TK _Type;

    public TypedToken(TK type) {
      _Type = type;
    }

    public override int TokenType {
      get { return (int) _Type; }
    }

    public override string ToString() {
      return string.Format("TypedToken: {0}", _Type);
    }
  }
}

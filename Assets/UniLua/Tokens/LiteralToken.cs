namespace UniLua {
  public class LiteralToken : Token {
    private int _Literal;

    public LiteralToken(int literal) {
      _Literal = literal;
    }

    public override int TokenType {
      get { return _Literal; }
    }

    public override string ToString() {
      return string.Format("LiteralToken: {0}", _Literal);
    }
  }
}

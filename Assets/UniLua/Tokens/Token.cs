namespace UniLua {
  public abstract class Token {
    public abstract int TokenType { get; }

    public bool EqualsToToken(Token other) {
      return TokenType == other.TokenType;
    }

    public bool EqualsToToken(int other) {
      return TokenType == other;
    }

    public bool EqualsToToken(TK other) {
      return TokenType == (int) other;
    }
  }
}

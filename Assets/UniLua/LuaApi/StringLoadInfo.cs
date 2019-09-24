namespace UniLua {
  public class StringLoadInfo : ILoadInfo {
    public StringLoadInfo(string s) {
      Str = s;
      Pos = 0;
    }

    public int ReadByte() {
      if (Pos >= Str.Length)
        return -1;
      else
        return Str[Pos++];
    }

    public int PeekByte() {
      if (Pos >= Str.Length)
        return -1;
      else
        return Str[Pos];
    }

    private string Str;
    private int Pos;
  }
}
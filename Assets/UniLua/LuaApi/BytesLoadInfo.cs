namespace UniLua {
  class BytesLoadInfo : ILoadInfo {
    public BytesLoadInfo(byte[] bytes) {
      Bytes = bytes;
      Pos = 0;
    }

    public int ReadByte() {
      if (Pos >= Bytes.Length)
        return -1;
      else
        return Bytes[Pos++];
    }

    public int PeekByte() {
      if (Pos >= Bytes.Length)
        return -1;
      else
        return Bytes[Pos];
    }

    private byte[] Bytes;
    private int Pos;
  }
}

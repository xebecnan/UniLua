namespace UniLua {
  public class BlockCnt {
    public BlockCnt Previous;
    public int FirstLabel;
    public int FirstGoto;
    public int NumActVar;
    public bool HasUpValue;
    public bool IsLoop;
  }
}

namespace UniLua {
  public class LabelDesc {
    public string Name; // label identifier
    public int Pc; // position in code
    public int Line; // line where it appear
    public int NumActVar; // local level where it appears in current block
  }
}

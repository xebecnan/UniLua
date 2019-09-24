namespace UniLua {
  public class ConstructorControl {
    public ExpDesc ExpLastItem;
    public ExpDesc ExpTable;
    public int NumRecord;
    public int NumArray;
    public int NumToStore;

    public ConstructorControl() {
      ExpLastItem = new ExpDesc();
    }
  }
}

namespace UniLua {
  public class LuaCsClosureValue {
    public CSharpFunctionDelegate F;
    public StkId[] Upvals;

    public LuaCsClosureValue(CSharpFunctionDelegate f) {
      F = f;
    }

    public LuaCsClosureValue(CSharpFunctionDelegate f, int numUpvalues) {
      F = f;
      Upvals = new StkId[numUpvalues];
      for (int i = 0; i < numUpvalues; ++i) {
        var newItem = new StkId();
        Upvals[i] = newItem;
        newItem.SetList(Upvals);
        newItem.SetIndex(i);
      }
    }
  }
}

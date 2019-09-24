namespace UniLua {
  public class ExpDesc {
    public ExpKind Kind;

    public int Info;

    public struct IndData {
      public int T;
      public int Idx;
      public ExpKind Vt;
    }

    public IndData Ind;

    public double NumberValue;

    public int ExitTrue;
    public int ExitFalse;

    public void CopyFrom(ExpDesc e) {
      this.Kind = e.Kind;
      this.Info = e.Info;
      // this.Ind.T 		= e.Ind.T;
      // this.Ind.Idx 	= e.Ind.Idx;
      // this.Ind.Vt 		= e.Ind.Vt;
      this.Ind = e.Ind;
      this.NumberValue = e.NumberValue;
      this.ExitTrue = e.ExitTrue;
      this.ExitFalse = e.ExitFalse;
    }
  }
}

namespace UniLua {
  public class StkId {
    public TValue V;

    private StkId[] List;
    public int Index { get; private set; }

    public void SetList(StkId[] list) {
      List = list;
    }

    public void SetIndex(int index) {
      Index = index;
    }

    public static StkId inc(ref StkId val) {
      var ret = val;
      val = val.List[val.Index + 1];
      return ret;
    }

    public override string ToString() {
      string detail;
      if (V.TtIsString()) {
        detail = V.SValue().Replace("\n", "»");
      }
      else {
        detail = "...";
      }

      return string.Format("StkId - {0} - {1}", LuaState.TypeName((LuaType) V.Tt), detail);
    }
  }
}
namespace UniLua {
  public class StkId {
    public TValue V;

    public StkId[] List;
    public int Index { get; set; }

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

      return string.Format("StkId - {0} - {1}", LuaState.TypeNameSt((LuaType) V.Tt), detail);
    }

    public static StkId Clone(StkId other)
    {
      if (other == null) return null;
      var ret = new StkId();
      ret.V = TValue.Clone(other.V);
      ret.Index = other.Index;
      if (other.List == null) ret.List = null;
      else
      {
        ret.List = new StkId[other.List.Length];
        for (var i = 0; i < other.List.Length; i++) ret.List[i] = Clone(other.List[i]);
      }
      return ret;
    }
  }
}
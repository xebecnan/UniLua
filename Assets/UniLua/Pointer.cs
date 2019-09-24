using System.Collections.Generic;

namespace UniLua {
  public struct Pointer<T> {
    private List<T> List;
    public int Index { get; set; }

    public T Value {
      get { return List[Index]; }
      set { List[Index] = value; }
    }

    public T ValueInc {
      get { return List[Index++]; }
      set { List[Index++] = value; }
    }

    public Pointer(List<T> list, int index) : this() {
      List = list;
      Index = index;
    }

    public Pointer(Pointer<T> other) : this() {
      List = other.List;
      Index = other.Index;
    }

    public static Pointer<T> operator +(Pointer<T> lhs, int rhs) {
      return new Pointer<T>(lhs.List, lhs.Index + rhs);
    }

    public static Pointer<T> operator -(Pointer<T> lhs, int rhs) {
      return new Pointer<T>(lhs.List, lhs.Index - rhs);
    }
  }
}
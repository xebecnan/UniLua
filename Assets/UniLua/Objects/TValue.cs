using System;

namespace UniLua {
  public struct TValue {
    private const UInt64 CLOSURE_LUA = 0; // lua closure
    private const UInt64 CLOSURE_CS = 1; // c# closure
    private const UInt64 CLOSURE_LCS = 2; // light c# closure

    private const UInt64 BOOLEAN_FALSE = 0;
    private const UInt64 BOOLEAN_TRUE = 1;

    public int Tt;
    public double NValue;
    public UInt64 UInt64Value;
    public object OValue;
#if DEBUG_DUMMY_TVALUE_MODIFY
		public bool Lock_;
#endif

    public override int GetHashCode() {
      return Tt.GetHashCode() ^ NValue.GetHashCode()
                              ^ UInt64Value.GetHashCode()
                              ^ (OValue != null ? OValue.GetHashCode() : 0x12345678);
    }

    public override bool Equals(object o) {
      if (!(o is TValue)) return false;
      return Equals((TValue) o);
    }

    public bool Equals(TValue o) {
      if (Tt != o.Tt || NValue != o.NValue || UInt64Value != o.UInt64Value) {
        return false;
      }

      switch (Tt) {
        case (int) LuaType.LUA_TNIL: return true;
        case (int) LuaType.LUA_TBOOLEAN: return BValue() == o.BValue();
        case (int) LuaType.LUA_TNUMBER: return NValue == o.NValue;
        case (int) LuaType.LUA_TUINT64: return UInt64Value == o.UInt64Value;
        case (int) LuaType.LUA_TSTRING: return SValue() == o.SValue();
        default: return System.Object.ReferenceEquals(OValue, o.OValue);
      }
    }

    public static bool operator ==(TValue lhs, TValue rhs) {
      return lhs.Equals(rhs);
    }

    public static bool operator !=(TValue lhs, TValue rhs) {
      return !lhs.Equals(rhs);
    }

#if DEBUG_DUMMY_TVALUE_MODIFY
		private void CheckLock() {
			if(Lock_) {
				UnityEngine.ULDebug.LogError("changing a lock value");
			}
		}
#endif

    internal bool TtIsNil() {
      return Tt == (int) LuaType.LUA_TNIL;
    }

    internal bool TtIsBoolean() {
      return Tt == (int) LuaType.LUA_TBOOLEAN;
    }

    internal bool TtIsNumber() {
      return Tt == (int) LuaType.LUA_TNUMBER;
    }

    internal bool TtIsUInt64() {
      return Tt == (int) LuaType.LUA_TUINT64;
    }

    internal bool TtIsString() {
      return Tt == (int) LuaType.LUA_TSTRING;
    }

    internal bool TtIsTable() {
      return Tt == (int) LuaType.LUA_TTABLE;
    }

    internal bool TtIsFunction() {
      return Tt == (int) LuaType.LUA_TFUNCTION;
    }

    internal bool TtIsThread() {
      return Tt == (int) LuaType.LUA_TTHREAD;
    }

    internal bool ClIsLuaClosure() {
      return UInt64Value == CLOSURE_LUA;
    }

    internal bool ClIsCsClosure() {
      return UInt64Value == CLOSURE_CS;
    }

    internal bool ClIsLcsClosure() {
      return UInt64Value == CLOSURE_LCS;
    }

    internal bool BValue() {
      return UInt64Value != BOOLEAN_FALSE;
    }

    internal string SValue() {
      return (string) OValue;
    }

    internal LuaTable HValue() {
      return OValue as LuaTable;
    }

    internal LuaLClosureValue ClLValue() {
      return (LuaLClosureValue) OValue;
    }

    internal LuaCsClosureValue ClCsValue() {
      return (LuaCsClosureValue) OValue;
    }

    internal LuaUserDataValue RawUValue() {
      return OValue as LuaUserDataValue;
    }

    internal void SetNilValue() {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TNIL;
      NValue = 0.0;
      UInt64Value = 0;
      OValue = null;
    }

    internal void SetBValue(bool v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TBOOLEAN;
      NValue = 0.0;
      UInt64Value = v ? BOOLEAN_TRUE : BOOLEAN_FALSE;
      OValue = null;
    }

    internal void SetObj(ref TValue v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = v.Tt;
      NValue = v.NValue;
      UInt64Value = v.UInt64Value;
      OValue = v.OValue;
    }

    internal void SetNValue(double v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TNUMBER;
      NValue = v;
      UInt64Value = 0;
      OValue = null;
    }

    internal void SetUInt64Value(UInt64 v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TUINT64;
      NValue = 0.0;
      UInt64Value = v;
      OValue = null;
    }

    internal void SetSValue(string v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TSTRING;
      NValue = 0.0;
      UInt64Value = 0;
      OValue = v;
    }

    internal void SetHValue(LuaTable v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TTABLE;
      NValue = 0.0;
      UInt64Value = 0;
      OValue = v;
    }

    internal void SetThValue(LuaState v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TTHREAD;
      NValue = 0.0;
      UInt64Value = 0;
      OValue = v;
    }

    internal void SetPValue(object v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TLIGHTUSERDATA;
      NValue = 0.0;
      UInt64Value = 0;
      OValue = v;
    }

    internal void SetClLValue(LuaLClosureValue v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TFUNCTION;
      NValue = 0.0;
      UInt64Value = CLOSURE_LUA;
      OValue = v;
    }

    internal void SetClCsValue(LuaCsClosureValue v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TFUNCTION;
      NValue = 0.0;
      UInt64Value = CLOSURE_CS;
      OValue = v;
    }

    internal void SetClLcsValue(CSharpFunctionDelegate v) {
#if DEBUG_DUMMY_TVALUE_MODIFY
			CheckLock();
#endif
      Tt = (int) LuaType.LUA_TFUNCTION;
      NValue = 0.0;
      UInt64Value = CLOSURE_LCS;
      OValue = v;
    }

    public override string ToString() {
      if (TtIsString()) {
        return string.Format("(string, {0})", SValue());
      }
      else if (TtIsNumber()) {
        return string.Format("(number, {0})", NValue);
      }
      else if (TtIsNil()) {
        return "(nil)";
      }
      else {
        return string.Format("(type:{0})", Tt);
      }
    }

    public static TValue Clone(TValue other) {
      return (TValue)other.MemberwiseClone();
    }
  }
}

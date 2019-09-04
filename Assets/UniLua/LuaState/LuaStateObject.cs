
// #define DEBUG_DUMMY_TVALUE_MODIFY

namespace UniLua {
  using System;
  using ULDebug = UniLua.Tools.ULDebug;

  public partial class LuaState {
    internal static StkId TheNilValue;

    public static bool O_Str2Decimal(string s, out double result) {
      result = 0.0;

      if (s.Contains("n") || s.Contains("N")) // reject `inf' and `nan'
        return false;

      int pos = 0;
      if (s.Contains("x") || s.Contains("X"))
        result = Utl.StrX2Number(s, ref pos);
      else
        result = Utl.Str2Number(s, ref pos);

      if (pos == 0)
        return false; // nothing recognized

      while (pos < s.Length && Char.IsWhiteSpace(s[pos])) ++pos;
      return pos == s.Length; // OK if no trailing characters
    }

    private static double O_Arith(LuaOp op, double v1, double v2) {
      switch (op) {
        case LuaOp.LUA_OPADD: return v1 + v2;
        case LuaOp.LUA_OPSUB: return v1 - v2;
        case LuaOp.LUA_OPMUL: return v1 * v2;
        case LuaOp.LUA_OPDIV: return v1 / v2;
        case LuaOp.LUA_OPMOD: return v1 % v2;
        case LuaOp.LUA_OPPOW: return Math.Pow(v1, v2);
        case LuaOp.LUA_OPUNM: return -v1;
        default: throw new System.NotImplementedException();
      }
    }

    private bool IsFalse(ref TValue v) {
      if (v.TtIsNil())
        return true;

      if ((v.TtIsBoolean() && v.BValue() == false))
        return true;

      return false;
    }

    private bool ToString(ref TValue o) {
      if (o.TtIsString()) {
        return true;
      }

      return V_ToString(ref o);
    }

    internal LuaLClosureValue GetCurrentLuaFunc(CallInfo ci) {
      if (ci.IsLua) {
        return Stack[ci.FuncIndex].V.ClLValue();
      }
      else return null;
    }

    internal int GetCurrentLine(CallInfo ci) {
      Utl.Assert(ci.IsLua);
      var cl = Stack[ci.FuncIndex].V.ClLValue();
      return cl.Proto.GetFuncLine(ci.CurrentPc);
    }
  }

}

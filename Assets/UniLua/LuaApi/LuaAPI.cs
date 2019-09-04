using System;

// #define DEBUG_RECORD_INS

namespace UniLua {
  public static class LuaAPI {
    public static ILuaState NewState() {
      return new LuaState();
    }
  }
}

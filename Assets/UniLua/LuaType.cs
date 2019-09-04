namespace UniLua {
  public enum LuaType {
    LUA_TNONE = -1,
    LUA_TNIL = 0,
    LUA_TBOOLEAN = 1,
    LUA_TLIGHTUSERDATA = 2,
    LUA_TNUMBER = 3,
    LUA_TSTRING = 4,
    LUA_TTABLE = 5,
    LUA_TFUNCTION = 6,
    LUA_TUSERDATA = 7,
    LUA_TTHREAD = 8,

    LUA_TUINT64 = 9,

    LUA_NUMTAGS = 10,

    LUA_TPROTO,
    LUA_TUPVAL,
    LUA_TDEADKEY,
  }
}
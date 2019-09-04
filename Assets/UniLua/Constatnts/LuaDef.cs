namespace UniLua {
  public static class LuaDef {
    public const int LUA_MINSTACK = 20;
    public const int BASIC_STACK_SIZE = LUA_MINSTACK * 2;
    public const int EXTRA_STACK = 5;

    public const int LUA_RIDX_MAINTHREAD = 1;
    public const int LUA_RIDX_GLOBALS = 2;
    public const int LUA_RIDX_LAST = LUA_RIDX_GLOBALS;

    public const int LUA_MULTRET = -1;

    public const int LUA_REGISTRYINDEX = LuaConf.LUAI_FIRSTPSEUDOIDX;

    // number of list items accumulate before a SETLIST instruction
    public const int LFIELDS_PER_FLUSH = 50;

    public const int LUA_IDSIZE = 60;

    public const string LUA_VERSION_MAJOR = "5";
    public const string LUA_VERSION_MINOR = "2";
    public const string LUA_VERSION = "Lua " + LUA_VERSION_MAJOR + "." + LUA_VERSION_MINOR;

    public const string LUA_ENV = "_ENV";

    public const int BASE_CI_SIZE = 8;
  }
}
namespace UniLua {
  public enum CallStatus {
    CIST_NONE = 0,

    CIST_LUA = (1 << 0), /* call is running a Lua function */
    CIST_HOOKED = (1 << 1), /* call is running a debug hook */
    CIST_REENTRY = (1 << 2), /* call is running on same invocation of
		                           	   luaV_execute of previous call */
    CIST_YIELDED = (1 << 3), /* call reentered after suspension */
    CIST_YPCALL = (1 << 4), /* call is a yieldable protected call */
    CIST_STAT = (1 << 5), /* call has an error status (pcall) */
    CIST_TAIL = (1 << 6), /* call was tail called */
  }
}

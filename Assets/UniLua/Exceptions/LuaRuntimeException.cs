using System;

namespace UniLua {
  public class LuaRuntimeException : Exception {
    public ThreadStatus ErrCode { get; private set; }

    public LuaRuntimeException(ThreadStatus errCode) {
      ErrCode = errCode;
    }
  }
}
using System;

namespace UniLua {
  class UndumpException : Exception {
    public string Why;

    public UndumpException(string why) {
      Why = why;
    }
  }
}

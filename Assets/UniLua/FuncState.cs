using System.Collections.Generic;

namespace UniLua {
  public class FuncState {
    public FuncState Prev;
    public BlockCnt Block;
    public LuaProto Proto;
    public LuaState State;
    public LLex Lexer;

    public Dictionary<TValue, int> H;

    public int NumActVar;
    public int FreeReg;

    public int Pc;
    public int LastTarget;
    public int Jpc;
    public int FirstLocal;

    public FuncState() {
      Proto = new LuaProto();
      H = new Dictionary<TValue, int>();
      NumActVar = 0;
      FreeReg = 0;
    }

    public Pointer<Instruction> GetCode(ExpDesc e) {
      return new Pointer<Instruction>(Proto.Code, e.Info);
    }
  }
}

using System.Collections.Generic;

namespace UniLua {
  public class LuaProto {
    public List<Instruction> Code;
    public List<StkId> K;
    public List<LuaProto> P;
    public List<UpvalDesc> Upvalues;

    public int LineDefined;
    public int LastLineDefined;

    public int NumParams;
    public bool IsVarArg;
    public byte MaxStackSize;

    public string Source;
    public List<int> LineInfo;
    public List<LocVar> LocVars;

    public LuaProto() {
      Code = new List<Instruction>();
      K = new List<StkId>();
      P = new List<LuaProto>();
      Upvalues = new List<UpvalDesc>();
      LineInfo = new List<int>();
      LocVars = new List<LocVar>();
    }

    public int GetFuncLine(int pc) {
      return (0 <= pc && pc < LineInfo.Count) ? LineInfo[pc] : 0;
    }
  }
}

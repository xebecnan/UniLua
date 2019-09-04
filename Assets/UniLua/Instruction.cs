namespace UniLua {
  public struct Instruction {
    public uint Value;

    public Instruction(uint val) {
      Value = val;
    }

    public static explicit operator Instruction(uint val) {
      return new Instruction(val);
    }

    public static explicit operator uint(Instruction i) {
      return i.Value;
    }

    public override string ToString() {
      var op = GET_OPCODE();
      var a = GETARG_A();
      var b = GETARG_B();
      var c = GETARG_C();
      var ax = GETARG_Ax();
      var bx = GETARG_Bx();
      var sbx = GETARG_sBx();
      var mode = OpCodeInfo.GetMode(op);
      switch (mode.OpMode) {
        case OpMode.iABC: {
          string ret = string.Format("{0,-9} {1}"
            , op
            , a);
          if (mode.BMode != OpArgMask.OpArgN)
            ret += " " + (ISK(b) ? MYK(INDEXK(b)) : b);
          if (mode.CMode != OpArgMask.OpArgN)
            ret += " " + (ISK(c) ? MYK(INDEXK(c)) : c);
          return ret;
        }
        case OpMode.iABx: {
          string ret = string.Format("{0,-9} {1}"
            , op
            , a);
          if (mode.BMode == OpArgMask.OpArgK)
            ret += " " + MYK(bx);
          else if (mode.BMode == OpArgMask.OpArgU)
            ret += " " + bx;
          return ret;
        }
        case OpMode.iAsBx: {
          return string.Format("{0,-9} {1} {2}"
            , op
            , a
            , sbx);
        }
        case OpMode.iAx: {
          return string.Format("{0,-9} {1}"
            , op
            , MYK(ax));
        }
        default:
          throw new System.NotImplementedException();
      }
    }

    public const int SIZE_C = 9;
    public const int SIZE_B = 9;
    public const int SIZE_Bx = (SIZE_C + SIZE_B);
    public const int SIZE_A = 8;
    public const int SIZE_Ax = (SIZE_C + SIZE_B + SIZE_A);

    public const int SIZE_OP = 6;

    public const int POS_OP = 0;
    public const int POS_A = (POS_OP + SIZE_OP);
    public const int POS_C = (POS_A + SIZE_A);
    public const int POS_B = (POS_C + SIZE_C);
    public const int POS_Bx = POS_C;
    public const int POS_Ax = POS_A;

#pragma warning disable 0429
    public const int MAXARG_Bx = SIZE_Bx < LuaConf.LUAI_BITSINT
      ? ((1 << SIZE_Bx) - 1)
      : LuaLimits.MAX_INT;

    public const int MAXARG_sBx = SIZE_Bx < LuaConf.LUAI_BITSINT
      ? (MAXARG_Bx >> 1)
      : LuaLimits.MAX_INT;
#pragma warning restore 0429

    public const int MAXARG_Ax = ((1 << SIZE_Ax) - 1);

    public const int MAXARG_A = ((1 << SIZE_A) - 1);
    public const int MAXARG_B = ((1 << SIZE_B) - 1);
    public const int MAXARG_C = ((1 << SIZE_C) - 1);

    public const int BITRK = (1 << (SIZE_B - 1));

    public const int MAXINDEXRK = (BITRK - 1);

    public static int RKASK(int x) {
      return (x | BITRK);
    }

    public static bool ISK(int x) {
      return ((x) & BITRK) != 0;
    }

    public static int INDEXK(int r) {
      return ((int) r & ~BITRK);
    }

    public static int MYK(int x) {
      return (-1 - x);
    }

    public static uint MASK1(int size, int pos) {
      return ((~((~((uint) 0)) << size)) << pos);
    }

    public static uint MASK0(int size, int pos) {
      return (~MASK1(size, pos));
    }

    public OpCode GET_OPCODE() {
      return (OpCode) ((Value >> POS_OP) & MASK1(SIZE_OP, 0));
    }

    public Instruction SET_OPCODE(OpCode op) {
      Value = (Value & MASK0(SIZE_OP, POS_OP)) |
              ((((uint) op) << POS_OP) & MASK1(SIZE_OP, POS_OP));
      return this;
    }

    public int GETARG(int pos, int size) {
      return (int) ((Value >> pos) & MASK1(size, 0));
    }

    public Instruction SETARG(int value, int pos, int size) {
      Value = ((Value & MASK0(size, pos)) |
               (((uint) value << pos) & MASK1(size, pos)));
      return this;
    }

    public int GETARG_A() {
      return GETARG(POS_A, SIZE_A);
    }

    public Instruction SETARG_A(int value) {
      return SETARG(value, POS_A, SIZE_A);
    }

    public int GETARG_B() {
      return GETARG(POS_B, SIZE_B);
    }

    public Instruction SETARG_B(int value) {
      return SETARG(value, POS_B, SIZE_B);
    }

    public int GETARG_C() {
      return GETARG(POS_C, SIZE_C);
    }

    public Instruction SETARG_C(int value) {
      return SETARG(value, POS_C, SIZE_C);
    }

    public int GETARG_Bx() {
      return GETARG(POS_Bx, SIZE_Bx);
    }

    public Instruction SETARG_Bx(int value) {
      return SETARG(value, POS_Bx, SIZE_Bx);
    }

    public int GETARG_Ax() {
      return GETARG(POS_Ax, SIZE_Ax);
    }

    public Instruction SETARG_Ax(int value) {
      return SETARG(value, POS_Ax, SIZE_Ax);
    }

    public int GETARG_sBx() {
      return GETARG_Bx() - MAXARG_sBx;
    }

    public Instruction SETARG_sBx(int value) {
      return SETARG_Bx(value + MAXARG_sBx);
    }

    public static Instruction CreateABC(OpCode op, int a, int b, int c) {
      return (Instruction) ((((uint) op) << POS_OP)
                            | ((uint) a << POS_A)
                            | ((uint) b << POS_B)
                            | ((uint) c << POS_C));
    }

    public static Instruction CreateABx(OpCode op, int a, uint bc) {
      return (Instruction) ((((uint) op) << POS_OP)
                            | ((uint) a << POS_A)
                            | ((uint) bc << POS_Bx));
    }

    public static Instruction CreateAx(OpCode op, int a) {
      return (Instruction) ((((uint) op) << POS_OP)
                            | ((uint) a << POS_Ax));
    }
  }
}
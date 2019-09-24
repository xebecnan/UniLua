
namespace UniLua
{
	using System.Collections.Generic;

  internal static class OpCodeInfo
	{
		public static OpCodeMode GetMode( OpCode op )
		{
			return Info[op];
		}

		private static Dictionary<OpCode, OpCodeMode> Info;

		static OpCodeInfo()
		{
			Info = new Dictionary<OpCode, OpCodeMode>();
			Info.Add( OpCode.OP_MOVE, 		M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_LOADK, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgN, OpMode.iABx) );
			Info.Add( OpCode.OP_LOADKX, 	M(false, true,  OpArgMask.OpArgN, OpArgMask.OpArgN, OpMode.iABx) );
			Info.Add( OpCode.OP_LOADBOOL, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_LOADNIL, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_GETUPVAL, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_GETTABUP, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_GETTABLE, 	M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_SETTABUP, 	M(false, false, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_SETUPVAL, 	M(false, false, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_SETTABLE, 	M(false, false, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_NEWTABLE, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_SELF, 		M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_ADD, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_SUB, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_MUL, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_DIV, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_MOD, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_POW, 		M(false, true,  OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_UNM, 		M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_NOT, 		M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_LEN, 		M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_CONCAT, 	M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgR, OpMode.iABC) );
			Info.Add( OpCode.OP_JMP, 		M(false, false, OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx) );
			Info.Add( OpCode.OP_EQ, 		M(true,  false, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_LT, 		M(true,  false, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_LE, 		M(true,  false, OpArgMask.OpArgK, OpArgMask.OpArgK, OpMode.iABC) );
			Info.Add( OpCode.OP_TEST, 		M(true,  false, OpArgMask.OpArgN, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_TESTSET, 	M(true,  true,  OpArgMask.OpArgR, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_CALL, 		M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_TAILCALL, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_RETURN, 	M(false, false, OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_FORLOOP, 	M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx) );
			Info.Add( OpCode.OP_FORPREP, 	M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx) );
			Info.Add( OpCode.OP_TFORCALL, 	M(false, false, OpArgMask.OpArgN, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_TFORLOOP, 	M(false, true,  OpArgMask.OpArgR, OpArgMask.OpArgN, OpMode.iAsBx) );
			Info.Add( OpCode.OP_SETLIST, 	M(false, false, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iABC) );
			Info.Add( OpCode.OP_CLOSURE, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABx) );
			Info.Add( OpCode.OP_VARARG, 	M(false, true,  OpArgMask.OpArgU, OpArgMask.OpArgN, OpMode.iABC) );
			Info.Add( OpCode.OP_EXTRAARG, 	M(false, false, OpArgMask.OpArgU, OpArgMask.OpArgU, OpMode.iAx) );
		}

		private static OpCodeMode M(bool t, bool a, OpArgMask b, OpArgMask c, OpMode op)
		{
			return new OpCodeMode {
				TMode = t,
				AMode = a,
				BMode = b,
				CMode = c,
				OpMode = op,
			};
		}
	}

}


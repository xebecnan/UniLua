
namespace UniLua
{
	using System.Collections.Generic;

	public enum OpCode
	{
		/*----------------------------------------------------------------------
		name		args	description
		------------------------------------------------------------------------*/
		OP_MOVE,/*	A B	R(A) := R(B)					*/
		OP_LOADK,/*	A Bx	R(A) := Kst(Bx)					*/
		OP_LOADKX,/*	A 	R(A) := Kst(extra arg)				*/
		OP_LOADBOOL,/*	A B C	R(A) := (Bool)B; if (C) pc++			*/
		OP_LOADNIL,/*	A B	R(A), R(A+1), ..., R(A+B) := nil		*/
		OP_GETUPVAL,/*	A B	R(A) := UpValue[B]				*/

		OP_GETTABUP,/*	A B C	R(A) := UpValue[B][RK(C)]			*/
		OP_GETTABLE,/*	A B C	R(A) := R(B)[RK(C)]				*/

		OP_SETTABUP,/*	A B C	UpValue[A][RK(B)] := RK(C)			*/
		OP_SETUPVAL,/*	A B	UpValue[B] := R(A)				*/
		OP_SETTABLE,/*	A B C	R(A)[RK(B)] := RK(C)				*/

		OP_NEWTABLE,/*	A B C	R(A) := {} (size = B,C)				*/

		OP_SELF,/*	A B C	R(A+1) := R(B); R(A) := R(B)[RK(C)]		*/

		OP_ADD,/*	A B C	R(A) := RK(B) + RK(C)				*/
		OP_SUB,/*	A B C	R(A) := RK(B) - RK(C)				*/
		OP_MUL,/*	A B C	R(A) := RK(B) * RK(C)				*/
		OP_DIV,/*	A B C	R(A) := RK(B) / RK(C)				*/
		OP_MOD,/*	A B C	R(A) := RK(B) % RK(C)				*/
		OP_POW,/*	A B C	R(A) := RK(B) ^ RK(C)				*/
		OP_UNM,/*	A B	R(A) := -R(B)					*/
		OP_NOT,/*	A B	R(A) := not R(B)				*/
		OP_LEN,/*	A B	R(A) := length of R(B)				*/

		OP_CONCAT,/*	A B C	R(A) := R(B).. ... ..R(C)			*/

		OP_JMP,/*	A sBx	pc+=sBx; if (A) close all upvalues >= R(A) + 1	*/
		OP_EQ,/*	A B C	if ((RK(B) == RK(C)) ~= A) then pc++		*/
		OP_LT,/*	A B C	if ((RK(B) <  RK(C)) ~= A) then pc++		*/
		OP_LE,/*	A B C	if ((RK(B) <= RK(C)) ~= A) then pc++		*/

		OP_TEST,/*	A C	if not (R(A) <=> C) then pc++			*/
		OP_TESTSET,/*	A B C	if (R(B) <=> C) then R(A) := R(B) else pc++	*/

		OP_CALL,/*	A B C	R(A), ... ,R(A+C-2) := R(A)(R(A+1), ... ,R(A+B-1)) */
		OP_TAILCALL,/*	A B C	return R(A)(R(A+1), ... ,R(A+B-1))		*/
		OP_RETURN,/*	A B	return R(A), ... ,R(A+B-2)	(see note)	*/

		OP_FORLOOP,/*	A sBx	R(A)+=R(A+2);
					if R(A) <?= R(A+1) then { pc+=sBx; R(A+3)=R(A) }*/
		OP_FORPREP,/*	A sBx	R(A)-=R(A+2); pc+=sBx				*/

		OP_TFORCALL,/*	A C	R(A+3), ... ,R(A+2+C) := R(A)(R(A+1), R(A+2));	*/
		OP_TFORLOOP,/*	A sBx	if R(A+1) ~= nil then { R(A)=R(A+1); pc += sBx }*/

		OP_SETLIST,/*	A B C	R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B	*/

		OP_CLOSURE,/*	A Bx	R(A) := closure(KPROTO[Bx])			*/

		OP_VARARG,/*	A B	R(A), R(A+1), ..., R(A+B-2) = vararg		*/

		OP_EXTRAARG/*	Ax	extra (larger) argument for previous opcode	*/
	}

	internal enum OpArgMask
	{
	  OpArgN,  /* argument is not used */
	  OpArgU,  /* argument is used */
	  OpArgR,  /* argument is a register or a jump offset */
	  OpArgK   /* argument is a constant or register/constant */
	}

	/// <summary>
	/// basic instruction format
	/// </summary>
	internal enum OpMode
	{
		iABC,
		iABx,
		iAsBx,
		iAx,
	}

	internal struct OpCodeMode
	{
		public bool 		TMode;
		public bool 		AMode;
		public OpArgMask 	BMode;
		public OpArgMask 	CMode;
		public OpMode		OpMode;
	}

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


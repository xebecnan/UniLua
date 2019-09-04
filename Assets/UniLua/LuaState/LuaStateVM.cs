
// #define DEBUG_NEW_FRAME
// #define DEBUG_INSTRUCTION
// #define DEBUG_INSTRUCTION_WITH_STACK

// #define DEBUG_OP_GETTABUP
// #define DEBUG_OP_GETUPVAL
// #define DEBUG_OP_GETTABLE
// #define DEBUG_OP_EQ
// #define DEBUG_OP_SETLIST
// #define DEBUG_OP_CLOSURE
// #define DEBUG_OP_SETTABLE

// #define DEBUG_RECORD_INS

using System;
using System.Collections.Generic;

namespace UniLua
{
	using ULDebug = UniLua.Tools.ULDebug;
	using StringBuilder = System.Text.StringBuilder;

	public partial class LuaState
	{
		private const int MAXTAGLOOP = 100;

		private struct ExecuteEnvironment
		{
			public StkId[]			Stack;
			public List<StkId> 		K;
			public int 				Base;
			public Instruction 		I;

			public StkId RA
			{
				get { return Stack[Base + I.GETARG_A()]; }
			}

			public StkId RB
			{
				get { return Stack[Base + I.GETARG_B()]; }
			}

			public StkId RK( int x )
			{
				return Instruction.ISK( x ) ? K[Instruction.INDEXK(x)] : Stack[Base+x];
			}

			public StkId RKB
			{
				get { return RK( I.GETARG_B() ); }
			}

			public StkId RKC
			{
				get { return RK( I.GETARG_C() ); }
			}
		}

		private void V_Execute()
		{
			ExecuteEnvironment env;
			CallInfo ci = CI;
newframe:
			Utl.Assert(ci == CI);
			var cl = Stack[ci.FuncIndex].V.ClLValue();

			env.Stack = Stack;
			env.K = cl.Proto.K;
			env.Base = ci.BaseIndex;

#if DEBUG_NEW_FRAME
			ULDebug.Log( "#### NEW FRAME #########################################################################" );
			ULDebug.Log( "## cl:" + cl );
			ULDebug.Log( "## Base:" + env.Base );
			ULDebug.Log( "########################################################################################" );
#endif

			while( true )
			{
				Instruction i = ci.SavedPc.ValueInc;
				env.I = i;

#if DEBUG_SRC_INFO
				int line = 0;
				string src = "";
				if(ci.IsLua) {
					line = GetCurrentLine(ci);
					src = GetCurrentLuaFunc(ci).Proto.Source;
				}
#endif

				StkId ra = env.RA;

#if DEBUG_DUMP_INS_STACK
#if DEBUG_DUMP_INS_STACK_EX
				DumpStack( env.Base, i.ToString() );
#else
				DumpStack( env.Base );
#endif
#endif

#if DEBUG_INSTRUCTION
				ULDebug.Log( System.DateTime.Now + " [VM] ======================================================================== Instruction: " + i
#if DEBUG_INSTRUCTION_WITH_STACK
				+ "\n" + DumpStackToString( env.Base.Index )
#endif
				);
#endif

#if DEBUG_RECORD_INS
				InstructionHistory.Enqueue(i);
				if( InstructionHistory.Count > 100 ) {
					InstructionHistory.Dequeue();
				}
#endif

				switch( i.GET_OPCODE() )
				{
					case OpCode.OP_MOVE:
					{
						var rb = env.RB;

#if DEBUG_OP_MOVE
						ULDebug.Log( "[VM] ==== OP_MOVE rb:" + rb );
						ULDebug.Log( "[VM] ==== OP_MOVE ra:" + ra );
#endif

						ra.V.SetObj(ref rb.V);
						break;
					}

					case OpCode.OP_LOADK:
					{
						var rb = env.K[i.GETARG_Bx()];
						ra.V.SetObj(ref rb.V);
						break;
					}

					case OpCode.OP_LOADKX:
					{
						Utl.Assert( ci.SavedPc.Value.GET_OPCODE() == OpCode.OP_EXTRAARG );
						var rb = env.K[ci.SavedPc.ValueInc.GETARG_Ax()];
						ra.V.SetObj(ref rb.V);
						break;
					}

					case OpCode.OP_LOADBOOL:
					{
						ra.V.SetBValue(i.GETARG_B() != 0);
						if( i.GETARG_C() != 0 )
							ci.SavedPc.Index += 1; // skip next instruction (if C)
						break;
					}

					case OpCode.OP_LOADNIL:
					{
						int b = i.GETARG_B();
						int index = ra.Index;
						do {
							Stack[index++].V.SetNilValue();
						} while (b-- > 0);
						break;
					}

					case OpCode.OP_GETUPVAL:
					{
						int b = i.GETARG_B();
						ra.V.SetObj(ref cl.Upvals[b].V.V);
						
#if DEBUG_OP_GETUPVAL
						// for( var j=0; j<cl.Upvals.Length; ++j)
						// {
						// 	ULDebug.Log("[VM] ==== GETUPVAL upval:" + cl.Upvals[j] );
						// }
						ULDebug.Log( "[VM] ==== GETUPVAL b:" + b );
						ULDebug.Log( "[VM] ==== GETUPVAL ra:" + ra );
#endif
						break;
					}

					case OpCode.OP_GETTABUP:
					{
						int b = i.GETARG_B();
						var key = env.RKC;
						V_GetTable( cl.Upvals[b].V, key, ra );
#if DEBUG_OP_GETTABUP
						ULDebug.Log( "[VM] ==== OP_GETTABUP key:" + key );
						ULDebug.Log( "[VM] ==== OP_GETTABUP val:" + ra );
#endif
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_GETTABLE:
					{
						var tbl = env.RB;
						var key = env.RKC;
						var val = ra;
						V_GetTable( tbl, key, val );
#if DEBUG_OP_GETTABLE
						ULDebug.Log("[VM] ==== OP_GETTABLE key:"+key.ToString());
						ULDebug.Log("[VM] ==== OP_GETTABLE val:"+val.ToString());
#endif
						break;
					}

					case OpCode.OP_SETTABUP:
					{
						int a = i.GETARG_A();

						var key = env.RKB;
						var val = env.RKC;
						V_SetTable( cl.Upvals[a].V, key, val );
#if DEBUG_OP_SETTABUP
						ULDebug.Log( "[VM] ==== OP_SETTABUP key:" + key.Value );
						ULDebug.Log( "[VM] ==== OP_SETTABUP val:" + val.Value );
#endif
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_SETUPVAL:
					{
						int b = i.GETARG_B();
						var uv = cl.Upvals[b];
						uv.V.V.SetObj(ref ra.V);
#if DEBUG_OP_SETUPVAL
						ULDebug.Log( "[VM] ==== SETUPVAL b:" + b );
						ULDebug.Log( "[VM] ==== SETUPVAL ra:" + ra );
#endif
						break;
					}

					case OpCode.OP_SETTABLE:
					{
						var key = env.RKB;
						var val = env.RKC;
#if DEBUG_OP_SETTABLE
						ULDebug.Log( "[VM] ==== OP_SETTABLE key:" + key.ToString() );
						ULDebug.Log( "[VM] ==== OP_SETTABLE val:" + val.ToString() );
#endif
						V_SetTable( ra, key, val );
						break;
					}

					case OpCode.OP_NEWTABLE:
					{
						int b = i.GETARG_B();
						int c = i.GETARG_C();
						var tbl = new LuaTable(this);
						ra.V.SetHValue(tbl);
						if(b > 0 || c > 0)
							{ tbl.Resize(b, c); }
						break;
					}

					case OpCode.OP_SELF:
					{
						// OP_SELF put function referenced by a table on ra
						// and the table on ra+1
						//
						// RB:  table
						// RKC: key
						var ra1 = Stack[ra.Index+1];
						var rb  = env.RB;
						ra1.V.SetObj(ref rb.V);
						V_GetTable( rb, env.RKC, ra );
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_ADD:
					{
						var rkb = env.RKB;
						var rkc = env.RKC;
						if(rkb.V.TtIsNumber() && rkc.V.TtIsNumber())
							{ ra.V.SetNValue(rkb.V.NValue + rkc.V.NValue); }
						else
							{ V_Arith(ra, rkb, rkc, TMS.TM_ADD); }

						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_SUB:
					{
						var rkb = env.RKB;
						var rkc = env.RKC;
						if(rkb.V.TtIsNumber() && rkc.V.TtIsNumber())
							{ ra.V.SetNValue(rkb.V.NValue - rkc.V.NValue); }
						else
							{ V_Arith(ra, rkb, rkc, TMS.TM_SUB); }
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_MUL:
					{
						var rkb = env.RKB;
						var rkc = env.RKC;
						if(rkb.V.TtIsNumber() && rkc.V.TtIsNumber())
							{ ra.V.SetNValue(rkb.V.NValue * rkc.V.NValue); }
						else
							{ V_Arith(ra, rkb, rkc, TMS.TM_MUL); }
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_DIV:
					{
						var rkb = env.RKB;
						var rkc = env.RKC;
						if(rkb.V.TtIsNumber() && rkc.V.TtIsNumber())
							{ ra.V.SetNValue(rkb.V.NValue / rkc.V.NValue); }
						else
							{ V_Arith(ra, rkb, rkc, TMS.TM_DIV); }
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_MOD:
					{
						var rkb = env.RKB;
						var rkc = env.RKC;
						if(rkb.V.TtIsNumber() && rkc.V.TtIsNumber())
							{ ra.V.SetNValue(rkb.V.NValue % rkc.V.NValue); }
						else
							{ V_Arith(ra, rkb, rkc, TMS.TM_MOD); }
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_POW:
					{
						var rkb = env.RKB;
						var rkc = env.RKC;
						if(rkb.V.TtIsNumber() && rkc.V.TtIsNumber())
							{ ra.V.SetNValue(Math.Pow(rkb.V.NValue, rkc.V.NValue)); }
						else
							{ V_Arith(ra, rkb, rkc, TMS.TM_POW); }
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_UNM:
					{
						var rb = env.RB;
						if(rb.V.TtIsNumber()) {
							ra.V.SetNValue(-rb.V.NValue);
						}
						else {
							V_Arith(ra, rb, rb, TMS.TM_UNM);
							env.Base = ci.BaseIndex;
						}
						break;
					}

					case OpCode.OP_NOT:
					{
						var rb = env.RB;
						ra.V.SetBValue(IsFalse(ref rb.V));
						break;
					}

					case OpCode.OP_LEN:
					{
						V_ObjLen( ra, env.RB );
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_CONCAT:
					{
						int b = i.GETARG_B();
						int c = i.GETARG_C();
						Top = Stack[env.Base + c + 1];
						V_Concat( c - b + 1 );
						env.Base = ci.BaseIndex;

						ra = env.RA; // 'V_Concat' may invoke TMs and move the stack
						StkId rb = env.RB;
						ra.V.SetObj(ref rb.V);

						Top = Stack[ci.TopIndex]; // restore top
						break;
					}

					case OpCode.OP_JMP:
					{
						V_DoJump( ci, i, 0 );
						break;
					}

					case OpCode.OP_EQ:
					{
						var lhs = env.RKB;
						var rhs = env.RKC;
						var expectEq = i.GETARG_A() != 0;
#if DEBUG_OP_EQ
						ULDebug.Log( "[VM] ==== OP_EQ lhs:" + lhs );
						ULDebug.Log( "[VM] ==== OP_EQ rhs:" + rhs );
						ULDebug.Log( "[VM] ==== OP_EQ expectEq:" + expectEq );
						ULDebug.Log( "[VM] ==== OP_EQ (lhs.V == rhs.V):" + (lhs.V == rhs.V) );
#endif
						if((lhs.V == rhs.V) != expectEq)
						{
							ci.SavedPc.Index += 1; // skip next jump instruction
						}
						else
						{
							V_DoNextJump( ci );
						}
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_LT:
					{
						var expectCmpResult = i.GETARG_A() != 0;
						if( V_LessThan( env.RKB, env.RKC ) != expectCmpResult )
							ci.SavedPc.Index += 1;
						else
							V_DoNextJump( ci );
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_LE:
					{
						var expectCmpResult = i.GETARG_A() != 0;
						if( V_LessEqual( env.RKB, env.RKC ) != expectCmpResult )
							ci.SavedPc.Index += 1;
						else
							V_DoNextJump( ci );
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_TEST:
					{
						if((i.GETARG_C() != 0) ?
							IsFalse(ref ra.V) : !IsFalse(ref ra.V))
						{
							ci.SavedPc.Index += 1;
						}
						else V_DoNextJump( ci );

						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_TESTSET:
					{
						var rb = env.RB;
						if((i.GETARG_C() != 0) ?
							IsFalse(ref rb.V) : !IsFalse(ref rb.V))
						{
							ci.SavedPc.Index += 1;
						}
						else
						{
							ra.V.SetObj(ref rb.V);
							V_DoNextJump( ci );
						}
						env.Base = ci.BaseIndex;
						break;
					}

					case OpCode.OP_CALL:
					{
						int b = i.GETARG_B();
						int nresults = i.GETARG_C() - 1;
						if( b != 0) { Top = Stack[ra.Index + b]; } 	// else previous instruction set top
						if( D_PreCall( ra, nresults ) ) { // C# function?
							if( nresults >= 0 )
								Top = Stack[ci.TopIndex];
							env.Base = ci.BaseIndex;
						}
						else { // Lua function
							ci = CI;
							ci.CallStatus |= CallStatus.CIST_REENTRY;
							goto newframe;
						}
						break;
					}

					case OpCode.OP_TAILCALL:
					{
						int b = i.GETARG_B();
						if( b != 0) { Top = Stack[ra.Index + b]; } 	// else previous instruction set top
						
						Utl.Assert( i.GETARG_C() - 1 == LuaDef.LUA_MULTRET );

						var called = D_PreCall( ra, LuaDef.LUA_MULTRET );

						// C# function ?
						if( called )
						{
							env.Base = ci.BaseIndex;
						}

						// LuaFunciton
						else
						{
							var nci = CI;				// called frame
							var oci = BaseCI[CI.Index-1]; // caller frame
							StkId nfunc = Stack[nci.FuncIndex];// called function
							StkId ofunc = Stack[oci.FuncIndex];// caller function
							var ncl = nfunc.V.ClLValue();
							var ocl = ofunc.V.ClLValue();

							// last stack slot filled by 'precall'
							int lim = nci.BaseIndex + ncl.Proto.NumParams;

							if(cl.Proto.P.Count > 0)
								{ F_Close( Stack[env.Base] ); }

							// move new frame into old one
							var nindex = nfunc.Index;
							var oindex = ofunc.Index;
							while(nindex < lim) {
								Stack[oindex++].V.SetObj(ref Stack[nindex++].V);
							}

							oci.BaseIndex = ofunc.Index + (nci.BaseIndex - nfunc.Index);
							oci.TopIndex = ofunc.Index + (Top.Index - nfunc.Index);
							Top = Stack[oci.TopIndex];
							oci.SavedPc = nci.SavedPc;
							oci.CallStatus |= CallStatus.CIST_TAIL;
							ci = CI = oci;

							ocl = ofunc.V.ClLValue();
							Utl.Assert(Top.Index == oci.BaseIndex + ocl.Proto.MaxStackSize);

							goto newframe;
						}

						break;
					}

					case OpCode.OP_RETURN:
					{
						int b = i.GETARG_B();
						if( b != 0 ) { Top = Stack[ra.Index + b - 1]; }
						if( cl.Proto.P.Count > 0 ) { F_Close(Stack[env.Base]); }
						b = D_PosCall( ra.Index );
						if( (ci.CallStatus & CallStatus.CIST_REENTRY) == 0 )
						{
							return;
						}
						else
						{
							ci = CI;
							if( b != 0 ) Top = Stack[ci.TopIndex];
							goto newframe;
						}
					}

					case OpCode.OP_FORLOOP:
					{
						var ra1 = Stack[ra.Index + 1];
						var ra2 = Stack[ra.Index + 2];
						var ra3 = Stack[ra.Index + 3];
						
						var step 	= ra2.V.NValue;
						var idx 	= ra.V.NValue + step;	// increment index
						var limit 	= ra1.V.NValue;

						if( (0 < step) ? idx <= limit
									   : limit <= idx )
						{
							ci.SavedPc.Index += i.GETARG_sBx(); // jump back
							ra.V.SetNValue(idx);// updateinternal index...
							ra3.V.SetNValue(idx);// ... and external index
						}

						break;
					}

					case OpCode.OP_FORPREP:
					{
						var init = new TValue();
						var limit = new TValue();
						var step = new TValue();

						var ra1 = Stack[ra.Index + 1];
						var ra2 = Stack[ra.Index + 2];

						// WHY: why limit is not used ?

						if(!V_ToNumber(ra, ref init))
							G_RunError("'for' initial value must be a number");
						if(!V_ToNumber(ra1, ref limit))
							G_RunError("'for' limit must be a number");
						if(!V_ToNumber(ra2, ref step))
							G_RunError("'for' step must be a number");

						ra.V.SetNValue(init.NValue - step.NValue);
						ci.SavedPc.Index += i.GETARG_sBx();

						break;
					}

					case OpCode.OP_TFORCALL:
					{
						int rai = ra.Index;
						int cbi = ra.Index + 3;
						Stack[cbi+2].V.SetObj(ref Stack[rai+2].V);
						Stack[cbi+1].V.SetObj(ref Stack[rai+1].V);
						Stack[cbi].V.SetObj(ref Stack[rai].V);

						StkId callBase = Stack[cbi];
						Top = Stack[cbi+3]; // func. +2 args (state and index)

						D_Call( callBase, i.GETARG_C(), true );

						env.Base = ci.BaseIndex;

						Top = Stack[ci.TopIndex];
						i = ci.SavedPc.ValueInc;	// go to next instruction
						env.I = i;
						ra = env.RA;

						DumpStack( env.Base );
#if DEBUG_INSTRUCTION
						ULDebug.Log( "[VM] ============================================================ OP_TFORCALL Instruction: " + i );
#endif

						Utl.Assert( i.GET_OPCODE() == OpCode.OP_TFORLOOP );
						goto l_tforloop;
					}

					case OpCode.OP_TFORLOOP:
l_tforloop:
					{
						StkId ra1 = Stack[ra.Index + 1];
						if(!ra1.V.TtIsNil())	// continue loop?
						{
							ra.V.SetObj(ref ra1.V);
							ci.SavedPc += i.GETARG_sBx();
						}
						break;
					}

					// sets the values for a range of array elements in a table(RA)
					// RA -> table
					// RB -> number of elements to set
					// C  -> encodes the block number of the table to be initialized
					// the values used to initialize the table are located in
					//   R(A+1), R(A+2) ...
					case OpCode.OP_SETLIST:
					{
						int n = i.GETARG_B();
						int c = i.GETARG_C();
						if( n == 0 ) n = (Top.Index - ra.Index) - 1;
						if( c == 0 )
						{
							Utl.Assert( ci.SavedPc.Value.GET_OPCODE() == OpCode.OP_EXTRAARG );
							c = ci.SavedPc.ValueInc.GETARG_Ax();
						}

						var tbl = ra.V.HValue();
						Utl.Assert( tbl != null );

						int last = ((c-1) * LuaDef.LFIELDS_PER_FLUSH) + n;
						int rai = ra.Index;
						for(; n>0; --n) {
							tbl.SetInt(last--, ref Stack[rai+n].V);
						}
#if DEBUG_OP_SETLIST
						ULDebug.Log( "[VM] ==== OP_SETLIST ci.Top:" + ci.Top.Index );
						ULDebug.Log( "[VM] ==== OP_SETLIST Top:" + Top.Index );
#endif
						Top = Stack[ci.TopIndex]; // correct top (in case of previous open call)
						break;
					}

					case OpCode.OP_CLOSURE:
					{
						LuaProto p = cl.Proto.P[ i.GETARG_Bx() ];
						V_PushClosure( p, cl.Upvals, env.Base, ra );
#if DEBUG_OP_CLOSURE
						ULDebug.Log( "OP_CLOSURE:" + ra.Value );
						var racl = ra.Value as LuaLClosure;
						if( racl != null )
						{
							for( int ii=0; ii<racl.Upvals.Count; ++ii )
							{
								ULDebug.Log( ii + " ) " + racl.Upvals[ii] );
							}
						}
#endif
						break;
					}

					/// <summary>
					/// VARARG implements the vararg operator `...' in expressions.
					/// VARARG copies B-1 parameters into a number of registers
					/// starting from R(A), padding with nils if there aren't enough values.
					/// If B is 0, VARARG copies as many values as it can based on
					/// the number of parameters passed.
					/// If a fixed number of values is required, B is a value greater than 1.
					/// If any number of values is required, B is 0.
					/// </summary>
					case OpCode.OP_VARARG:
					{
						int b = i.GETARG_B() - 1;
						int n = (env.Base - ci.FuncIndex) - cl.Proto.NumParams - 1;
						if( b < 0 ) // B == 0?
						{
							b = n;
							D_CheckStack(n);
							ra = env.RA; // previous call may change the stack
							Top = Stack[ra.Index + n];
						}

						var p = ra.Index;
						var q = env.Base - n;
						for(int j=0; j<b; ++j) {
							if(j < n) {
								Stack[p++].V.SetObj(ref Stack[q++].V);
							}
							else {
								Stack[p++].V.SetNilValue();
							}
						}
						break;
					}

					case OpCode.OP_EXTRAARG:
					{
						Utl.Assert( false );
						V_NotImplemented( i );
						break;
					}

					default:
						V_NotImplemented( i );
						break;
				}
			}
		}

		private void V_NotImplemented( Instruction i )
		{
			ULDebug.LogError( "[VM] ==================================== Not Implemented Instruction: " + i );
			// throw new NotImplementedException();
		}

		private StkId FastTM( LuaTable et, TMS tm )
		{
			if( et == null )
				return null;

			if( (et.NoTagMethodFlags & (1u << (int)tm)) != 0u )
				return null;

			return T_GetTM( et, tm );
		}

		private void V_GetTable( StkId t, StkId key, StkId val )
		{
			for( int loop=0; loop<MAXTAGLOOP; ++loop ) {
				StkId tmObj;
				if(t.V.TtIsTable()) {
					var tbl = t.V.HValue();
					var res = tbl.Get( ref key.V );
					if( !res.V.TtIsNil() ) {
						val.V.SetObj(ref res.V);
						return;
					}

					tmObj = FastTM( tbl.MetaTable, TMS.TM_INDEX );
					if( tmObj == null ) {
						val.V.SetObj(ref res.V);
						return;
					}

					// else will try the tag method
				}
				else {
					tmObj = T_GetTMByObj(ref t.V, TMS.TM_INDEX);
					if(tmObj.V.TtIsNil())
						G_SimpleTypeError(ref t.V, "index" );
				}

				if(tmObj.V.TtIsFunction()) {
					CallTM( ref tmObj.V, ref t.V, ref key.V, val, true );
					return;
				}

				t = tmObj;
			}
			G_RunError( "loop in gettable" );
		}

		private void V_SetTable(StkId t, StkId key, StkId val)
		{
			for( int loop=0; loop<MAXTAGLOOP; ++loop ) {
				StkId tmObj;
				if(t.V.TtIsTable()) {
					var tbl = t.V.HValue();
					var oldval = tbl.Get(ref key.V);
					if(!oldval.V.TtIsNil()) {
						tbl.Set(ref key.V, ref val.V);
						return;
					}

					// check meta method
					tmObj = FastTM(tbl.MetaTable, TMS.TM_NEWINDEX);
					if( tmObj == null ) {
						tbl.Set(ref key.V, ref val.V);
						return;
					}

					// else will try the tag method
				}
				else {
					tmObj = T_GetTMByObj(ref t.V, TMS.TM_NEWINDEX);
					if(tmObj.V.TtIsNil())
						G_SimpleTypeError(ref t.V, "index" );
				}

				if(tmObj.V.TtIsFunction()) {
					CallTM( ref tmObj.V, ref t.V, ref key.V, val, false );
					return;
				}

				t = tmObj;
			}
			G_RunError( "loop in settable" );
		}

		private void V_PushClosure( LuaProto p, LuaUpvalue[] encup, int stackBase, StkId ra )
		{
			var ncl = new LuaLClosureValue( p );
			ra.V.SetClLValue(ncl);
			for( int i=0; i<p.Upvalues.Count; ++i )
			{
				// ULDebug.Log( "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ V_PushClosure i:" + i );
				// ULDebug.Log( "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ V_PushClosure InStack:" + p.Upvalues[i].InStack );
				// ULDebug.Log( "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ V_PushClosure Index:" + p.Upvalues[i].Index );

				if( p.Upvalues[i].InStack ) // upvalue refers to local variable
					ncl.Upvals[i] = F_FindUpval(
						Stack[stackBase + p.Upvalues[i].Index] );
				else	// get upvalue from enclosing function
					ncl.Upvals[i] = encup[ p.Upvalues[i].Index ];
			}
		}

		private void V_ObjLen( StkId ra, StkId rb )
		{
			StkId tmObj = null;

			var rbt = rb.V.HValue();
			if( rbt != null )
			{
				tmObj = FastTM( rbt.MetaTable, TMS.TM_LEN );
				if( tmObj != null )
					goto calltm;
				ra.V.SetNValue(rbt.Length);
				return;
			}

			var rbs = rb.V.SValue();
			if( rbs != null )
			{
				ra.V.SetNValue(rbs.Length);
				return;
			}

			tmObj = T_GetTMByObj(ref rb.V, TMS.TM_LEN);
			if(tmObj.V.TtIsNil())
				G_TypeError( rb, "get length of" );

calltm:
			CallTM( ref tmObj.V, ref rb.V, ref rb.V, ra, true );
		}

		private void V_Concat( int total )
		{
			Utl.Assert( total >= 2 );

			do
			{
				var top = Top;
				int n = 2;
				var lhs = Stack[top.Index - 2];
				var rhs = Stack[top.Index - 1];
				if(!(lhs.V.TtIsString() || lhs.V.TtIsNumber()) || !ToString(ref rhs.V))
				{
					if( !CallBinTM( lhs, rhs, lhs, TMS.TM_CONCAT ) )
						G_ConcatError( lhs, rhs );
				}
				else if(rhs.V.SValue().Length == 0) {
					ToString(ref lhs.V);
				}
				else if(lhs.V.TtIsString() && lhs.V.SValue().Length == 0) {
					lhs.V.SetObj(ref rhs.V);
				}
				else
				{
					StringBuilder sb = new StringBuilder();
					n = 0;
					for( ; n<total; ++n )
					{
						var cur = Stack[top.Index-(n+1)];

						if(cur.V.TtIsString())
							sb.Insert(0, cur.V.SValue());
						else if(cur.V.TtIsNumber())
							sb.Insert(0, cur.V.NValue.ToString());
						else
							break;
					}

					var dest = Stack[top.Index - n];
					dest.V.SetSValue(sb.ToString());
				}
				total -= n-1;
				Top = Stack[Top.Index - (n-1)];
			} while( total > 1 );
		}

		private void V_DoJump( CallInfo ci, Instruction i, int e )
		{
			int a = i.GETARG_A();
			if( a > 0 )
				F_Close(Stack[ci.BaseIndex + (a-1)]);
			ci.SavedPc += i.GETARG_sBx() + e;
		}

		private void V_DoNextJump( CallInfo ci )
		{
			Instruction i = ci.SavedPc.Value;
			V_DoJump( ci, i, 1 );
		}

		private bool V_ToNumber( StkId obj, ref TValue n )
		{
			if( obj.V.TtIsNumber() ) {
				n.SetNValue( obj.V.NValue );
				return true;
			}
			if( obj.V.TtIsString() ) {
				double val;
				if( O_Str2Decimal(obj.V.SValue(), out val) ) {
					n.SetNValue( val );
					return true;
				}
			}

			return false;
		}

		private bool V_ToString(ref TValue v)
		{
			if(!v.TtIsNumber()) { return false; }

			v.SetSValue(v.NValue.ToString());
			return true;
		}

		private LuaOp TMS2OP( TMS op )
		{
			switch( op )
			{
				case TMS.TM_ADD: return LuaOp.LUA_OPADD;
				case TMS.TM_SUB: return LuaOp.LUA_OPSUB;
				case TMS.TM_MUL: return LuaOp.LUA_OPMUL;
				case TMS.TM_DIV: return LuaOp.LUA_OPDIV;
				case TMS.TM_POW: return LuaOp.LUA_OPPOW;
				case TMS.TM_UNM: return LuaOp.LUA_OPUNM;

				// case TMS.TM_EQ:	return LuaOp.LUA_OPEQ;
				// case TMS.TM_LT: return LuaOp.LUA_OPLT;
				// case TMS.TM_LE: return LuaOp.LUA_OPLE;

				default: throw new System.NotImplementedException();
			}
		}

		private void CallTM( ref TValue f, ref TValue p1, ref TValue p2, StkId p3, bool hasres )
		{
			var result = p3.Index;
			var func = Top;
			StkId.inc(ref Top).V.SetObj(ref f); 	// push function
			StkId.inc(ref Top).V.SetObj(ref p1);	// push 1st argument
			StkId.inc(ref Top).V.SetObj(ref p2);	// push 2nd argument
			if( !hasres ) 		// no result? p3 is 3rd argument
				StkId.inc(ref Top).V.SetObj(ref p3.V);
			D_CheckStack(0);
			D_Call( func, (hasres ? 1 : 0), CI.IsLua );
			if( hasres )		// if has result, move it ot its place
			{
				Top = Stack[Top.Index - 1];
				Stack[result].V.SetObj(ref Top.V);
			}
		}

		private bool CallBinTM( StkId p1, StkId p2, StkId res, TMS tm )
		{
			var tmObj = T_GetTMByObj(ref p1.V, tm);
			if(tmObj.V.TtIsNil())
				tmObj = T_GetTMByObj(ref p2.V, tm);
			if(tmObj.V.TtIsNil())
				return false;

			CallTM( ref tmObj.V, ref p1.V, ref p2.V, res, true );
			return true;
		}

		private void V_Arith( StkId ra, StkId rb, StkId rc, TMS op )
		{
			var nb = new TValue();
			var nc = new TValue();
			if(V_ToNumber(rb, ref nb) && V_ToNumber(rc, ref nc))
			{
				var res = O_Arith( TMS2OP(op), nb.NValue, nc.NValue );
				ra.V.SetNValue( res );
			}
			else if( !CallBinTM( rb, rc, ra, op ) )
			{
				G_ArithError( rb, rc );
			}
		}

		private bool CallOrderTM( StkId p1, StkId p2, TMS tm, out bool error )
		{
			if( !CallBinTM( p1, p2, Top, tm ) )
			{
				error = true; // no metamethod
				return false;
			}

			error = false;
			return !IsFalse(ref Top.V);
		}

		private bool V_LessThan( StkId lhs, StkId rhs )
		{
			// compare number
			if(lhs.V.TtIsNumber() && rhs.V.TtIsNumber()) {
				return lhs.V.NValue < rhs.V.NValue;
			}

			// compare string
			if(lhs.V.TtIsString() && rhs.V.TtIsString()) {
				return string.Compare(lhs.V.SValue(), rhs.V.SValue()) < 0;
			}

			bool error;
			var res = CallOrderTM( lhs, rhs, TMS.TM_LT, out error );
			if( error )
			{
				G_OrderError( lhs, rhs );
				return false;
			}
			return res;
		}

		private bool V_LessEqual( StkId lhs, StkId rhs )
		{
			// compare number
			if(lhs.V.TtIsNumber() && rhs.V.TtIsNumber()) {
				return lhs.V.NValue <= rhs.V.NValue;
			}

			// compare string
			if(lhs.V.TtIsString() && rhs.V.TtIsString()) {
				return string.Compare(lhs.V.SValue(), rhs.V.SValue()) <= 0;
			}

			// first try `le'
			bool error;
			var res = CallOrderTM( lhs, rhs, TMS.TM_LE, out error );
			if( !error )
				return res;

			// else try `lt'
			res = CallOrderTM( rhs, lhs, TMS.TM_LT, out error );
			if( !error )
				return res;

			G_OrderError( lhs, rhs );
			return false;
		}

		private void V_FinishOp()
		{
			int ciIndex = CI.Index;
			int stackBase = CI.BaseIndex;
			Instruction i = (CI.SavedPc - 1).Value; // interrupted instruction
			OpCode op = i.GET_OPCODE();
			switch( op )
			{
				case OpCode.OP_ADD: case OpCode.OP_SUB: case OpCode.OP_MUL: case OpCode.OP_DIV:
				case OpCode.OP_MOD: case OpCode.OP_POW: case OpCode.OP_UNM: case OpCode.OP_LEN:
				case OpCode.OP_GETTABUP: case OpCode.OP_GETTABLE: case OpCode.OP_SELF:
				{
					var tmp = Stack[stackBase + i.GETARG_A()];
					Top = Stack[Top.Index-1];
					tmp.V.SetObj(ref Stack[Top.Index].V);
					break;
				}

				case OpCode.OP_LE: case OpCode.OP_LT: case OpCode.OP_EQ:
				{
					bool res = !IsFalse(ref Stack[Top.Index-1].V);
					Top = Stack[Top.Index-1];
					// metamethod should not be called when operand is K
					Utl.Assert( !Instruction.ISK( i.GETARG_B() ) );
					if( op == OpCode.OP_LE && // `<=' using `<' instead?
						T_GetTMByObj(ref Stack[stackBase + i.GETARG_B()].V, TMS.TM_LE ).V.TtIsNil() )
					{
						res = !res; // invert result
					}

					var ci = BaseCI[ciIndex];
					Utl.Assert( ci.SavedPc.Value.GET_OPCODE() == OpCode.OP_JMP );
					if( (res ? 1 : 0) != i.GETARG_A() )
					if( (i.GETARG_A() == 0) == res ) // condition failed?
					{
						ci.SavedPc.Index++; // skip jump instruction
					}
					break;
				}

				case OpCode.OP_CONCAT:
				{
					StkId top = Stack[Top.Index - 1]; // top when `CallBinTM' was called
					int b = i.GETARG_B(); // first element to concatenate
					int total = top.Index-1 - (stackBase+b); // yet to concatenate
					var tmp = Stack[top.Index-2];
					tmp.V.SetObj(ref top.V); // put TM result in proper position
					if(total > 1) // are there elements to concat?
					{
						Top = Stack[Top.Index-1];
						V_Concat( total );
					}
					// move final result to final position
					var ci = BaseCI[ciIndex];
					var tmp2 = Stack[ci.BaseIndex + i.GETARG_A()];
					tmp2.V.SetObj(ref Stack[Top.Index-1].V);
					Top = Stack[ci.TopIndex];
					break;
				}

				case OpCode.OP_TFORCALL:
				{
					var ci = BaseCI[ciIndex];
					Utl.Assert( ci.SavedPc.Value.GET_OPCODE() == OpCode.OP_TFORLOOP );
					Top = Stack[ci.TopIndex]; // restore top
					break;
				}

				case OpCode.OP_CALL:
				{
					if( i.GETARG_C() - 1 >= 0 ) // numResults >= 0?
					{
						var ci = BaseCI[ciIndex];
						Top = Stack[ci.TopIndex]; // restore top
					}
					break;
				}

				case OpCode.OP_TAILCALL: case OpCode.OP_SETTABUP:  case OpCode.OP_SETTABLE:
					break;

				default:
					Utl.Assert( false );
					break;
			}
		}

		internal bool V_RawEqualObj( ref TValue t1, ref TValue t2 )
		{
			return (t1.Tt == t2.Tt) && V_EqualObject( ref t1, ref t2, true );
		}

		private bool EqualObj( ref TValue t1, ref TValue t2, bool rawEq )
		{
			return (t1.Tt == t2.Tt) && V_EqualObject( ref t1, ref t2, rawEq );
		}

		private StkId GetEqualTM( LuaTable mt1, LuaTable mt2, TMS tm )
		{
			var tm1 = FastTM( mt1, tm );
			if(tm1 == null) // no metamethod
				return null;
			if(mt1 == mt2) // same metatables => same metamethods
				return tm1;
			var tm2 = FastTM( mt2, tm );
			if(tm2 == null) // no metamethod
				return null;
			if(V_RawEqualObj(ref tm1.V, ref tm2.V)) // same metamethods?
				return tm1;
			return null;
		}

		private bool V_EqualObject( ref TValue t1, ref TValue t2, bool rawEq )
		{
			Utl.Assert( t1.Tt == t2.Tt );
			StkId tm = null;
			switch( t1.Tt )
			{
				case (int)LuaType.LUA_TNIL:
					return true;
				case (int)LuaType.LUA_TNUMBER:
					return t1.NValue == t2.NValue;
				case (int)LuaType.LUA_TUINT64:
					return t1.UInt64Value == t2.UInt64Value;
				case (int)LuaType.LUA_TBOOLEAN:
					return t1.BValue() == t2.BValue();
				case (int)LuaType.LUA_TSTRING:
					return t1.SValue() == t2.SValue();
				case (int)LuaType.LUA_TUSERDATA:
				{
					var ud1 = t1.RawUValue();
					var ud2 = t2.RawUValue();
					if(ud1.Value == ud2.Value)
						return true;
					if(rawEq)
						return false;
					tm = GetEqualTM( ud1.MetaTable, ud2.MetaTable, TMS.TM_EQ );
					break;
				}
				case (int)LuaType.LUA_TTABLE:
				{
					var tbl1 = t1.HValue();
					var tbl2 = t2.HValue();
					if( System.Object.ReferenceEquals( tbl1, tbl2 ) )
						return true;
					if( rawEq )
						return false;
					tm = GetEqualTM( tbl1.MetaTable, tbl2.MetaTable, TMS.TM_EQ );
					break;
				}
				default:
					return t1.OValue == t2.OValue;
			}
			if( tm == null ) // no TM?
				return false;
			CallTM(ref tm.V, ref t1, ref t2, Top, true ); // call TM
			return !IsFalse(ref Top.V);
		}

	}

}


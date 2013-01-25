
// #define DEBUG_NEW_FRAME
// #define DEBUG_INSTRUCTION
// #define DEBUG_INSTRUCTION_WITH_STACK

// #define DEBUG_OP_GETTABUP
// #define DEBUG_OP_GETUPVAL
// #define DEBUG_OP_GETTABLE
// #define DEBUG_OP_EQ
// #define DEBUG_OP_SETLIST
// #define DEBUG_OP_CLOSURE

// #define DEBUG_RECORD_INS

using System;
using System.Collections.Generic;

namespace UniLua
{
	using Debug = UniLua.Tools.Debug;
	using StringBuilder = System.Text.StringBuilder;

	public partial class LuaState
	{
		private const int MAXTAGLOOP = 100;

		private struct ExecuteEnvironment
		{
			public StkId 		K;
			public StkId 		Base;
			public Instruction 	I;

			public StkId RA
			{
				get { return Base + I.GETARG_A(); }
			}

			public StkId RB
			{
				get { return Base + I.GETARG_B(); }
			}

			public StkId RK( int x )
			{
				return Instruction.ISK( x ) ? K + Instruction.INDEXK(x) : Base + x;
			}

			public StkId RKB
			{
				get { return RK( I.GETARG_B() ); }
			}

			public StkId RKC
			{
				get { return RK( I.GETARG_C() ); }
			}

			public delegate double ArithDelegate( double lhs, double rhs );
			public void ArithOp( LuaState lua, TMS tm, ArithDelegate op )
			{
				var lhs = RKB.Value as LuaNumber;
				var rhs = RKC.Value as LuaNumber;
				if( lhs != null && rhs != null )
				{
					var ra = RA;
					var res = op( lhs.Value, rhs.Value );
					ra.Value = new LuaNumber( res );
				}
				else lua.V_Arith( RA, RKB, RKC, tm );
			}
		}

		private void V_Execute()
		{
			ExecuteEnvironment env;
			CallInfo ci = CI;
newframe:
			LuaLClosure cl = ci.Func.Value as LuaLClosure;

			env.K = new StkId( cl.Proto.K, 0 );
			env.Base = ci.Base;

#if DEBUG_NEW_FRAME
			Debug.Log( "#### NEW FRAME #########################################################################" );
			Debug.Log( "## cl:" + cl );
			Debug.Log( "## Base:" + env.Base );
			Debug.Log( "########################################################################################" );
#endif

			while( true )
			{
				Instruction i = ci.SavedPc.ValueInc;
				env.I = i;

				StkId ra = env.RA;

				DumpStack( env.Base.Index );

#if DEBUG_INSTRUCTION
				Debug.Log( System.DateTime.Now + " [VM] ======================================================================== Instruction: " + i
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
						Debug.Log( "[VM] ==== OP_MOVE rb:" + rb );
						Debug.Log( "[VM] ==== OP_MOVE ra:" + ra );
#endif

						ra.Value = rb.Value;
						break;
					}

					case OpCode.OP_LOADK:
					{
						StkId rb = env.K + i.GETARG_Bx();
						ra.Value = rb.Value;
						break;
					}

					case OpCode.OP_LOADKX:
					{
						Utl.Assert( ci.SavedPc.Value.GET_OPCODE() == OpCode.OP_EXTRAARG );
						StkId rb = env.K + ci.SavedPc.ValueInc.GETARG_Ax();
						ra.Value = rb.Value;
						break;
					}

					case OpCode.OP_LOADBOOL:
					{
						ra.Value = new LuaBoolean( i.GETARG_B() != 0 );
						if( i.GETARG_C() != 0 )
							ci.SavedPc.Index += 1; // skip next instruction (if C)
						break;
					}

					case OpCode.OP_LOADNIL:
					{
						int b = i.GETARG_B();
						do {
							ra.ValueInc = new LuaNil();
						} while (b-- > 0);
						break;
					}

					case OpCode.OP_GETUPVAL:
					{
						int b = i.GETARG_B();
						ra.Value = cl.Upvals[b].V.Value;
						
#if DEBUG_OP_GETUPVAL
						// foreach( var upval in cl.Upvals )
						// {
						// 	Debug.Log("[VM] ==== GETUPVAL upval:" + upval );
						// }
						Debug.Log( "[VM] ==== GETUPVAL b:" + b );
						Debug.Log( "[VM] ==== GETUPVAL ra:" + ra );
#endif
						break;
					}

					case OpCode.OP_GETTABUP:
					{
						int b = i.GETARG_B();
						var key = env.RKC;
						V_GetTable( cl.Upvals[b].V.Value, key.Value, ra );
#if DEBUG_OP_GETTABUP
						Debug.Log( "[VM] ==== OP_GETTABUP key:" + key );
						Debug.Log( "[VM] ==== OP_GETTABUP val:" + ra );
#endif
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_GETTABLE:
					{
						var tbl = env.RB;
						var key = env.RKC;
						var val = ra;
						V_GetTable( tbl.Value, key.Value, val );
#if DEBUG_OP_GETTABLE
						Debug.Log( "[VM] ==== OP_GETTABLE key:" + key.Value );
						Debug.Log( "[VM] ==== OP_GETTABLE val:" + val.Value );
#endif
						break;
					}

					case OpCode.OP_SETTABUP:
					{
						int a = i.GETARG_A();

						var key = env.RKB;
						var val = env.RKC;
						V_SetTable( cl.Upvals[a].V.Value, key.Value, val );
#if DEBUG_OP_SETTABUP
						Debug.Log( "[VM] ==== OP_SETTABUP key:" + key.Value );
						Debug.Log( "[VM] ==== OP_SETTABUP val:" + val.Value );
#endif
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_SETUPVAL:
					{
						int b = i.GETARG_B();
						var uv = cl.Upvals[b];
						uv.V.Value = ra.Value;
#if DEBUG_OP_SETUPVAL
						Debug.Log( "[VM] ==== SETUPVAL b:" + b );
						Debug.Log( "[VM] ==== SETUPVAL ra:" + ra );
#endif
						break;
					}

					case OpCode.OP_SETTABLE:
					{
						var key = env.RKB;
						var val = env.RKC;
						V_SetTable( ra.Value, key.Value, val );
#if DEBUG_OP_SETTABLE
						Debug.Log( "[VM] ==== OP_SETTABLE key:" + key.Value );
						Debug.Log( "[VM] ==== OP_SETTABLE val:" + val.Value );
#endif
						break;
					}

					case OpCode.OP_NEWTABLE:
					{
						// int b = i.GETARG_B();
						// int c = i.GETARG_C();
						ra.Value = new LuaTable();
						break;
					}

					case OpCode.OP_SELF:
					{
						// OP_SELF put function referenced by a table on ra
						// and the table on ra+1
						//
						// RB:  table
						// RKC: key
						var ra1 = ra + 1;
						var rb  = env.RB;
						ra1.Value = rb.Value;
						V_GetTable( rb.Value, env.RKC.Value, ra );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_ADD:
					{
						env.ArithOp( this, TMS.TM_ADD, (lhs, rhs) => lhs + rhs );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_SUB:
					{
						env.ArithOp( this, TMS.TM_SUB, (lhs, rhs) => lhs - rhs );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_MUL:
					{
						env.ArithOp( this, TMS.TM_MUL, (lhs, rhs) => lhs * rhs );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_DIV:
					{
						env.ArithOp( this, TMS.TM_DIV, (lhs, rhs) => lhs / rhs );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_MOD:
					{
						env.ArithOp( this, TMS.TM_DIV, (lhs, rhs) => lhs % rhs );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_POW:
					{
						env.ArithOp( this, TMS.TM_DIV, (lhs, rhs) => Math.Pow(lhs, rhs) );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_UNM:
					{
						var rb = env.RB;
						var n = rb.Value as LuaNumber;
						if( n != null )
						{
							ra.Value = new LuaNumber( -n.Value );
						}
						else
						{
							V_Arith( ra, rb, rb, TMS.TM_UNM );
							env.Base = ci.Base;
						}
						break;
					}

					case OpCode.OP_NOT:
					{
						var rb = env.RB;
						ra.Value = new LuaBoolean( rb.Value.IsFalse );
						break;
					}

					case OpCode.OP_LEN:
					{
						V_ObjLen( ra, env.RB );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_CONCAT:
					{
						int b = i.GETARG_B();
						int c = i.GETARG_C();
						Top = env.Base + (c + 1);
						V_Concat( c - b + 1 );
						env.Base = ci.Base;

						ra = env.RA; // 'V_Concat' may invoke TMs and move the stack
						StkId rb = env.RB;
						ra.Value = rb.Value;

						Top = ci.Top; // restore top
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
						Debug.Log( "[VM] ==== OP_EQ lhs:" + lhs );
						Debug.Log( "[VM] ==== OP_EQ rhs:" + rhs );
						Debug.Log( "[VM] ==== OP_EQ expectEq:" + expectEq );
						Debug.Log( "[VM] ==== OP_EQ (lhs.Value == rhs.Value):" + (lhs.Value == rhs.Value) );
#endif
						if( (lhs.Value == rhs.Value) != expectEq )
						{
							ci.SavedPc.Index += 1; // skip next jump instruction
						}
						else
						{
							V_DoNextJump( ci );
						}
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_LT:
					{
						var expectCmpResult = i.GETARG_A() != 0;
						if( V_LessThan( env.RKB, env.RKC ) != expectCmpResult )
							ci.SavedPc.Index += 1;
						else
							V_DoNextJump( ci );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_LE:
					{
						var expectCmpResult = i.GETARG_A() != 0;
						if( V_LessEqual( env.RKB, env.RKC ) != expectCmpResult )
							ci.SavedPc.Index += 1;
						else
							V_DoNextJump( ci );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_TEST:
					{
						if( (i.GETARG_C() != 0) ? ra.Value.IsFalse : !ra.Value.IsFalse )
							ci.SavedPc.Index += 1;
						else
							V_DoNextJump( ci );
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_TESTSET:
					{
						var rb = env.RB;
						if( (i.GETARG_C() != 0) ? rb.Value.IsFalse : !rb.Value.IsFalse )
							ci.SavedPc.Index += 1;
						else
						{
							ra.Value = rb.Value;
							V_DoNextJump( ci );
						}
						env.Base = ci.Base;
						break;
					}

					case OpCode.OP_CALL:
					{
						int b = i.GETARG_B();
						int nresults = i.GETARG_C() - 1;
						if( b != 0) { Top = ra + b; } 	// else previous instruction set top
						if( D_PreCall( ra, nresults ) ) { // C# function?
							if( nresults >= 0 )
								Top = ci.Top;
							env.Base = ci.Base;
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
						if( b != 0) { Top = ra + b; } 	// else previous instruction set top
						
						Utl.Assert( i.GETARG_C() - 1 == LuaDef.LUA_MULTRET );

						var called = D_PreCall( ra, LuaDef.LUA_MULTRET );

						// C# function ?
						if( called )
						{
							env.Base = ci.Base;
						}

						// LuaFunciton
						else
						{
							CallInfo nci = CI;				// called frame
							CallInfo oci = nci.Previous;	// caller frame
							StkId nfunc = nci.Func;			// called function
							StkId ofunc = oci.Func;			// caller function
							LuaLClosure ncl = nfunc.Value as LuaLClosure;
							LuaLClosure ocl = ofunc.Value as LuaLClosure;

							// last stack slot filled by 'precall'
							StkId lim = nci.Base + ncl.Proto.NumParams;

							if( cl.Proto.P.Count > 0 ) { F_Close( env.Base ); }

							// move new frame into old one
							var nfuncaux = nfunc;
							var ofuncaux = ofunc;
							while( nfuncaux.Index<lim.Index )
							{
								// Debug.Log( "================== assign lhs ofuncaux:" + ofuncaux );
								// Debug.Log( "================== assign rhs nfuncaux:" + nfuncaux );
								ofuncaux.ValueInc = nfuncaux.ValueInc;
							}

							oci.Base = ofunc + (nci.Base.Index - nfunc.Index);
							oci.Top = Top = ofunc + (Top.Index - nfunc.Index);
							oci.SavedPc = nci.SavedPc;
							oci.CallStatus |= CallStatus.CIST_TAIL;
							ci = CI = oci;
							ocl = ofunc.Value as LuaLClosure;

							Utl.Assert( Top.Index == oci.Base.Index + ocl.Proto.MaxStackSize );

							goto newframe;
						}

						break;
					}

					case OpCode.OP_RETURN:
					{
						int b = i.GETARG_B();
						if( b != 0 ) { Top = ra + b - 1; }
						if( cl.Proto.P.Count > 0 ) { F_Close( env.Base ); }
						b = D_PosCall( ra );
						if( (ci.CallStatus & CallStatus.CIST_REENTRY) == 0 )
						{
							// Debug.Log( System.DateTime.Now + DumpStackToString( env.Base.Index ));
							// Debug.Log( "{{ RETURN }}" );
							return;
						}
						else
						{
							ci = CI;
							if( b != 0 ) Top = ci.Top;
							goto newframe;
						}
					}

					case OpCode.OP_FORLOOP:
					{
						var ra1 = ra + 1;
						var ra2 = ra + 2;
						var ra3 = ra + 3;
						
						var ran 	= ra.Value as LuaNumber;
						var ra1n 	= ra1.Value as LuaNumber;
						var ra2n 	= ra2.Value as LuaNumber;

						var step 	= ra2n.Value;
						var idx 	= ran.Value + step;			// increment index
						var limit 	= ra1n.Value;

						if( (0 < step) ? idx <= limit
									   : limit <= idx )
						{
							ci.SavedPc.Index += i.GETARG_sBx(); // jump back
							ra.Value  = new LuaNumber( idx ); 	// updateinternal index...
							ra3.Value = new LuaNumber( idx ); 	// ... and external index
						}

						break;
					}

					case OpCode.OP_FORPREP:
					{
						var ra1		= ra + 1;
						var ra2		= ra + 2;

						var init 	= V_ToNumber( ra.Value );
						var limit 	= V_ToNumber( ra1.Value );
						var step 	= V_ToNumber( ra2.Value );

						if( init == null )
							G_RunError( "'for' initial value must be a number" );
						if( limit == null )
							G_RunError( "'for' limit must be a number" );
						if( step == null )
							G_RunError( "'for' step must be a number" );

						ra.Value = new LuaNumber( init.Value - step.Value );
						ci.SavedPc.Index += i.GETARG_sBx();

						break;
					}

					case OpCode.OP_TFORCALL:
					{
						StkId s = ra;
						StkId d = ra + 3;
						d.ValueInc = s.ValueInc;
						d.ValueInc = s.ValueInc;
						d.ValueInc = s.ValueInc;

						StkId callBase = ra + 3;
						Top = callBase + 3; // func. +2 args (state and index)

						D_Call( callBase, i.GETARG_C(), true );

						env.Base = ci.Base;

						Top = ci.Top;
						i = ci.SavedPc.ValueInc;	// go to next instruction
						env.I = i;
						ra = env.RA;

						DumpStack( env.Base.Index );
#if DEBUG_INSTRUCTION
						Debug.Log( "[VM] ============================================================ OP_TFORCALL Instruction: " + i );
#endif

						Utl.Assert( i.GET_OPCODE() == OpCode.OP_TFORLOOP );
						goto l_tforloop;
					}

					case OpCode.OP_TFORLOOP:
l_tforloop:
					{
						StkId ra1 = ra + 1;
						if( ra1.Value as LuaNil == null )	// continue loop?
						{
							ra.Value = ra1.Value;
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

						var tbl = ra.Value as LuaTable;
						Utl.Assert( tbl != null );

						int last = ((c-1) * LuaDef.LFIELDS_PER_FLUSH) + n;
						for( ; n>0; --n )
						{
							var val = ra + n;
							tbl.SetInt( last--, val.Value );
						}
#if DEBUG_OP_SETLIST
						Debug.Log( "[VM] ==== OP_SETLIST ci.Top:" + ci.Top.Index );
						Debug.Log( "[VM] ==== OP_SETLIST Top:" + Top.Index );
#endif
						Top = ci.Top; // correct top (in case of previous open call)
						break;
					}

					case OpCode.OP_CLOSURE:
					{
						LuaProto p = cl.Proto.P[ i.GETARG_Bx() ];
						V_PushClosure( p, cl.Upvals, env.Base, ra );
#if DEBUG_OP_CLOSURE
						Debug.Log( "OP_CLOSURE:" + ra.Value );
						var racl = ra.Value as LuaLClosure;
						if( racl != null )
						{
							for( int ii=0; ii<racl.Upvals.Count; ++ii )
							{
								Debug.Log( ii + " ) " + racl.Upvals[ii] );
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
						int n = (env.Base.Index - ci.Func.Index) - cl.Proto.NumParams - 1;
						if( b < 0 ) // B == 0?
						{
							b = n;
							Top = ra + n;
						}

						var p = ra;
						var q = env.Base - n;
						for( int j=0; j<b; ++j )
						{
							if( j<n )
							{
								p.ValueInc = q.ValueInc;
							}
							else
							{
								p.ValueInc = new LuaNil();
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
			Debug.LogError( "[VM] ==================================== Not Implemented Instruction: " + i );
			// throw new NotImplementedException();
		}

		private LuaObject FastTM( LuaTable et, TMS tm )
		{
			if( et == null )
				return null;

			if( (et.NoTagMethodFlags & (1u << (int)tm)) != 0u )
				return null;

			return T_GetTM( et, tm );
		}

		private void V_GetTable( LuaObject t, LuaObject key, StkId val )
		{
			for( int loop=0; loop<MAXTAGLOOP; ++loop )
			{
				LuaObject tmObj;
				var tbl = t as LuaTable;
				if( tbl != null )
				{
					var res = tbl.Get( key );
					if( !res.IsNil )
					{
						val.Value = res;
						return;
					}

					tmObj = FastTM( tbl.MetaTable, TMS.TM_INDEX );
					if( tmObj == null )
					{
						val.Value = res;
						return;
					}

					// else will try the tag method
				}
				else
				{
					tmObj = T_GetTMByObj( t, TMS.TM_INDEX );
					if( tmObj.IsNil )
						G_SimpleTypeError( t, "index" );
				}

				if( tmObj.IsFunction )
				{
					CallTM( tmObj, t, key, val, true );
					return;
				}

				t = tmObj;
			}
			G_RunError( "loop in gettable" );
		}

		private void V_SetTable( LuaObject t, LuaObject key, StkId val )
		{
			// // Debug.Log( "V_SetTable: " + t );
			// var tbl = t as LuaTable;
			// if( tbl == null )
			// {
			// 	throw new Exception( "t is not indexable" );
			// }
			// tbl.Set( key, val.Value );

			for( int loop=0; loop<MAXTAGLOOP; ++loop )
			{
				LuaObject tmObj;
				var tbl = t as LuaTable;
				if( tbl != null )
				{
					var oldval = tbl.Get( key );
					if( !oldval.IsNil )
					{
						tbl.Set( key, val.Value );
						return;
					}

					// check meta method
					tmObj = FastTM( tbl.MetaTable, TMS.TM_NEWINDEX );
					if( tmObj == null )
					{
						tbl.Set( key, val.Value );
						return;
					}

					// else will try the tag method
				}
				else
				{
					tmObj = T_GetTMByObj( t, TMS.TM_NEWINDEX );
					if( tmObj.IsNil )
						G_SimpleTypeError( t, "index" );
				}

				if( tmObj.IsFunction )
				{
					CallTM( tmObj, t, key, val, false );
					return;
				}

				t = tmObj;
			}
			G_RunError( "loop in settable" );
		}

		private void V_PushClosure( LuaProto p, List<LuaUpvalue> encup, StkId stackBase, StkId ra )
		{
			LuaLClosure ncl = new LuaLClosure( p );
			ra.Value = ncl;
			for( int i=0; i<p.Upvalues.Count; ++i )
			{
				// Debug.Log( "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ V_PushClosure i:" + i );
				// Debug.Log( "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ V_PushClosure InStack:" + p.Upvalues[i].InStack );
				// Debug.Log( "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ V_PushClosure Index:" + p.Upvalues[i].Index );

				if( p.Upvalues[i].InStack ) // upvalue refers to local variable
					ncl.Upvals[i] = F_FindUpval( stackBase + p.Upvalues[i].Index );
				else	// get upvalue from enclosing function
					ncl.Upvals[i] = encup[ p.Upvalues[i].Index ];
			}
		}

		private void V_ObjLen( StkId ra, StkId rb )
		{
			LuaObject tmObj = null;

			var rbt = rb.Value as LuaTable;
			if( rbt != null )
			{
				tmObj = FastTM( rbt.MetaTable, TMS.TM_LEN );
				if( tmObj != null )
					goto calltm;
				ra.Value = new LuaNumber( rbt.Length );
				return;
			}

			var rbs = rb.Value as LuaString;
			if( rbs != null )
			{
				ra.Value = new LuaNumber( rbs.Value.Length );
				return;
			}

			tmObj = T_GetTMByObj( rb.Value, TMS.TM_LEN );
			if( tmObj.IsNil )
				G_TypeError( rb, "get length of" );

calltm:
			CallTM( tmObj, rb.Value, rb.Value, ra, true );
		}

		private void V_Concat( int total )
		{
			Utl.Assert( total >= 2 );

			do
			{
				var top = Top;
				int n = 2;
				var lhs = top - 2;
				var rhs = top - 1;
				var lhss = lhs.Value as LuaString;
				var lhsn = lhs.Value as LuaNumber;
				var rhss = rhs.Value as LuaString;
				var rhsn = rhs.Value as LuaNumber;
				if( lhss == null && lhsn == null &&
					(rhss != null || rhsn != null) )
				{
					if( !CallBinTM( lhs, rhs, lhs, TMS.TM_CONCAT ) )
						G_ConcatError( lhs, rhs );
				}
				else if( rhss != null && string.IsNullOrEmpty( rhss.Value ) )
				{
					if( lhss == null && lhsn != null )
					{
						lhs.Value = new LuaString( lhsn.ToLiteral() );
					}
				}
				else if( lhss != null && string.IsNullOrEmpty( lhss.Value ) )
				{
					lhs.Value = rhs.Value;
				}
				else
				{
					StringBuilder sb = new StringBuilder();
					n = 0;
					for( ; n<total; ++n )
					{
						var cur = top-(n+1);
						var curs = cur.Value as LuaString;
						var curn = cur.Value as LuaNumber;

						if( curs != null )
							sb.Insert( 0, curs.Value );
						else if( curn != null )
							sb.Insert( 0, curn.ToLiteral() );
						else
							break;
					}

					var dest = top - n;
					dest.Value = new LuaString( sb.ToString() );
				}
				total -= n-1;
				Top -= n-1;
			} while( total > 1 );
		}

		private void V_DoJump( CallInfo ci, Instruction i, int e )
		{
			int a = i.GETARG_A();
			if( a > 0 )
				F_Close( ci.Base + (a-1) );
			ci.SavedPc += i.GETARG_sBx() + e;
		}

		private void V_DoNextJump( CallInfo ci )
		{
			Instruction i = ci.SavedPc.Value;
			V_DoJump( ci, i, 1 );
		}

		private LuaNumber V_ToNumber( LuaObject obj )
		{
			var n = obj as LuaNumber;
			if( n != null )
				return n;

			var s = obj as LuaString;
			if( s != null )
			{
				double val;
				if( O_Str2Decimal(s.Value, out val) )
					return new LuaNumber( val );
			}

			return null;
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

		private void CallTM( LuaObject f, LuaObject p1, LuaObject p2, StkId p3, bool hasres )
		{
			var func = Top;
			Top.ValueInc = f;	// push function
			Top.ValueInc = p1;	// push 1st argument
			Top.ValueInc = p2;	// push 2nd argument
			if( !hasres ) 		// no result? p3 is 3rd argument
				Top.ValueInc = p3.Value;
			D_Call( func, (hasres ? 1 : 0), CI.IsLua );
			if( hasres )		// if has result, move it ot its place
			{
				var below = Top - 1;
				p3.Value = below.Value;
				Top -= 1;
			}
		}

		private bool CallBinTM( StkId p1, StkId p2, StkId res, TMS tm )
		{
			var tmObj = T_GetTMByObj( p1.Value, tm );
			if( tmObj.IsNil )
				tmObj = T_GetTMByObj( p2.Value, tm );
			if( tmObj.IsNil )
				return false;

			CallTM( tmObj, p1.Value, p2.Value, res, true );
			return true;
		}

		private void V_Arith( StkId ra, StkId rb, StkId rc, TMS op )
		{
			var b = V_ToNumber( rb.Value );
			var c = V_ToNumber( rc.Value );
			if( b != null && c != null )
			{
				var res = O_Arith( TMS2OP(op), b.Value, c.Value );
				ra.Value = new LuaNumber( res );
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
			return !Top.Value.IsFalse;
		}

		private bool V_LessThan( StkId lhs, StkId rhs )
		{
			// compare number
			var lhsn = lhs.Value as LuaNumber;
			var rhsn = rhs.Value as LuaNumber;
			if( lhsn != null && rhsn != null )
			{
				return lhsn.Value < rhsn.Value;
			}

			// compare string
			var lhss = lhs.Value as LuaString;
			var rhss = rhs.Value as LuaString;
			if( lhss != null && rhss != null )
			{
				return string.Compare( lhss.Value, rhss.Value ) < 0;
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
			var lhsn = lhs.Value as LuaNumber;
			var rhsn = rhs.Value as LuaNumber;
			if( lhsn != null && rhsn != null )
			{
				return lhsn.Value <= rhsn.Value;
			}

			// compare string
			var lhss = lhs.Value as LuaString;
			var rhss = rhs.Value as LuaString;
			if( lhss != null && rhss != null )
			{
				return string.Compare( lhss.Value, rhss.Value ) <= 0;
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
			CallInfo ci = CI;
			StkId stackBase = ci.Base;
			Instruction i = (ci.SavedPc - 1).Value; // interrupted instruction
			OpCode op = i.GET_OPCODE();
			switch( op )
			{
				case OpCode.OP_ADD: case OpCode.OP_SUB: case OpCode.OP_MUL: case OpCode.OP_DIV:
				case OpCode.OP_MOD: case OpCode.OP_POW: case OpCode.OP_UNM: case OpCode.OP_LEN:
				case OpCode.OP_GETTABUP: case OpCode.OP_GETTABLE: case OpCode.OP_SELF:
				{
					var tmp = stackBase + i.GETARG_A();
					tmp.Value = (Top-1).Value;
					Top.Index--;
					break;
				}

				case OpCode.OP_LE: case OpCode.OP_LT: case OpCode.OP_EQ:
				{
					bool res = !(Top-1).Value.IsFalse;
					Top.Index--;
					// metamethod should not be called when operand is K
					Utl.Assert( !Instruction.ISK( i.GETARG_B() ) );
					if( op == OpCode.OP_LE && // `<=' using `<' instead?
						T_GetTMByObj( (stackBase + i.GETARG_B()).Value, TMS.TM_LE ).IsNil )
					{
						res = !res; // invert result
					}
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
					StkId top = Top - 1; // top when `CallBinTM' was called
					int b = i.GETARG_B(); // first element to concatenate
					int total = (top-1).Index - (stackBase+b).Index; // yet to concatenate
					var tmp = top - 2;
					tmp.Value = top.Value; // put TM result in proper position
					if( total > 1 ) // are there elements to concat?
					{
						Top = top - 1;
						V_Concat( total );
					}
					// move final result to final position
					var tmp2 = ci.Base + i.GETARG_A();
					tmp2.Value = (Top - 1).Value;
					Top = ci.Top;
					break;
				}

				case OpCode.OP_TFORCALL:
				{
					Utl.Assert( ci.SavedPc.Value.GET_OPCODE() == OpCode.OP_TFORLOOP );
					Top = ci.Top; // restore top
					break;
				}

				case OpCode.OP_CALL:
				{
					if( i.GETARG_C() - 1 >= 0 ) // numResults >= 0?
						Top = ci.Top;
					break;
				}

				case OpCode.OP_TAILCALL: case OpCode.OP_SETTABUP:  case OpCode.OP_SETTABLE:
					break;

				default:
					Utl.Assert( false );
					break;
			}
		}

		private bool V_RawEqualObj( LuaObject t1, LuaObject t2 )
		{
			return (t1.LuaType == t2.LuaType) && V_EqualObject( t1, t2, true );
		}

		private bool EqualObj( LuaObject t1, LuaObject t2, bool rawEq )
		{
			return (t1.LuaType == t2.LuaType) && V_EqualObject( t1, t2, rawEq );
		}

		private LuaObject GetEqualTM( LuaTable mt1, LuaTable mt2, TMS tm )
		{
			LuaObject tm1 = FastTM( mt1, tm );
			if( tm1 == null ) // no metamethod
				return null;
			if( mt1 == mt2 ) // same metatables => same metamethods
				return tm1;
			LuaObject tm2 = FastTM( mt2, tm );
			if( tm2 == null ) // no metamethod
				return null;
			if( V_RawEqualObj( tm1, tm2 ) ) // same metamethods?
				return tm1;
			return null;
		}

		private bool V_EqualObject( LuaObject t1, LuaObject t2, bool rawEq )
		{
			Utl.Assert( t1.LuaType == t2.LuaType );
			LuaObject tm = null;
			switch( t1.LuaType )
			{
				case LuaType.LUA_TNIL:
					return true;
				case LuaType.LUA_TNUMBER:
				case LuaType.LUA_TBOOLEAN:
				case LuaType.LUA_TSTRING:
					return t1.Equals( t2 );
				case LuaType.LUA_TUSERDATA:
				{
					LuaUserData ud1 = t1 as LuaUserData;
					LuaUserData ud2 = t2 as LuaUserData;
					if( ud1.Value == ud2.Value )
						return true;
					if( rawEq )
						return false;
					tm = GetEqualTM( ud1.MetaTable, ud2.MetaTable, TMS.TM_EQ );
					break;
				}
				case LuaType.LUA_TTABLE:
				{
					LuaTable tbl1 = t1 as LuaTable;
					LuaTable tbl2 = t2 as LuaTable;
					if( System.Object.ReferenceEquals( tbl1, tbl2 ) )
						return true;
					if( rawEq )
						return false;
					tm = GetEqualTM( tbl1.MetaTable, tbl2.MetaTable, TMS.TM_EQ );
					break;
				}
				default:
					return System.Object.ReferenceEquals( t1, t2 );
			}
			if( tm == null ) // no TM?
				return false;
			CallTM( tm, t1, t2, Top, true ); // call TM
			return !Top.Value.IsFalse;
		}

	}

}


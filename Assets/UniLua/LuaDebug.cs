
namespace UniLua
{
	public class LuaDebug
	{
		public string 		Name;
		public string 		NameWhat;
		public CallInfo 	ActiveCI;
		public int			CurrentLine;
		public int			NumUps;
		public bool			IsVarArg;
		public int			NumParams;
		public bool			IsTailCall;
		public string		Source;
		public int			LineDefined;
		public int			LastLineDefined;
		public string		What;
		public string		ShortSrc;
	}

	public partial class LuaState
	{
		bool ILuaAPI.GetStack( int level, LuaDebug ar )
		{
			if( level < 0 )
				return false;

			CallInfo ci;
			for( ci = CI; level > 0 && ci != BaseCI; ci = ci.Previous )
				level--;

			bool status = false;
			if( level == 0 && ci != BaseCI )
			{
				status = true;
				ar.ActiveCI = ci;
			}
			return status;
		}

		public int GetInfo( string what, LuaDebug ar )
		{
			CallInfo 	ci;
			StkId		func;

			int	pos = 0;
			if( what[pos] == '>' )
			{
				ci = null;
				func = Top - 1;

				Utl.ApiCheck( func.Value.IsFunction, "function expected" );
				pos++;

				Top -= 1;
			}
			else
			{
				ci = ar.ActiveCI;
				func = ci.Func;
				Utl.Assert( ci.Func.Value.IsFunction );
			}

			// var IsClosure( func.Value ) ? func.Value
			int status = AuxGetInfo( what, ar, func, ci );
			if( what.Contains( "f" ) )
			{
				Top.Value = func.Value;
				IncrTop();
			}
			if( what.Contains( "L" ) )
			{
				CollectValidLines( func );
			}
			return status;
		}

		private int AuxGetInfo( string what, LuaDebug ar, StkId func, CallInfo ci )
		{
			int status = 1;
			for( int i=0; i<what.Length; ++i )
			{
				char c = what[i];
				switch( c )
				{
					case 'S':
					{
						FuncInfo( ar, func );
						break;
					}
					case 'l':
					{
						ar.CurrentLine = (ci != null && ci.IsLua) ? ci.CurrentLine : -1;
						break;
					}
					case 'u':
					{
						var ccl = func.Value as LuaCSharpClosure;
						var lcl = func.Value as LuaLClosure;
						if( ccl != null )
						{
							ar.NumUps = ccl.Upvals.Count;
							ar.IsVarArg = true;
							ar.NumParams = 0;
						}
						else if( lcl != null )
						{
							ar.NumUps = lcl.Upvals.Count;
							ar.IsVarArg = lcl.Proto.IsVarArg;
							ar.NumParams = lcl.Proto.NumParams;
						}
						break;
					}
					case 't':
					{
						ar.IsTailCall = (ci != null)
							? ( (ci.CallStatus & CallStatus.CIST_TAIL) != 0 )
							: false;
						break;
					}
					case 'n':
					{
						if( ci != null
							&& ((ci.CallStatus & CallStatus.CIST_TAIL) == 0)
							&& ci.Previous.IsLua )
						{
							ar.NameWhat = GetFuncName( ci.Previous, out ar.Name );
						}
						else
						{
							ar.NameWhat = null;
						}
						if( ar.NameWhat == null )
						{
							ar.NameWhat = ""; // not found
							ar.Name = null;
						}
						break;
					}
					case 'L':
					case 'f': // handled by GetInfo
						break;
					default: status = 0; // invalid option
						break;
				}
			}
			return status;
		}

		private void CollectValidLines( StkId func )
		{
			var ccl = func.Value as LuaCSharpClosure;
			var lcl = func.Value as LuaLClosure;
			if( ccl != null )
			{
				Top.Value = new LuaNil();
				IncrTop();
			}
			else if( lcl != null )
			{
				var p = lcl.Proto;
				var lineinfo = p.LineInfo;
				var t = new LuaTable();
				Top.Value = t;
				IncrTop();
				for( int i=0; i<lineinfo.Count; ++i )
					t.SetInt( lineinfo[i], new LuaBoolean(true) );
			}
			else
				throw new System.NotImplementedException();
		}

		private string GetFuncName( CallInfo ci, out string name )
		{
			var proto = ci.CurrentLuaFunc.Proto; // calling function
			var pc = ci.CurrentPc; // calling instruction index
			var ins = proto.Code[pc]; // calling instruction

			TMS tm;
			switch( ins.GET_OPCODE() )
			{
				case OpCode.OP_CALL:
				case OpCode.OP_TAILCALL:  /* get function name */
					return GetObjName(proto, pc, ins.GETARG_A(), out name);

				case OpCode.OP_TFORCALL: {  /* for iterator */
					name = "for iterator";
					return "for iterator";
				}

				/* all other instructions can call only through metamethods */
				case OpCode.OP_SELF:
				case OpCode.OP_GETTABUP:
				case OpCode.OP_GETTABLE: tm = TMS.TM_INDEX; break;

				case OpCode.OP_SETTABUP:
				case OpCode.OP_SETTABLE: tm = TMS.TM_NEWINDEX; break;

				case OpCode.OP_EQ: tm = TMS.TM_EQ; break;
				case OpCode.OP_ADD: tm = TMS.TM_ADD; break;
				case OpCode.OP_SUB: tm = TMS.TM_SUB; break;
				case OpCode.OP_MUL: tm = TMS.TM_MUL; break;
				case OpCode.OP_DIV: tm = TMS.TM_DIV; break;
				case OpCode.OP_MOD: tm = TMS.TM_MOD; break;
				case OpCode.OP_POW: tm = TMS.TM_POW; break;
				case OpCode.OP_UNM: tm = TMS.TM_UNM; break;
				case OpCode.OP_LEN: tm = TMS.TM_LEN; break;
				case OpCode.OP_LT: tm = TMS.TM_LT; break;
				case OpCode.OP_LE: tm = TMS.TM_LE; break;
				case OpCode.OP_CONCAT: tm = TMS.TM_CONCAT; break;

				default:
					name = null;
					return null;  /* else no useful name can be found */
			}

			name = GetTagMethodName( tm );
			return "metamethod";
		}

		private void FuncInfo( LuaDebug ar, StkId func )
		{
			var ccl = func.Value as LuaCSharpClosure;
			var lcl = func.Value as LuaLClosure;
			if( ccl != null )
			{
				ar.Source = "=[C#]";
				ar.LineDefined = -1;
				ar.LastLineDefined = -1;
				ar.What = "C#";
			}
			else if( lcl != null )
			{
				var p = lcl.Proto;
				ar.Source = string.IsNullOrEmpty(p.Source) ? "=?" : p.Source;
				ar.LineDefined = p.LineDefined;
				ar.LastLineDefined = p.LastLineDefined;
				ar.What = (ar.LineDefined == 0) ? "main" : "Lua";
			}
			if( ar.Source.Length > LuaDef.LUA_IDSIZE )
			{
				ar.ShortSrc = ar.Source.Substring(0, LuaDef.LUA_IDSIZE);
			}
			else ar.ShortSrc = ar.Source;
		}

		private void AddInfo( string msg )
		{
			// var api = (ILuaAPI)this;
			// TODO
			if( CI.IsLua )
			{
				var line = CI.CurrentLine;
				var src = CI.CurrentLuaFunc.Proto.Source;
				if( src == null )
					src = "?";

				// 不能用 PushString, 因为 PushString 是 API 接口
				// API 接口中的 ApiIncrTop 会检查 Top 是否超过了 CI.Top 导致出错
				// api.PushString( msg );
				O_PushString( string.Format( "{0}:{1}: {2}",
					src, line, msg ) );
			}
		}

		internal void G_RunError( string fmt, params object[] args )
		{
			AddInfo( string.Format( fmt, args ) );
			G_ErrorMsg();
		}

		private void G_ErrorMsg()
		{
			if( ErrFunc != 0 ) // is there an error handling function?
			{
				StkId errFunc = RestoreStack( ErrFunc );

				var lcl = errFunc.Value as LuaLClosure;
				var ccl = errFunc.Value as LuaCSharpClosure;
				if( lcl == null && ccl == null )
					D_Throw( ThreadStatus.LUA_ERRERR );

				var below = Top - 1;
				Top.Value = below.Value;
				below.Value = errFunc.Value;
				IncrTop();
				
				D_Call( below, 1, false );
			}

			D_Throw( ThreadStatus.LUA_ERRRUN );
		}

		private string UpvalName( LuaProto p, int uv )
		{
			// TODO
			return "(UpvalName:NotImplemented)";
		}

		private string GetUpvalueName( CallInfo ci, StkId o, out string name )
		{
			var lcl = ci.Func.Value as LuaLClosure;
			for( int i=0; i<lcl.Upvals.Count; ++i )
			{
				if( lcl.Upvals[i].V == o )
				{
					name = UpvalName( lcl.Proto, i );
					return "upvalue";
				}
			}
			name = default(string);
			return null;
		}

		private void KName( LuaProto proto, int pc, int c, out string name )
		{
			if( Instruction.ISK(c) ) { // is `c' a constant
				var val = proto.K[Instruction.INDEXK(c)];
				if( val.IsString ) { // literal constant?
					name = (val as LuaString).Value;
					return;
				}
				// else no reasonable name found
			}
			else { // `c' is a register
				string what = GetObjName( proto, pc, c, out name );
				if( what == "constant" ) { // found a constant name
					return; // `name' already filled
				}
				// else no reasonable name found
			}
			name = "?"; // no reasonable name found
		}

		private int FindSetReg( LuaProto proto, int lastpc, int reg )
		{
			var setreg = -1; // keep last instruction that changed `reg'
			for( int pc=0; pc<lastpc; ++pc ) {
				var ins = proto.Code[pc];
				var op  = ins.GET_OPCODE();
				var a 	= ins.GETARG_A();
				switch( op ) {
					case OpCode.OP_LOADNIL: {
						var b = ins.GETARG_B();
						// set registers from `a' to `a+b'
						if( a <= reg && reg <= a + b )
							setreg = pc;
						break;
					}

					case OpCode.OP_TFORCALL: {
						// effect all regs above its base
						if( reg >= a+2 )
							setreg = pc;
						break;
					}

					case OpCode.OP_CALL:
					case OpCode.OP_TAILCALL: {
						// effect all registers above base
						if( reg >= a )
							setreg = pc;
						break;
					}
					
					case OpCode.OP_JMP: {
						var b = ins.GETARG_sBx();
						var dest = pc + 1 + b;
						// jump is forward and do not skip `lastpc'
						if( pc < dest && dest <= lastpc )
							pc += b; // do the jump
						break;
					}

					case OpCode.OP_TEST: {
						// jumped code can change `a'
						if( reg == a )
							setreg = pc;
						break;
					}

					default: {
						// any instruction that set A
						if( Coder.TestAMode( op ) && reg == a ) {
							setreg = pc;
						}
						break;
					}
				}
			}
			return setreg;
		}

		private string GetObjName( LuaProto proto, int lastpc, int reg,
			out string name )
		{
			name = F_GetLocalName( proto, reg+1, lastpc );
			if( name != null ) // is a local?
				return "local";

			// else try symbolic execution
			var pc = FindSetReg( proto, lastpc, reg );
			if( pc != -1 )
			{
				var ins = proto.Code[pc];
				var op = ins.GET_OPCODE();
				switch( op )
				{
					case OpCode.OP_MOVE: {
						var b = ins.GETARG_B(); // move from `b' to `a'
						if( b < ins.GETARG_A() )
							return GetObjName(proto, pc, b, out name);
						break;
					}
					case OpCode.OP_GETTABUP:
					case OpCode.OP_GETTABLE: {
						var k = ins.GETARG_C();
						var t = ins.GETARG_B();
						var vn = (op == OpCode.OP_GETTABLE)
							? F_GetLocalName( proto, t+1, pc )
							: UpvalName( proto, t );
						KName( proto, pc, k, out name );
						return (vn == LuaDef.LUA_ENV) ? "global" : "field";
					}

					case OpCode.OP_GETUPVAL: {
						name = UpvalName( proto, ins.GETARG_B() );
						return "upvalue";
					}

					case OpCode.OP_LOADK:
					case OpCode.OP_LOADKX: {
						var b = (op == OpCode.OP_LOADK)
							? ins.GETARG_Bx()
							: proto.Code[pc+1].GETARG_Ax();
						var val = proto.K[b];
						if( val.IsString )
						{
							name = (val as LuaString).Value;
							return "constant";
						}
						break;
					}

					case OpCode.OP_SELF: {
						var k = ins.GETARG_C(); // key index
						KName( proto, pc, k, out name );
						return "method";
					}

					default: break; // go through to return null
				}
			}

			return null; // could not find reasonable name
		}

		private bool IsInStack( CallInfo ci, StkId o )
		{
			// TODO
			return false;
		}

		private void G_SimpleTypeError( LuaObject o, string op )
		{
			string t = ObjTypeName( o );
			G_RunError( "attempt to {0} a {1} value", op, t );
		}

		private void G_TypeError( StkId o, string op )
		{
			CallInfo ci = CI;
			string name = null;
			string kind = null;
			string t = ObjTypeName( o.Value );
			if( ci.IsLua )
			{
				kind = GetUpvalueName( ci, o, out name);
				if( kind != null && IsInStack( ci, o ) )
				{
					var lcl = ci.Func.Value as LuaLClosure;
					kind = GetObjName( lcl.Proto, ci.CurrentPc,
						(o.Index - ci.Base.Index), out name );
				}
			}
			if( kind != null )
				G_RunError( "attempt to {0} {1} '{2}' (a {3} value)",
					op, kind, name, t );
			else
				G_RunError( "attempt to {0} a {1} value", op, t );
		}

		private void G_ArithError( StkId p1, StkId p2 )
		{
			if( V_ToNumber( p1.Value ) == null )
				p2 = p1;

			G_TypeError( p2, "perform arithmetic on" );
		}

		private void G_OrderError( StkId p1, StkId p2 )
		{
			string t1 = ObjTypeName( p1.Value );
			string t2 = ObjTypeName( p2.Value );
			if( t1 == t2 )
				G_RunError( "attempt to compare two {0} values", t1 );
			else
				G_RunError( "attempt to compare {0} with {1}", t1, t2 );
		}

		private void G_ConcatError( StkId p1, StkId p2 )
		{
			// TODO
		}
	}

}


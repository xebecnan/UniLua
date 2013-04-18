
using System;
using System.IO;
using System.Collections.Generic;

using ULDebug = UniLua.Tools.ULDebug;

namespace UniLua
{
	using InstructionPtr = Pointer<Instruction>;

	public class FuncState
	{
		public FuncState Prev;
		public BlockCnt	Block;
		public LuaProto Proto;
		public LuaState State;
		public LLex		Lexer;

		public Dictionary<TValue, int> H;

		public int NumActVar;
		public int FreeReg;

		public int Pc;
		public int LastTarget;
		public int Jpc;
		public int FirstLocal;

		public FuncState()
		{
			Proto = new LuaProto();
			H = new Dictionary<TValue, int>();
			NumActVar = 0;
			FreeReg = 0;
		}

		public InstructionPtr GetCode( ExpDesc e )
		{
			return new InstructionPtr( Proto.Code, e.Info );
		}
	}

	public class BlockCnt
	{
		public BlockCnt Previous;
		public int		FirstLabel;
		public int		FirstGoto;
		public int		NumActVar;
		public bool		HasUpValue;
		public bool		IsLoop;
	}

	public class ConstructorControl
	{
		public ExpDesc	ExpLastItem;
		public ExpDesc	ExpTable;
		public int		NumRecord;
		public int		NumArray;
		public int		NumToStore;

		public ConstructorControl()
		{
			ExpLastItem = new ExpDesc();
		}
	}

	public enum ExpKind
	{
		VVOID,	/* no value */
		VNIL,
		VTRUE,
		VFALSE,
		VK,		/* info = index of constant in `k' */
		VKNUM,	/* nval = numerical value */
		VNONRELOC,	/* info = result register */
		VLOCAL,	/* info = local register */
		VUPVAL,       /* info = index of upvalue in 'upvalues' */
		VINDEXED,	/* t = table register/upvalue; idx = index R/K */
		VJMP,		/* info = instruction pc */
		VRELOCABLE,	/* info = instruction pc */
		VCALL,	/* info = instruction pc */
		VVARARG	/* info = instruction pc */
	}

	public static class ExpKindUtl
	{
		public static bool VKIsVar( ExpKind k )
		{
			return ((int)ExpKind.VLOCAL <= (int)k &&
					(int)k <= (int)ExpKind.VINDEXED);
		}

		public static bool VKIsInReg( ExpKind k )
		{
			return k == ExpKind.VNONRELOC || k == ExpKind.VLOCAL;
		}
	}
	
	public enum BinOpr
	{
		ADD,
		SUB,
		MUL,
		DIV,
		MOD,
		POW,
		CONCAT,
		EQ,
		LT,
		LE,
		NE,
		GT,
		GE,
		AND,
		OR,
		NOBINOPR,
	}

	public enum UnOpr
	{
		MINUS,
		NOT,
		LEN,
		NOUNOPR,
	}

	public class ExpDesc
	{
		public ExpKind Kind;

		public int Info;

		public struct IndData
		{
			public int T;
			public int Idx;
			public ExpKind Vt;
		}
		public IndData Ind;

		public double NumberValue;

		public int ExitTrue;
		public int ExitFalse;

		public void CopyFrom( ExpDesc e )
		{
			this.Kind 			= e.Kind;
			this.Info 			= e.Info;
			// this.Ind.T 		= e.Ind.T;
			// this.Ind.Idx 	= e.Ind.Idx;
			// this.Ind.Vt 		= e.Ind.Vt;
			this.Ind 			= e.Ind;
			this.NumberValue 	= e.NumberValue;
			this.ExitTrue 		= e.ExitTrue;
			this.ExitFalse 		= e.ExitFalse;
		}
	}

	public class VarDesc
	{
		public int Index;
	}

	public class LabelDesc
	{
		public string 	Name;		// label identifier
		public int 		Pc;			// position in code
		public int		Line;		// line where it appear
		public int		NumActVar;	// local level where it appears in current block
	}

	public class LHSAssign
	{
		public LHSAssign 	Prev;
		public ExpDesc		Exp;

		public LHSAssign() {
			Prev = null;
			Exp  = new ExpDesc();
		}
	}

	public class Parser
	{
		public static LuaProto Parse(
			ILuaState lua, ILoadInfo loadinfo, string name )
		{
			var parser = new Parser();
			parser.Lua = (LuaState)lua;
			parser.Lexer = new LLex( lua, loadinfo, name );

			var topFuncState = new FuncState();
			parser.MainFunc( topFuncState );
			return topFuncState.Proto;
		}

		private const int 		MAXVARS = 200;

		private LLex 			Lexer;
		private FuncState 		CurFunc;
		private List<VarDesc> 	ActVars;
		private List<LabelDesc> PendingGotos;
		private List<LabelDesc> ActiveLabels;
		private LuaState		Lua;

		private Parser()
		{
			ActVars = new List<VarDesc>();
			PendingGotos = new List<LabelDesc>();
			ActiveLabels = new List<LabelDesc>();
			CurFunc = null;
		}

		private LuaProto AddPrototype()
		{
			var p = new LuaProto();
			CurFunc.Proto.P.Add( p );
			return p;
		}

		private void CodeClosure( ExpDesc v )
		{
			FuncState fs = CurFunc.Prev;
			InitExp( v, ExpKind.VRELOCABLE,
				Coder.CodeABx( fs, OpCode.OP_CLOSURE, 0,
					(uint)(fs.Proto.P.Count-1) ) );

			// fix it at stack top
			Coder.Exp2NextReg( fs, v );
		}

		private void OpenFunc( FuncState fs, BlockCnt block )
		{
			fs.Lexer = Lexer;

			fs.Prev = CurFunc;
			CurFunc = fs;

			fs.Pc = 0;
			fs.LastTarget = 0;
			fs.Jpc = Coder.NO_JUMP;
			fs.FreeReg = 0;
			fs.NumActVar = 0;
			fs.FirstLocal = ActVars.Count;

			// registers 0/1 are always valid
			fs.Proto.MaxStackSize = 2;
			fs.Proto.Source = Lexer.Source;

			EnterBlock( fs, block, false );
		}

		private void CloseFunc()
		{
			Coder.Ret( CurFunc, 0, 0 );

			LeaveBlock( CurFunc );

			CurFunc = CurFunc.Prev;
		}

		private void MainFunc( FuncState fs )
		{
			ExpDesc v = new ExpDesc();
			var block = new BlockCnt();
			OpenFunc( fs, block );
			fs.Proto.IsVarArg = true; // main func is always vararg
			InitExp( v, ExpKind.VLOCAL, 0 );
			NewUpvalue( fs, LuaDef.LUA_ENV, v );
			Lexer.Next(); // read first token
			StatList();
			// check TK_EOS
			CloseFunc();
		}

		private bool BlockFollow( bool withUntil )
		{
			switch( Lexer.Token.TokenType )
			{
				case (int)TK.ELSE:
				case (int)TK.ELSEIF:
				case (int)TK.END:
				case (int)TK.EOS:
					return true;

				case (int)TK.UNTIL:
					return withUntil;

				default:
					return false;
			}
		}

		private bool TestNext( int tokenType )
		{
			if( Lexer.Token.TokenType == tokenType )
			{
				Lexer.Next();
				return true;
			}
			else return false;
		}

		private void Check( int tokenType )
		{
			if( Lexer.Token.TokenType != tokenType )
				ErrorExpected( tokenType );
		}

		private void CheckNext( int tokenType )
		{
			Check( tokenType );
			Lexer.Next();
		}

		private void CheckCondition( bool cond, string msg )
		{
			if( !cond )
				Lexer.SyntaxError( msg );
		}

		private void EnterLevel()
		{
			++Lua.NumCSharpCalls;
			CheckLimit( CurFunc, Lua.NumCSharpCalls,
				LuaLimits.LUAI_MAXCCALLS, "C# levels" );
		}

		private void LeaveLevel()
		{
			--Lua.NumCSharpCalls;
		}

		private void SemanticError( string msg )
		{
			// TODO
			Lexer.SyntaxError( msg );
		}

		private void ErrorLimit( FuncState fs, int limit, string what )
		{
			int line = fs.Proto.LineDefined;
			string where = (line == 0)
				? "main function"
				: string.Format("function at line {0}", line);
			string msg = string.Format("too many {0} (limit is {1}) in {2}",
				what, limit, where);
			Lexer.SyntaxError( msg );
		}

		private void CheckLimit( FuncState fs, int v, int l, string what )
		{
			if( v > l )
				ErrorLimit( fs, l, what );
		}

		private int RegisterLocalVar( string varname )
		{
			var v = new LocVar();
			v.VarName = varname;
			v.StartPc = 0;
			v.EndPc   = 0;
			CurFunc.Proto.LocVars.Add(v);
			return CurFunc.Proto.LocVars.Count - 1;
		}

		private VarDesc NewLocalVar( string name )
		{
			var fs = CurFunc;
			int reg = RegisterLocalVar( name );
			CheckLimit( fs, ActVars.Count + 1 - fs.FirstLocal,
				MAXVARS, "local variables");
			var v = new VarDesc();
			v.Index = reg;
			return v;
		}

		private LocVar GetLocalVar( FuncState fs, int i )
		{
			int idx = ActVars[fs.FirstLocal + i].Index;
			Utl.Assert( idx < fs.Proto.LocVars.Count );
			return fs.Proto.LocVars[idx];
		}

		private void AdjustLocalVars( int nvars )
		{
			var fs = CurFunc;
			fs.NumActVar += nvars;
			for( ; nvars > 0; --nvars )
			{
				var v = GetLocalVar( fs, fs.NumActVar - nvars );
				v.StartPc = fs.Pc;
			}
		}

		private void RemoveVars( FuncState fs, int toLevel )
		{
			var len = fs.NumActVar - toLevel;
			while( fs.NumActVar > toLevel )
			{
				var v = GetLocalVar( fs, --fs.NumActVar );
				v.EndPc = fs.Pc;
			}
			ActVars.RemoveRange( ActVars.Count-len, len );
		}

		private void CloseGoto( int g, LabelDesc label )
		{
			var gt = PendingGotos[g];
			Utl.Assert( gt.Name == label.Name );
			if( gt.NumActVar < label.NumActVar )
			{
				var v = GetLocalVar( CurFunc, gt.NumActVar );
				var msg = string.Format(
					"<goto {0}> at line {1} jumps into the scope of local '{2}'",
					gt.Name, gt.Line, v.VarName);
				SemanticError( msg );
			}
			Coder.PatchList( CurFunc, gt.Pc, label.Pc );
			
			PendingGotos.RemoveAt(g);
		}

		// try to close a goto with existing labels; this solves backward jumps
		private bool FindLabel( int g )
		{
			var gt = PendingGotos[g];
			var block = CurFunc.Block;
			for( int i=block.FirstLabel; i<ActiveLabels.Count; ++i )
			{
				var label = ActiveLabels[i];

				// correct label?
				if( label.Name == gt.Name )
				{
					if( gt.NumActVar > label.NumActVar &&
						(block.HasUpValue || ActiveLabels.Count > block.FirstLabel) )
					{
						Coder.PatchClose( CurFunc, gt.Pc, label.NumActVar );
					}
					CloseGoto( g, label );
					return true;
				}
			}
			return false;
		}

		private LabelDesc NewLebelEntry( string name, int line, int pc )
		{
			var desc = new LabelDesc();
			desc.Name 		= name;
			desc.Pc			= pc;
			desc.Line		= line;
			desc.NumActVar	= CurFunc.NumActVar;
			return desc;
		}

		// check whether new label `label' matches any pending gotos in current
		// block; solves forward jumps
		private void FindGotos( LabelDesc label )
		{
			int i = CurFunc.Block.FirstGoto;
			while( i < PendingGotos.Count )
			{
				if( PendingGotos[i].Name == label.Name )
					CloseGoto( i, label );
				else
					++i;
			}
		}

		// "export" pending gotos to outer level, to check them against
		// outer labels; if the block being exited has upvalues, and
		// the goto exits the scope of any variable (which can be the
		// upvalue), close those variables being exited.
		private void MoveGotosOut( FuncState fs, BlockCnt block )
		{
			int i = block.FirstGoto;

			// correct pending gotos to current block and try to close it
			// with visible labels
			while( i < PendingGotos.Count )
			{
				var gt = PendingGotos[i];
				if( gt.NumActVar > block.NumActVar )
				{
					if( block.HasUpValue )
						Coder.PatchClose( fs, gt.Pc, block.NumActVar );
					gt.NumActVar = block.NumActVar;
				}
				if( !FindLabel(i) )
					++i; // move to next one
			}
		}

		// create a label named `break' to resolve break statements
		private void BreakLabel()
		{
			var desc = NewLebelEntry( "break", 0, CurFunc.Pc );
			ActiveLabels.Add( desc );
			FindGotos( ActiveLabels[ActiveLabels.Count-1] );
		}

		// generates an error for an undefined `goto'; choose appropriate
		// message when label name is a reserved word (which can only be `break')
		private void UndefGoto( LabelDesc gt )
		{
			string template = Lexer.IsReservedWord( gt.Name )
				? "<{0}> at line {1} not inside a loop"
				: "no visible label '{0}' for <goto> at line {1}";
			var msg = string.Format( template, gt.Name, gt.Line );
			SemanticError( msg );
		}

		private void EnterBlock( FuncState fs, BlockCnt block, bool isLoop )
		{
			block.IsLoop 		= isLoop;
			block.NumActVar 	= fs.NumActVar;
			block.FirstLabel 	= ActiveLabels.Count;
			block.FirstGoto 	= PendingGotos.Count;
			block.HasUpValue 	= false;
			block.Previous 		= fs.Block;
			fs.Block 			= block;
			Utl.Assert( fs.FreeReg == fs.NumActVar );
		}

		private void LeaveBlock( FuncState fs )
		{
			var block = fs.Block;

			if( block.Previous != null && block.HasUpValue )
			{
				int j = Coder.Jump( fs );
				Coder.PatchClose( fs, j, block.NumActVar );
				Coder.PatchToHere( fs, j );
			}

			if( block.IsLoop )
				BreakLabel();

			fs.Block = block.Previous;
			RemoveVars( fs, block.NumActVar );
			Utl.Assert( block.NumActVar == fs.NumActVar );
			fs.FreeReg =  fs.NumActVar; // free registers
			ActiveLabels.RemoveRange( block.FirstLabel,
				ActiveLabels.Count - block.FirstLabel );

			// inner block?
			if( block.Previous != null )
				MoveGotosOut( fs, block );

			// pending gotos in outer block?
			else if( block.FirstGoto < PendingGotos.Count )
				UndefGoto( PendingGotos[block.FirstGoto] ); // error
		}

		private UnOpr GetUnOpr( int op )
		{
			switch( op )
			{
				case (int)TK.NOT:
					return UnOpr.NOT;
				case (int)'-':
					return UnOpr.MINUS;
				case (int)'#':
					return UnOpr.LEN;
				default:
					return UnOpr.NOUNOPR;
			}
		}

		private BinOpr GetBinOpr( int op )
		{
			switch( op )
			{
				case (int)'+': return BinOpr.ADD;
				case (int)'-': return BinOpr.SUB;
				case (int)'*': return BinOpr.MUL;
				case (int)'/': return BinOpr.DIV;
				case (int)'%': return BinOpr.MOD;
				case (int)'^': return BinOpr.POW;
				case (int)TK.CONCAT: return BinOpr.CONCAT;
				case (int)TK.NE: return BinOpr.NE;
				case (int)TK.EQ: return BinOpr.EQ;
				case (int)'<': return BinOpr.LT;
				case (int)TK.LE: return BinOpr.LE;
				case (int)'>': return BinOpr.GT;
				case (int)TK.GE: return BinOpr.GE;
				case (int)TK.AND: return BinOpr.AND;
				case (int)TK.OR: return BinOpr.OR;
				default: return BinOpr.NOBINOPR;
			}
		}

		private int GetBinOprLeftPrior( BinOpr opr )
		{
			switch( opr )
			{
				case BinOpr.ADD: return 6;
				case BinOpr.SUB: return 6;
				case BinOpr.MUL: return 7;
				case BinOpr.DIV: return 7;
				case BinOpr.MOD: return 7;
				case BinOpr.POW: return 10;
				case BinOpr.CONCAT: return 5;
				case BinOpr.EQ: return 3;
				case BinOpr.LT: return 3;
				case BinOpr.LE: return 3;
				case BinOpr.NE: return 3;
				case BinOpr.GT: return 3;
				case BinOpr.GE: return 3;
				case BinOpr.AND: return 2;
				case BinOpr.OR: return 1;
				case BinOpr.NOBINOPR:
					throw new Exception("GetBinOprLeftPrior(NOBINOPR)");
				default:
					throw new Exception("Unknown BinOpr");
			}
		}

		private int GetBinOprRightPrior( BinOpr opr )
		{
			switch( opr )
			{
				case BinOpr.ADD: return 6;
				case BinOpr.SUB: return 6;
				case BinOpr.MUL: return 7;
				case BinOpr.DIV: return 7;
				case BinOpr.MOD: return 7;
				case BinOpr.POW: return 9;
				case BinOpr.CONCAT: return 4;
				case BinOpr.EQ: return 3;
				case BinOpr.LT: return 3;
				case BinOpr.LE: return 3;
				case BinOpr.NE: return 3;
				case BinOpr.GT: return 3;
				case BinOpr.GE: return 3;
				case BinOpr.AND: return 2;
				case BinOpr.OR: return 1;
				case BinOpr.NOBINOPR:
					throw new Exception("GetBinOprRightPrior(NOBINOPR)");
				default:
					throw new Exception("Unknown BinOpr");
			}
		}

		private const int UnaryPrior = 8;

		// statlist -> { stat [';'] }
		private void StatList()
		{
			while( ! BlockFollow( true ) )
			{
				if( Lexer.Token.TokenType == (int)TK.RETURN )
				{
					Statement();
					return; // 'return' must be last statement
				}
				Statement();
			}
		}

		// fieldsel -> ['.' | ':'] NAME
		private void FieldSel( ExpDesc v )
		{
			var fs = CurFunc;
			var key = new ExpDesc();
			Coder.Exp2AnyRegUp( fs, v );
			Lexer.Next(); // skip the dot or colon
			CodeString( key, CheckName() );
			Coder.Indexed( fs, v, key );
		}

		// cond -> exp
		private int Cond()
		{
			var v = new ExpDesc();
			Expr( v ); // read condition

			// `falses' are all equal here
			if( v.Kind == ExpKind.VNIL )
				v.Kind = ExpKind.VFALSE;

			Coder.GoIfTrue( CurFunc, v );
			return v.ExitFalse;
		}

		private void GotoStat( int pc )
		{
			string label;
			if( TestNext( (int)TK.GOTO ) )
				label = CheckName();
			else
			{
				Lexer.Next();
				label = "break";
			}

			PendingGotos.Add( NewLebelEntry( label, Lexer.LineNumber, pc ) );

			// close it if label already defined
			FindLabel( PendingGotos.Count-1 );
		}

		// check for repeated labels on the same block
		private void CheckRepeated( FuncState fs, List<LabelDesc> list, string label )
		{
			for( int i = fs.Block.FirstLabel; i < list.Count; ++i )
			{
				if( label == list[i].Name )
				{
					SemanticError( string.Format(
						"label '{0}' already defined on line {1}",
						label, list[i].Line ) );
				}
			}
		}

		// skip no-op statements
		private void SkipNoOpStat()
		{
			while( Lexer.Token.TokenType == (int)';' ||
				   Lexer.Token.TokenType == (int)TK.DBCOLON )
			{
				Statement();
			}
		}

		private void LabelStat( string label, int line )
		{
			var fs = CurFunc;
			CheckRepeated( fs, ActiveLabels, label );
			CheckNext( (int)TK.DBCOLON );

			var desc = NewLebelEntry( label, line, fs.Pc );
			ActiveLabels.Add( desc );
			SkipNoOpStat();
			if( BlockFollow( false ) )
			{
				// assume that locals are already out of scope
				desc.NumActVar = fs.Block.NumActVar;
			}
			FindGotos( desc );
		}

		// whilestat -> WHILE cond DO block END
		private void WhileStat( int line )
		{
			var fs = CurFunc;
			var block = new BlockCnt();

			Lexer.Next(); // skip WHILE
			int whileInit = Coder.GetLabel( fs );
			int condExit = Cond();
			EnterBlock( fs, block, true );
			CheckNext( (int)TK.DO );
			Block();
			Coder.JumpTo( fs, whileInit );
			CheckMatch( (int)TK.END, (int)TK.WHILE, line );
			LeaveBlock( fs );
			Coder.PatchToHere( fs, condExit );
		}

		// repeatstat -> REPEAT block UNTIL cond
		private void RepeatStat( int line )
		{
			var fs = CurFunc;
			int repeatInit = Coder.GetLabel( fs );
			var blockLoop  = new BlockCnt();
			var blockScope = new BlockCnt();
			EnterBlock( fs, blockLoop, true );
			EnterBlock( fs, blockScope, false );
			Lexer.Next();
			StatList();
			CheckMatch( (int)TK.UNTIL, (int)TK.REPEAT, line );
			int condExit = Cond();
			if( blockScope.HasUpValue )
			{
				Coder.PatchClose( fs, condExit, blockScope.NumActVar );
			}
			LeaveBlock( fs );
			Coder.PatchList( fs, condExit, repeatInit ); // close the loop
			LeaveBlock( fs );
		}

		private int Exp1()
		{
			var e = new ExpDesc();
			Expr( e );
			Coder.Exp2NextReg( CurFunc, e );
			Utl.Assert( e.Kind == ExpKind.VNONRELOC );
			return e.Info;
		}

		// forbody -> DO block
		private void ForBody( int t, int line, int nvars, bool isnum )
		{
			var fs = CurFunc;
			var block = new BlockCnt();
			AdjustLocalVars( 3 ); // control variables
			CheckNext( (int)TK.DO );
			int prep = isnum ? Coder.CodeAsBx( fs, OpCode.OP_FORPREP, t, Coder.NO_JUMP )
				: Coder.Jump( fs );
			EnterBlock( fs, block, false );
			AdjustLocalVars( nvars );
			Coder.ReserveRegs( fs, nvars );
			Block();
			LeaveBlock( fs );
			Coder.PatchToHere( fs, prep );

			int endfor;
			if( isnum ) // numeric for?
			{
				endfor = Coder.CodeAsBx( fs, OpCode.OP_FORLOOP, t, Coder.NO_JUMP );
			}
			else // generic for
			{
				Coder.CodeABC( fs, OpCode.OP_TFORCALL, t, 0, nvars );
				Coder.FixLine( fs, line );
				endfor = Coder.CodeAsBx( fs, OpCode.OP_TFORLOOP, t+2, Coder.NO_JUMP );
			}
			Coder.PatchList( fs, endfor, prep+1 );
			Coder.FixLine( fs, line );
		}

		// fornum -> NAME = exp1,expe1[,exp1] forbody
		private void ForNum( string varname, int line )
		{
			var fs = CurFunc;
			var save = fs.FreeReg;
			ActVars.Add( NewLocalVar("(for index)") );
			ActVars.Add( NewLocalVar("(for limit)") );
			ActVars.Add( NewLocalVar("(for step)") );
			ActVars.Add( NewLocalVar(varname) );
			CheckNext( (int)'=' );
			Exp1(); // initial value
			CheckNext( (int)',' );
			Exp1(); // limit
			if( TestNext( (int)',' ) )
			{
				Exp1(); // optional step
			}
			else // default step = 1
			{
				Coder.CodeK( fs, fs.FreeReg, Coder.NumberK( fs, 1 ) );
				Coder.ReserveRegs( fs, 1 );
			}
			ForBody( save, line, 1, true );
		}

		// forlist -> NAME {,NAME} IN explist forbody
		private void ForList( string indexName )
		{
			var fs = CurFunc;
			var e = new ExpDesc();
			int nvars = 4; // gen, state, control, plus at least one declared var
			int save = fs.FreeReg;

			// create control variables
			ActVars.Add( NewLocalVar("(for generator)") );
			ActVars.Add( NewLocalVar("(for state)") );
			ActVars.Add( NewLocalVar("(for control)") );

			// create declared variables
			ActVars.Add( NewLocalVar( indexName ) );
			while( TestNext( (int)',' ) )
			{
				ActVars.Add( NewLocalVar( CheckName() ) );
				nvars++;
			}
			CheckNext( (int)TK.IN );
			int line = Lexer.LineNumber;
			AdjustAssign( 3, ExpList(e), e );
			Coder.CheckStack( fs, 3 ); // extra space to call generator
			ForBody( save, line, nvars-3, false );
		}

		// forstat -> FOR (fornum | forlist) END
		private void ForStat( int line )
		{
			var fs = CurFunc;
			var block = new BlockCnt();
			EnterBlock( fs, block, true );
			Lexer.Next(); // skip `for'
			string varname = CheckName();
			switch( Lexer.Token.TokenType )
			{
				case (int)'=': ForNum( varname, line ); break;
				case (int)',': case (int)TK.IN: ForList( varname ); break;
				default: Lexer.SyntaxError("'=' or 'in' expected"); break;
			}
			CheckMatch( (int)TK.END, (int)TK.FOR, line );
			LeaveBlock( fs );
		}

		// test_then_block -> [IF | ELSEIF] cond THEN block
		private int TestThenBlock( int escapeList )
		{
			var fs = CurFunc;
			var block = new BlockCnt();
			int jf; // instruction to skip `then' code (if condition is false)

			// skip IF or ELSEIF
			Lexer.Next();

			// read condition
			var v = new ExpDesc();
			Expr( v );

			CheckNext( (int)TK.THEN );
			if( Lexer.Token.TokenType == (int)TK.GOTO ||
				Lexer.Token.TokenType == (int)TK.BREAK )
			{
				// will jump to label if condition is true
				Coder.GoIfFalse( CurFunc, v );

				// must enter block before `goto'
				EnterBlock( fs, block, false );

				// handle goto/break
				GotoStat( v.ExitTrue );

				// skip other no-op statements
				SkipNoOpStat();

				// `goto' is the entire block?
				if( BlockFollow( false ) )
				{
					LeaveBlock( fs );
					return escapeList;
				}
				else
				{
					jf = Coder.Jump( fs );
				}
			}
			// regular case (not goto/break)
			else
			{
				// skip over block if condition is false
				Coder.GoIfTrue( CurFunc, v );
				EnterBlock( fs, block, false );
				jf = v.ExitFalse;
			}

			// `then' part
			StatList();
			LeaveBlock( fs );

			// followed by `else' or `elseif'
			if( Lexer.Token.TokenType == (int)TK.ELSE ||
				Lexer.Token.TokenType == (int)TK.ELSEIF )
			{
				// must jump over it
				escapeList = Coder.Concat( fs, escapeList, Coder.Jump(fs) );
			}
			Coder.PatchToHere( fs, jf );
			return escapeList;
		}

		// ifstat -> IF cond THEN block {ELSEIF cond THEN block} [ELSE block] END
		private void IfStat( int line )
		{
			var fs = CurFunc;

			// exit list for finished parts
			int escapeList = Coder.NO_JUMP;

			// IF cond THEN block
			escapeList = TestThenBlock( escapeList );

			// ELSEIF cond THEN block
			while( Lexer.Token.TokenType == (int)TK.ELSEIF )
				escapeList = TestThenBlock( escapeList );

			// `else' part
			if( TestNext( (int)TK.ELSE ) )
				Block();

			CheckMatch( (int)TK.END, (int)TK.IF, line );
			Coder.PatchToHere( fs, escapeList );
		}

		private void LocalFunc()
		{
			var b = new ExpDesc();
			var fs = CurFunc;
			var v = NewLocalVar(CheckName());
			ActVars.Add(v);
			AdjustLocalVars(1); /// enter its scope
			Body( b, false, Lexer.LineNumber );
			GetLocalVar( fs, b.Info ).StartPc = fs.Pc;
		}

		// stat -> LOCAL NAME {`,' NAME} [`=' explist]
		private void LocalStat()
		{
			int nvars = 0;
			int nexps;
			var e = new ExpDesc();
			do {
				var v = NewLocalVar( CheckName() );
				ActVars.Add(v);
				++nvars;
			} while( TestNext( (int)',' ) );

			if( TestNext( (int)'=' ) )
				nexps = ExpList( e );
			else
			{
				e.Kind = ExpKind.VVOID;
				nexps = 0;
			}
			AdjustAssign( nvars, nexps, e );
			AdjustLocalVars( nvars );
		}

		// funcname -> NAME {fieldsel} [`:' NAME]
		private bool FuncName( ExpDesc v )
		{
			SingleVar( v );
			while( Lexer.Token.TokenType == (int)'.' )
			{
				FieldSel( v );
			}
			if( Lexer.Token.TokenType == (int)':' )
			{
				FieldSel( v );
				return true; // is method
			}
			else
			{
				return false;
			}
		}

		// funcstat -> FUNCTION funcname BODY
		private void FuncStat( int line )
		{
			var v = new ExpDesc();
			var b = new ExpDesc();
			Lexer.Next();
			bool isMethod = FuncName( v );
			Body( b, isMethod, line );
			Coder.StoreVar( CurFunc, v, b );
			Coder.FixLine( CurFunc, line );
		}

		// stat -> func | assignment
		private void ExprStat()
		{
			var v = new LHSAssign();
			SuffixedExp( v.Exp );

			// stat -> assignment ?
			if( Lexer.Token.TokenType == (int)'=' ||
				Lexer.Token.TokenType == (int)',' )
			{
				v.Prev = null;
				Assignment( v, 1 );
			}

			// stat -> func
			else
			{
				if( v.Exp.Kind != ExpKind.VCALL )
					Lexer.SyntaxError("syntax error");

				var pi = CurFunc.GetCode( v.Exp );
				pi.Value = pi.Value.SETARG_C( 1 ); // call statment uses no results
			}
		}

		// stat -> RETURN [explist] [';']
		private void RetStat()
		{
			var fs = CurFunc;
			int first, nret; // registers with returned values
			if( BlockFollow( true ) || Lexer.Token.TokenType == (int)';' )
			{
				first = nret = 0; // return no values
			}
			else
			{
				var e = new ExpDesc();
				nret = ExpList( e );
				if( HasMultiRet( e.Kind ) )
				{
					Coder.SetMultiRet( fs, e );
					if( e.Kind == ExpKind.VCALL && nret == 1 ) // tail call?
					{
						var pi = fs.GetCode(e);
						pi.Value = pi.Value.SET_OPCODE( OpCode.OP_TAILCALL );
						Utl.Assert( pi.Value.GETARG_A() == fs.NumActVar );
					}
					first = fs.NumActVar;
					nret = LuaDef.LUA_MULTRET;
				}
				else
				{
					if( nret == 1 ) // only one single value
					{
						first = Coder.Exp2AnyReg( fs, e );
					}
					else
					{
						Coder.Exp2NextReg( fs, e ); // values must go to the `stack'
						first = fs.NumActVar;
						Utl.Assert( nret == fs.FreeReg - first );
					}
				}
			}
			Coder.Ret( fs, first, nret );
			TestNext( (int)';' ); // skip optional semicolon
		}

		private void Statement()
		{
			// ULDebug.Log("Statement ::" + Lexer.Token);
			int line = Lexer.LineNumber;
			EnterLevel();
			switch( Lexer.Token.TokenType )
			{
				case (int)';': {
					Lexer.Next();
					break;
				}

				// stat -> ifstat
				case (int)TK.IF: {
					IfStat( line );
					break;
				}

				// stat -> whilestat
				case (int)TK.WHILE: {
					WhileStat( line );
					break;
				}

				// stat -> DO block END
				case (int)TK.DO: {
					Lexer.Next();
					Block();
					CheckMatch( (int)TK.END, (int)TK.DO, line );
					break;
				}

				// stat -> forstat
				case (int)TK.FOR: {
					ForStat( line );
					break;
				}

				// stat -> repeatstat
				case (int)TK.REPEAT: {
					RepeatStat( line );
					break;
				}

				// stat -> funcstat
				case (int)TK.FUNCTION: {
					FuncStat( line );
					break;
				}

				// stat -> localstat
				case (int)TK.LOCAL: {
					Lexer.Next();

					// local function?
					if( TestNext( (int)TK.FUNCTION ) )
						LocalFunc();
					else
						LocalStat();
					break;
				}

				// stat -> label
				case (int)TK.DBCOLON: {
					Lexer.Next(); // skip double colon
					LabelStat( CheckName(), line );
					break;
				}

				// stat -> retstat
				case (int)TK.RETURN: {
					Lexer.Next(); // skip RETURN
					RetStat();
					break;
				}

				// stat -> breakstat
				// stat -> 'goto' NAME
				case (int)TK.BREAK:
				case (int)TK.GOTO: {
					GotoStat( Coder.Jump( CurFunc ) );
					break;
				}

				// stat -> func | assignment
				default:
					ExprStat();
					break;
			}
			// ULDebug.Log( "MaxStackSize: " + CurFunc.Proto.MaxStackSize );
			// ULDebug.Log( "FreeReg: " + CurFunc.FreeReg );
			// ULDebug.Log( "NumActVar: " + CurFunc.NumActVar );
			Utl.Assert( CurFunc.Proto.MaxStackSize >= CurFunc.FreeReg &&
						CurFunc.FreeReg >= CurFunc.NumActVar );
			CurFunc.FreeReg = CurFunc.NumActVar; // free registers
			LeaveLevel();
		}

		private string CheckName()
		{
			// ULDebug.Log( Lexer.Token );
			var t = Lexer.Token as NameToken;

			// TEST CODE
			if( t == null )
			{
				ULDebug.LogError( Lexer.LineNumber + ":" + Lexer.Token );
			}
			string name = t.SemInfo;
			Lexer.Next();
			return name;
		}

		private int SearchVar( FuncState fs, string name )
		{
			for( int i=fs.NumActVar-1; i>=0; i-- )
			{
				if( name == GetLocalVar( fs, i ).VarName )
					return i;
			}
			return -1; // not found
		}

		private void MarkUpvalue( FuncState fs, int level )
		{
			var block = fs.Block;
			while( block.NumActVar > level ) block = block.Previous;
			block.HasUpValue = true;
		}

		private ExpKind SingleVarAux( FuncState fs, string name, ExpDesc e, bool flag )
		{
			if( fs == null )
				return ExpKind.VVOID;

			// look up locals at current level
			int v = SearchVar( fs, name );
			if( v >= 0 )
			{
				InitExp( e, ExpKind.VLOCAL, v );
				if( !flag )
					MarkUpvalue( fs, v ); // local will be used as an upval
				return ExpKind.VLOCAL;
			}

			// not found as local at current level; try upvalues
			int idx = SearchUpvalues( fs, name );
			if( idx < 0 ) // not found?
			{
				if( SingleVarAux( fs.Prev, name, e, false ) == ExpKind.VVOID )
					return ExpKind.VVOID; // not found; is a global
				idx = NewUpvalue( fs, name, e );
			}
			InitExp( e, ExpKind.VUPVAL, idx );
			return ExpKind.VUPVAL;
		}

		private void SingleVar( ExpDesc e )
		{
			string name = CheckName();
			if( SingleVarAux( CurFunc, name, e, true ) == ExpKind.VVOID )
			{
				ExpDesc key = new ExpDesc();
				SingleVarAux( CurFunc, LuaDef.LUA_ENV, e, true );
				Utl.Assert( e.Kind == ExpKind.VLOCAL ||
							e.Kind == ExpKind.VUPVAL );
				CodeString( key, name );
				Coder.Indexed( CurFunc, e, key );
			}
		}

		private void AdjustAssign( int nvars, int nexps, ExpDesc e )
		{
			var fs = CurFunc;
			int extra = nvars - nexps;
			if( HasMultiRet( e.Kind ) )
			{
				// includes call itself
				++extra;
				if( extra < 0 )
					extra = 0;
				Coder.SetReturns( fs, e, extra );
				if( extra > 1 )
					Coder.ReserveRegs( fs, extra-1 );
			}
			else
			{
				if( e.Kind != ExpKind.VVOID )
					Coder.Exp2NextReg( fs, e ); // close last expression
				if( extra > 0 )
				{
					int reg = fs.FreeReg;
					Coder.ReserveRegs( fs, extra );
					Coder.CodeNil( fs, reg, extra );
				}
			}
		}

		// check whether, in an assignment to an upvalue/local variable, the
		// upvalue/local variable is begin used in a previous assignment to a
		// table. If so, save original upvalue/local value in a safe place and
		// use this safe copy in the previous assignment.
		private void CheckConflict( LHSAssign lh, ExpDesc v )
		{
			var fs = CurFunc;

			// eventual position to save local variable
			int extra = fs.FreeReg;
			bool conflict = false;

			// check all previous assignments
			for( ; lh != null; lh = lh.Prev )
			{
				var e = lh.Exp;
				// assign to a table?
				if( e.Kind == ExpKind.VINDEXED )
				{
					// table is the upvalue/local being assigned now?
					if( e.Ind.Vt == v.Kind && e.Ind.T == v.Info )
					{
						conflict = true;
						e.Ind.Vt = ExpKind.VLOCAL;
						e.Ind.T  = extra; // previous assignment will use safe copy
					}
					// index is the local being assigned? (index cannot be upvalue)
					if( v.Kind == ExpKind.VLOCAL && e.Ind.Idx == v.Info )
					{
						conflict = true;
						e.Ind.Idx = extra; // previous assignment will use safe copy
					}
				}
			}
			if( conflict )
			{
				// copy upvalue/local value to a temporary (in position 'extra')
				var op = (v.Kind == ExpKind.VLOCAL) ? OpCode.OP_MOVE : OpCode.OP_GETUPVAL;
				Coder.CodeABC( fs, op, extra, v.Info, 0 );
				Coder.ReserveRegs( fs, 1 );
			}
		}

		// assignment -> ',' suffixedexp assignment
		private void Assignment( LHSAssign lh, int nvars )
		{
			CheckCondition( ExpKindUtl.VKIsVar( lh.Exp.Kind ), "syntax error" );
			ExpDesc e = new ExpDesc();

			if( TestNext( (int)',' ) )
			{
				var nv = new LHSAssign();
				nv.Prev = lh;
				SuffixedExp( nv.Exp );
				if( nv.Exp.Kind != ExpKind.VINDEXED )
					CheckConflict( lh, nv.Exp );
				CheckLimit( CurFunc, nvars + Lua.NumCSharpCalls,
					LuaLimits.LUAI_MAXCCALLS, "C# levels" );
				Assignment( nv, nvars+1 );
			}
			else
			{
				CheckNext( (int)'=' );

				int nexps = ExpList( e );
				if( nexps != nvars )
				{
					AdjustAssign( nvars, nexps, e );
					if( nexps > nvars )
					{
						// remove extra values
						CurFunc.FreeReg -= (nexps - nvars);
					}
				}
				else
				{
					Coder.SetOneRet( CurFunc, e );
					Coder.StoreVar( CurFunc, lh.Exp, e );
					return;
				}
			}

			// default assignment
			InitExp( e, ExpKind.VNONRELOC, CurFunc.FreeReg-1 );
			Coder.StoreVar( CurFunc, lh.Exp, e );
		}

		private int ExpList( ExpDesc e )
		{
			int n = 1; // at least one expression
			Expr( e );
			while( TestNext( (int)',' ) )
			{
				Coder.Exp2NextReg( CurFunc, e );
				Expr( e );
				n++;
			}
			return n;
		}
		
		private void Expr( ExpDesc e )
		{
			SubExpr( e, 0 );
		}

		private BinOpr SubExpr( ExpDesc e, int limit )
		{
			// ULDebug.Log("SubExpr limit:" + limit);
			EnterLevel();
			UnOpr uop = GetUnOpr( Lexer.Token.TokenType );
			if( uop != UnOpr.NOUNOPR )
			{
				int line = Lexer.LineNumber;
				Lexer.Next();
				SubExpr( e, UnaryPrior );
				Coder.Prefix( CurFunc, uop, e, line );
			}
			else SimpleExp( e );

			// expand while operators have priorities higher than `limit'
			BinOpr op = GetBinOpr(Lexer.Token.TokenType );
			while( op != BinOpr.NOBINOPR && GetBinOprLeftPrior( op ) > limit )
			{
				// ULDebug.Log("op:" + op);
				int line = Lexer.LineNumber;
				Lexer.Next();
				Coder.Infix( CurFunc, op, e );

				// read sub-expression with higher priority
				ExpDesc e2 = new ExpDesc();
				BinOpr nextOp = SubExpr( e2, GetBinOprRightPrior( op ) );
				Coder.Posfix( CurFunc, op, e, e2, line );
				op = nextOp;
			}

			LeaveLevel();
			return op;
		}

		private bool HasMultiRet( ExpKind k )
		{
			return k == ExpKind.VCALL || k == ExpKind.VVARARG;
		}

		private void ErrorExpected( int token )
		{
			Lexer.SyntaxError( string.Format( "{0} expected",
				((char)token).ToString() ) );
		}

		private void CheckMatch( int what, int who, int where )
		{
			if( !TestNext( what ) )
			{
				if( where == Lexer.LineNumber )
					ErrorExpected( what );
				else
					Lexer.SyntaxError( string.Format(
						"{0} expected (to close {1} at line {2})",
						((char)what).ToString(),
						((char)who).ToString(),
						where ) );
			}
		}

		// block -> statlist
		private void Block()
		{
			var fs = CurFunc;
			var block = new BlockCnt();
			EnterBlock( fs, block, false );
			StatList();
			LeaveBlock( fs );
		}

		// index -> '[' expr ']'
		private void YIndex( ExpDesc v )
		{
			Lexer.Next();
			Expr( v );
			Coder.Exp2Val( CurFunc, v );
			CheckNext( (int)']' );
		}

		// recfield -> (NAME | '[' exp1 ']') = exp1
		private void RecField( ConstructorControl cc )
		{
			var fs = CurFunc;
			int reg = fs.FreeReg;
			var key = new ExpDesc();
			var val = new ExpDesc();
			if( Lexer.Token.TokenType == (int)TK.NAME )
			{
				CheckLimit( fs, cc.NumRecord, LuaLimits.MAX_INT,
					"items in a constructor" );
				CodeString( key, CheckName() );
			}
			// ls->t.token == '['
			else
			{
				YIndex( key );
			}
			cc.NumRecord++;
			CheckNext( (int)'=' );
			int rkkey = Coder.Exp2RK( fs, key );
			Expr( val );
			Coder.CodeABC( fs, OpCode.OP_SETTABLE, cc.ExpTable.Info, rkkey,
				Coder.Exp2RK( fs, val ) );
			fs.FreeReg = reg; // free registers
		}

		private void CloseListField( FuncState fs, ConstructorControl cc )
		{
			// there is no list item
			if( cc.ExpLastItem.Kind == ExpKind.VVOID )
				return;

			Coder.Exp2NextReg( fs, cc.ExpLastItem );
			cc.ExpLastItem.Kind = ExpKind.VVOID;
			if( cc.NumToStore == LuaDef.LFIELDS_PER_FLUSH )
			{
				// flush
				Coder.SetList( fs, cc.ExpTable.Info, cc.NumArray, cc.NumToStore );

				// no more item pending
				cc.NumToStore = 0;
			}
		}

		private void LastListField( FuncState fs, ConstructorControl cc )
		{
			if( cc.NumToStore == 0 )
				return;

			if( HasMultiRet( cc.ExpLastItem.Kind ) )
			{
				Coder.SetMultiRet( fs, cc.ExpLastItem );
				Coder.SetList( fs, cc.ExpTable.Info, cc.NumArray, LuaDef.LUA_MULTRET );

				// do not count last expression (unknown number of elements)
				cc.NumArray--;
			}
			else
			{
				if( cc.ExpLastItem.Kind != ExpKind.VVOID )
					Coder.Exp2NextReg( fs, cc.ExpLastItem );
				Coder.SetList( fs, cc.ExpTable.Info, cc.NumArray, cc.NumToStore );
			}
		}

		// listfield -> exp
		private void ListField( ConstructorControl cc )
		{
			Expr( cc.ExpLastItem );
			CheckLimit( CurFunc, cc.NumArray, LuaLimits.MAX_INT,
				"items in a constructor" );
			cc.NumArray++;
			cc.NumToStore++;
		}

		// field -> listfield | recfield
		private void Field( ConstructorControl cc )
		{
			switch( Lexer.Token.TokenType )
			{
				// may be 'listfield' or 'recfield'
				case (int)TK.NAME: {
					// expression?
					if( Lexer.GetLookAhead().TokenType != (int)'=' )
						ListField( cc );
					else
						RecField( cc );
					break;
				}

				case (int)'[': {
					RecField( cc );
					break;
				}

				default: {
					ListField( cc );
					break;
				}
			}
		}

		// converts an integer to a "floating point byte", represented as
		// (eeeeexxx), where the real value is (1xxx) * 2^(eeeee - 1) if
		// eeeee != 0 and (xxx) otherwise.
		private int Integer2FloatingPointByte(uint x)
		{
			int e = 0; // exponent
			if( x < 8 )
				return (int)x;
			while( x >= 0x10 )
			{
				x = (x+1) >> 1;
				++e;
			}
			return ((e+1) << 3) | ((int)x - 8);
		}

		// constructor -> '{' [ field { sep field } [sep] ] '}'
		// sep -> ',' | ';'
		private void Constructor( ExpDesc t )
		{
			var fs = CurFunc;
			int line = Lexer.LineNumber;
			int pc = Coder.CodeABC( fs, OpCode.OP_NEWTABLE, 0, 0, 0 );
			var cc = new ConstructorControl();
			cc.ExpTable = t;
			InitExp( t, ExpKind.VRELOCABLE, pc );
			InitExp( cc.ExpLastItem, ExpKind.VVOID, 0); // no value (yet)
			Coder.Exp2NextReg( fs, t );
			CheckNext( (int)'{' );
			do {
				Utl.Assert( cc.ExpLastItem.Kind == ExpKind.VVOID ||
							cc.NumToStore > 0 );
				if( Lexer.Token.TokenType == (int)'}' )
					break;
				CloseListField( fs, cc );
				Field( cc );
			} while( TestNext( (int)',' ) || TestNext( (int)';' ) );
			CheckMatch( (int)'}', (int)'{', line );
			LastListField( fs, cc );

			// set initial array size and table size
			// 因为没有实现 OP_NEWTABLE 对 ARG_B 和 ARG_C 的处理, 所以这里也暂不实现
			// 不影响逻辑, 只是效率差别
			// var ins = fs.Proto.Code[pc];
			// ins.SETARG_B( 0 );
			// ins.SETARG_C( 0 );

			// 算了, 还是实现吧, 方便检查生成的 bytecode 是否跟 luac 一样
			var ins = fs.Proto.Code[pc];
			ins.SETARG_B( Integer2FloatingPointByte( (uint)cc.NumArray ) );
			ins.SETARG_C( Integer2FloatingPointByte( (uint)cc.NumRecord) );
			fs.Proto.Code[pc] = ins; // Instruction 是值类型的 唉
		}

		private void ParList()
		{
			int numParams = 0;
			CurFunc.Proto.IsVarArg = false;

			// is `parlist' not empty?
			if( Lexer.Token.TokenType != (int)')' )
			{
				do {
					switch( Lexer.Token.TokenType )
					{
						// param -> NAME
						case (int)TK.NAME: {
							var v =NewLocalVar( CheckName() );
							ActVars.Add( v );
							++numParams;
							break;
						}

						case (int)TK.DOTS: {
							Lexer.Next();
							CurFunc.Proto.IsVarArg = true;
							break;
						}

						default: {
							Lexer.SyntaxError("<name> or '...' expected");
							break;
						}
					}
				} while( !CurFunc.Proto.IsVarArg && TestNext( (int)',' ) );
			}
			AdjustLocalVars( numParams );
			CurFunc.Proto.NumParams = CurFunc.NumActVar;
			Coder.ReserveRegs( CurFunc, CurFunc.NumActVar );
		}

		private void Body( ExpDesc e, bool isMethod, int line )
		{
			var newFs = new FuncState();
			var block = new BlockCnt();
			newFs.Proto = AddPrototype();
			newFs.Proto.LineDefined = line;
			OpenFunc( newFs, block );
			CheckNext( (int)'(' );
			if( isMethod )
			{
				// create `self' parameter
				var v = NewLocalVar("self");
				ActVars.Add(v);
				AdjustLocalVars(1);
			}
			ParList();
			CheckNext( (int)')' );
			StatList();
			newFs.Proto.LastLineDefined = Lexer.LineNumber;
			CheckMatch( (int)TK.END, (int)TK.FUNCTION, line );
			CodeClosure(e);
			CloseFunc();
		}

		private void FuncArgs( ExpDesc e, int line )
		{
			var args = new ExpDesc();
			switch( Lexer.Token.TokenType )
			{
				// funcargs -> `(' [ explist ] `)'
				case (int)'(': {
					Lexer.Next();
					if( Lexer.Token.TokenType == ')' ) // arg list is empty?
						args.Kind = ExpKind.VVOID;
					else {
						ExpList( args );
						Coder.SetMultiRet( CurFunc, args );
					}
					CheckMatch( (int)')', (int)'(', line );
					break;
				}

				// funcargs -> constructor
				case (int)'{': {
					Constructor( args );
					break;
				}

				// funcargs -> STRING
				case (int)TK.STRING: {
					var st = Lexer.Token as StringToken;
					CodeString( args, st.SemInfo );
					Lexer.Next();
					break;
				}

				default: {
					Lexer.SyntaxError( "function arguments expected" );
					break;
				}
			}

			Utl.Assert( e.Kind == ExpKind.VNONRELOC );
			int baseReg = e.Info;
			int nparams;
			if( HasMultiRet( args.Kind ) )
				nparams = LuaDef.LUA_MULTRET;
			else {
				if( args.Kind != ExpKind.VVOID )
					Coder.Exp2NextReg( CurFunc, args ); // close last argument
				nparams = CurFunc.FreeReg - (baseReg+1);
			}
			InitExp( e, ExpKind.VCALL, Coder.CodeABC( CurFunc,
				OpCode.OP_CALL, baseReg, nparams+1, 2 ) );
			Coder.FixLine( CurFunc, line );

			// call remove function and arguments and leaves
			// (unless changed) one result
			CurFunc.FreeReg = baseReg + 1;
		}

		// ==============================================================
		// Expression parsing
		// ==============================================================

		// primaryexp -> NAME | '(' expr ')'
		private void PrimaryExp( ExpDesc e )
		{
			switch( Lexer.Token.TokenType )
			{
				case (int)'(': {
					int line = Lexer.LineNumber;
					Lexer.Next();
					Expr( e );
					CheckMatch( (int)')', (int)'(', line );
					Coder.DischargeVars( CurFunc, e );
					return;
				}

				case (int)TK.NAME: {
					SingleVar( e );
					return;
				}

				default: {
					Lexer.SyntaxError("unexpected symbol");
					return;
				}
			}
		}

		// suffixedexp -> primaryexp { '.' NAME | '[' exp ']' | ':' NAME funcargs | funcargs
		private void SuffixedExp( ExpDesc e )
		{
			var fs = CurFunc;
			int line = Lexer.LineNumber;
			PrimaryExp( e );
			for(;;)
			{
				switch( Lexer.Token.TokenType )
				{
					case (int)'.': { // fieldsel
						FieldSel( e );
						break;
					}
					case (int)'[': { // `[' exp1 `]'
						var key = new ExpDesc();
						Coder.Exp2AnyRegUp( fs, e );
						YIndex( key );
						Coder.Indexed( fs, e, key );
						break;
					}
					case (int)':': { // `:' NAME funcargs
						var key = new ExpDesc();
						Lexer.Next();
						CodeString( key, CheckName() );
						Coder.Self( fs, e, key );
						FuncArgs( e, line );
						break;
					}
					case (int)'(':
					case (int)TK.STRING:
					case (int)'{': { // funcargs
						Coder.Exp2NextReg( CurFunc, e );
						FuncArgs( e, line );
						break;
					}
					default: return;
				}
			}
		}

		private void SimpleExp( ExpDesc e )
		{
			var t = Lexer.Token;
			switch( t.TokenType )
			{
				case (int)TK.NUMBER: {
					var nt = t as NumberToken;
					InitExp( e, ExpKind.VKNUM, 0 );
					e.NumberValue = nt.SemInfo;
					break;
				}

				case (int)TK.STRING: {
					var st = t as StringToken;
					CodeString( e, st.SemInfo );
					break;
				}

				case (int)TK.NIL: {
					InitExp( e, ExpKind.VNIL, 0 );
					break;
				}

				case (int)TK.TRUE: {
					InitExp( e, ExpKind.VTRUE, 0 );
					break;
				}

				case (int)TK.FALSE: {
					InitExp( e, ExpKind.VFALSE, 0 );
					break;
				}

				case (int)TK.DOTS: {
					CheckCondition( CurFunc.Proto.IsVarArg,
						"cannot use '...' outside a vararg function" );
					InitExp( e, ExpKind.VVARARG,
						Coder.CodeABC( CurFunc, OpCode.OP_VARARG, 0, 1, 0 ) );
					break;
				}

				case (int)'{': {
					Constructor( e );
					return;
				}

				case (int)TK.FUNCTION: {
					Lexer.Next();
					Body( e, false, Lexer.LineNumber );
					return;
				}

				default: {
					SuffixedExp( e );
					return;
				}
			}
			Lexer.Next();
		}

		private int SearchUpvalues( FuncState fs, string name )
		{
			var upvalues = fs.Proto.Upvalues;
			for( int i=0; i< upvalues.Count; ++i )
			{
				if( upvalues[i].Name == name )
					return i;
			}
			return -1;
		}

		private int NewUpvalue( FuncState fs, string name, ExpDesc e )
		{
			var f = fs.Proto;
			int idx = f.Upvalues.Count;
			var upval = new UpvalDesc();
			upval.InStack = (e.Kind == ExpKind.VLOCAL);
			upval.Index = e.Info;
			upval.Name = name;
			f.Upvalues.Add( upval );
			return idx;
		}

		private void CodeString( ExpDesc e, string s )
		{
			InitExp( e, ExpKind.VK, Coder.StringK( CurFunc, s) );
		}

		private void InitExp( ExpDesc e, ExpKind k, int i )
		{
			e.Kind = k;
			e.Info = i;
			e.ExitTrue = Coder.NO_JUMP;
			e.ExitFalse = Coder.NO_JUMP;
		}
	}

}


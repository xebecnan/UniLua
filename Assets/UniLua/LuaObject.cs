
namespace UniLua
{
	using System;
	using System.Collections.Generic;
	using Debug = UniLua.Tools.Debug;

	public abstract class LuaObject
	{
		public virtual string ToLiteral()
		{
			return ToString();
		}

		public virtual double ToNumber( out bool isnum )
		{
			isnum = false;
			return 0.0;
		}

		public virtual LuaType LuaType
		{
			get { return LuaType.LUA_TNONE; }
		}

		public static bool operator ==( LuaObject lhs, LuaObject rhs )
		{
			if( System.Object.ReferenceEquals( lhs, rhs ) )
				return true;

			if( ((object)lhs == null) || ((object)rhs == null) )
				return false;
			
			return lhs.Equals( rhs );
		}

		public static bool operator !=( LuaObject lhs, LuaObject rhs )
		{
			return !(lhs == rhs);
		}

		public override bool Equals( object o )
		{
			if( !(o is LuaObject) )
				return false;

			return System.Object.ReferenceEquals( this, o );
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public virtual bool IsNil		{ get { return false; } }
		public virtual bool IsFalse		{ get { return false; } }
		public virtual bool IsFunction	{ get { return false; } }
		public virtual bool IsClosure	{ get { return false; } }
		public virtual bool IsString	{ get { return false; } }
		public virtual bool IsNumber	{ get { return false; } }
		public virtual bool IsTable		{ get { return false; } }
		public virtual bool IsThread	{ get { return false; } }
	}

	public class LuaNil : LuaObject
	{
		public override LuaType LuaType
		{
			get { return LuaType.LUA_TNIL; }
		}

		public override bool IsNil		{ get { return true; } }
		public override bool IsFalse	{ get { return true; } }

		public override int GetHashCode()
		{
			return 0;
		}

		public override bool Equals( object o )
		{
			if( o == null ) return false;

			return (object)(o as LuaNil) != null;
		}
	}

	public class LuaBoolean : LuaObject
	{
		public bool Value;

		public LuaBoolean( bool val )
		{
			Value = val;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TBOOLEAN; }
		}

		public override bool IsFalse	{ get { return !Value; } }

		public override bool Equals( object o )
		{
			if( o == null )
				return false;

			LuaBoolean p = o as LuaBoolean;
			if( (object)p == null )
				return false;

			return Value == p.Value;
		}
	}

	public class LocVar
	{
		public string VarName;
		public int StartPc;
		public int EndPc;
	}

	public class UpvalDesc
	{
		public string Name;
		public int Index;
		public bool InStack;
	}

	public class LuaProto : LuaObject
	{
		public List<Instruction> 	Code;
		public List<LuaObject>		K;
		public List<LuaProto>		P;
		public List<UpvalDesc>		Upvalues;

		public int					LineDefined;
		public int					LastLineDefined;

		public int					NumParams;
		public bool					IsVarArg;
		public byte					MaxStackSize;

		public string				Source;
		public List<int>			LineInfo;
		public List<LocVar>			LocVars;

		public LuaProto()
		{
			Code = new List<Instruction>();
			K = new List<LuaObject>();
			P = new List<LuaProto>();
			Upvalues = new List<UpvalDesc>();
			LineInfo = new List<int>();
			LocVars = new List<LocVar>();
		}

		public int GetFuncLine( int pc )
		{
			return (pc < LineInfo.Count) ? LineInfo[pc] : 0;
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TPROTO; }
		}
	}
	
	public class LuaUpvalue : LuaObject
	{
		public StkId			V;
		public List<LuaObject>	Value;
		// public LuaUpvalue		Next;
		// public LuaUpvalue		Prev;

		public LuaUpvalue()
		{
			Value = new List<LuaObject>(1);
			Value.Add( new LuaNil() );

			V = new StkId( Value, 0 );
		}

		public override string ToString()
		{
			return string.Format("[LuaUpvalue(self:{0} v:{1})]", V.Value == Value[0], V);
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TUPVAL; }
		}
	}

	public abstract class LuaClosure : LuaObject
	{
		public abstract ClosureType ClosureType { get; }

		public abstract string GetUpvalue( int n, out LuaObject val );

		public abstract string SetUpvalue( int n, LuaObject val );
	}

	public class LuaLClosure : LuaClosure
	{
		public LuaProto 		Proto;
		public List<LuaUpvalue>	Upvals;

		public LuaLClosure( LuaProto proto )
		{
			Proto = proto;

			Upvals = new List<LuaUpvalue>();
			for( int i=0; i<proto.Upvalues.Count; ++i )
			{
				Upvals.Add( null );
			}
		}

		public override ClosureType ClosureType { get { return ClosureType.LUA; } }

		public override string ToString()
		{
			return string.Format("[LuaLClosure(Upvalues:{0})]", Upvals.Count);
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TFUNCTION; }
		}

		public override bool IsFunction	{ get { return true; } }
		public override bool IsClosure	{ get { return true; } }

		public override string GetUpvalue( int n, out LuaObject val )
		{
			if( !(1 <= n && n <= Upvals.Count) )
			{
				val = null;
				return null;
			}
			else
			{
				val = Upvals[n-1].V.Value;
				string name = Proto.Upvalues[n-1].Name;
				return (name == null) ? "" : name;
			}
		}

		public override string SetUpvalue( int n, LuaObject val )
		{
			if( !(1 <= n && n <= Upvals.Count) )
				return null;
			else
			{
				Upvals[n-1].V.Value = val;
				string name = Proto.Upvalues[n-1].Name;
				return (name == null) ? "" : name;
			}
		}
	}

	public class LuaCSharpClosure : LuaClosure
	{
		public CSharpFunctionDelegate 	F;
		public List<LuaObject>			Upvals;

		public LuaCSharpClosure( CSharpFunctionDelegate f )
		{
			F = f;
		}

		public LuaCSharpClosure( CSharpFunctionDelegate f, int numUpvalues )
		{
			F = f;
			Upvals = new List<LuaObject>(numUpvalues);
		}

		public override ClosureType ClosureType { get { return ClosureType.CSHARP; } }

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TFUNCTION; }
		}

		public override bool IsFunction	{ get { return true; } }
		public override bool IsClosure	{ get { return true; } }

		public override string GetUpvalue( int n, out LuaObject val )
		{
			if( !(1 <= n && n <= Upvals.Count) )
			{
				val = null;
				return null;
			}
			else
			{
				val = Upvals[n-1];
				return "";
			}
		}

		public override string SetUpvalue( int n, LuaObject val )
		{
			if( !(1 <= n && n <= Upvals.Count) )
				return null;
			else
			{
				Upvals[n-1] = val;
				return "";
			}
		}
	}

	public class LuaString : LuaObject
	{
		public string Value;

		public LuaString( string val )
		{
			Value = val;
		}

		public override bool IsString	{ get { return true; } }

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==( LuaString lhs, LuaString rhs )
		{
			if( System.Object.ReferenceEquals( lhs, rhs ) )
				return true;

			if( ((object)lhs == null) || ((object)rhs == null) )
				return false;
			
			return lhs.Value == rhs.Value;
		}

		public static bool operator !=( LuaString lhs, LuaString rhs )
		{
			return !(lhs == rhs);
		}

		public override bool Equals( object o )
		{
			if( o == null )
				return false;

			LuaString p = o as LuaString;
			if( (object)p == null )
				return false;

			return Value == p.Value;
		}

		public override string ToString()
		{
			return "[LuaString(" + Value + ")]";
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TSTRING; }
		}

		// public LuaNumber ToLuaNumber()
		// {
		// 	if( Value.Contains( "n" ) || Value.Contains( "N" ) ) // reject 'inf' and 'nan'
		// 		return null;

		// 	if( Value.Contains( 'x' ) || Value.Contains( 'X' ) ) // hexa?

		// }
	}

	public class LuaUserData : LuaObject
	{
		public object Value;

		public LuaUserData( object val )
		{
			Value = val;
		}

		public LuaTable MetaTable { get; set; }
		public int Length { get { return 0; } } // TODO

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TUSERDATA; }
		}
	}

	public class LuaLightUserData : LuaObject
	{
		public object Value;

		public LuaLightUserData( object val )
		{
			Value = val;
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TLIGHTUSERDATA; }
		}
	}

	public class LuaUInt64 : LuaObject
	{
		public UInt64 Value;

		public LuaUInt64( UInt64 val )
		{
			Value = val;
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TUINT64; }
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==( LuaUInt64 lhs,
			LuaUInt64 rhs )
		{
			if( System.Object.ReferenceEquals( lhs, rhs ) )
				return true;

			if( ((object)lhs == null) || ((object)rhs == null) )
				return false;
			
			return lhs.Value == rhs.Value;
		}

		public static bool operator !=( LuaUInt64 lhs,
			LuaUInt64 rhs )
		{
			return !(lhs == rhs);
		}

		public override bool Equals( object o )
		{
			// UnityEngine.Debug.Log("LuaUInt64.Equals");
			if( o == null )
				return false;

			LuaUInt64 p = o as LuaUInt64;
			if( (object)p == null )
				return false;

			// UnityEngine.Debug.Log("this.Value:" + this.Value);
			// UnityEngine.Debug.Log("p.Value:" + p.Value);
			// UnityEngine.Debug.Log("this.Value == p.Value:" + (this.Value == p.Value));
			// UnityEngine.Debug.Log("this.Value == p.Value:" + ((UInt64)this.Value == (UInt64)p.Value));
			return Value == p.Value;
		}

		public override string ToString()
		{
			return "[LuaUInt64(" + Value + ")]";
		}
	}

	public class LuaNumber : LuaObject
	{
		public double Value;

		public LuaNumber( double val )
		{
			Value = val;
		}

		public override bool IsNumber	{ get { return true; } }

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==( LuaNumber lhs, LuaNumber rhs )
		{
			if( System.Object.ReferenceEquals( lhs, rhs ) )
				return true;

			if( ((object)lhs == null) || ((object)rhs == null) )
				return false;
			
			return lhs.Value == rhs.Value;
		}

		public static bool operator !=( LuaNumber lhs, LuaNumber rhs )
		{
			return !(lhs == rhs);
		}

		public override bool Equals( object o )
		{
			if( o == null )
				return false;

			LuaNumber p = o as LuaNumber;
			if( (object)p == null )
				return false;

			return Value == p.Value;
		}

		public override string ToString()
		{
			return "[LuaNumber(" + Value + ")]";
		}

		public override string ToLiteral()
		{
			return Value.ToString();
		}

		public override double ToNumber( out bool isnum )
		{
			isnum = true;
			return Value;
		}

		public static LuaNumber operator +( LuaNumber lhs, LuaNumber rhs )
		{
			return new LuaNumber( lhs.Value + rhs.Value );
		}

		public static LuaNumber operator -( LuaNumber lhs, LuaNumber rhs )
		{
			return new LuaNumber( lhs.Value - rhs.Value );
		}

		public static LuaNumber operator *( LuaNumber lhs, LuaNumber rhs )
		{
			return new LuaNumber( lhs.Value * rhs.Value );
		}

		public static LuaNumber operator /( LuaNumber lhs, LuaNumber rhs )
		{
			return new LuaNumber( lhs.Value / rhs.Value );
		}

		public override LuaType LuaType
		{
			get { return LuaType.LUA_TNUMBER; }
		}
	}

	public partial class LuaState
	{
		public override LuaType LuaType
		{
			get { return LuaType.LUA_TTHREAD; }
		}

		public override bool IsThread	{ get { return true; } }

		public static bool O_Str2Decimal( string s, out double result )
		{
			result = 0.0;

			if( s.Contains("n") || s.Contains("N") ) // reject `inf' and `nan'
				return false;

			int pos = 0;
			if( s.Contains("x") || s.Contains("X") )
				result = Utl.StrX2Number( s, ref pos );
			else
				result = Utl.Str2Number( s, ref pos );

			if( pos == 0 )
				return false; // nothing recognized

			while( pos < s.Length && Char.IsWhiteSpace( s[pos] ) ) ++pos;
			return pos == s.Length; // OK if no trailing characters
		}

		private static double O_Arith( LuaOp op, double v1, double v2 )
		{
			switch( op )
			{
				case LuaOp.LUA_OPADD: return v1+v2;
				case LuaOp.LUA_OPSUB: return v1-v2;
				case LuaOp.LUA_OPMUL: return v1*v2;
				case LuaOp.LUA_OPDIV: return v1/v2;
				case LuaOp.LUA_OPMOD: return v1%v2;
				case LuaOp.LUA_OPPOW: return Math.Pow(v1, v2);
				case LuaOp.LUA_OPUNM: return -v1;
				default: throw new System.NotImplementedException();
			}
		}
	}

}


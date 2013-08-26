﻿
// #define DEBUG_BINARY_READER
// #define DEBUG_UNDUMP

using System;

using ULDebug = UniLua.Tools.ULDebug;

namespace UniLua
{
	using PlatformCfg = PlatformCfgX32;
	using SizeT = UInt32;
	// using PlatformCfg = PlatformCfgX64;
	// using SizeT = UInt64;

	internal static class PlatformCfgX64
	{
		public const int SizeTypeLength = 8;

		public static UInt64 ToSizeT( byte[] bytes, int pos )
		{
			return BitConverter.ToUInt64( bytes, pos );
		}
	}

	internal static class PlatformCfgX32
	{
		public const int SizeTypeLength = 4;

		public static UInt32 ToSizeT( byte[] bytes, int pos )
		{
			return BitConverter.ToUInt32( bytes, pos );
		}
	}

	public class BinaryBytesReader
	{
		private ILoadInfo 	LoadInfo;

		public BinaryBytesReader( ILoadInfo loadinfo )
		{
			LoadInfo 	= loadinfo;
		}

		public byte[] ReadBytes( int count )
		{
			byte[] ret = new byte[count];
			for( int i=0; i<count; ++i )
			{
				var c = LoadInfo.ReadByte();
				if( c == -1 )
					throw new UndumpException("truncated");
				ret[i] = (byte)c;
			}
#if DEBUG_BINARY_READER
			var sb = new System.Text.StringBuilder();
			sb.Append("ReadBytes:");
			for( var i=0; i<ret.Length; ++i )
			{
				sb.Append( string.Format(" {0:X02}", ret[i]) );
			}
			ULDebug.Log( sb.ToString() );
#endif
			return ret;
		}

		public int ReadInt()
		{
			var bytes = ReadBytes( 4 );
			int ret = BitConverter.ToInt32( bytes, 0 );
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadInt: " + ret );
#endif
			return ret;
		}

		public uint ReadUInt()
		{
			var bytes = ReadBytes( 4 );
			uint ret = BitConverter.ToUInt32( bytes, 0 );
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadUInt: " + ret );
#endif
			return ret;
		}

		public SizeT ReadSizeT()
		{
			var bytes = ReadBytes( PlatformCfg.SizeTypeLength );
			var ret = PlatformCfg.ToSizeT( bytes, 0 );
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadSizeT: " + ret );
#endif
			return ret;
		}

		public double ReadDouble()
		{
			var bytes = ReadBytes( 8 );
			double ret = BitConverter.ToDouble( bytes, 0 );
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadDouble: " + ret );
#endif
			return ret;
		}

		public byte ReadByte()
		{
			var c = LoadInfo.ReadByte();
			if( c == -1 )
				throw new UndumpException("truncated");
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadBytes: " + c );
#endif
			return (byte)c;
		}

		public string ReadString()
		{
			var n = (int)ReadSizeT();
			if( n == 0 )
				return null;

			var bytes = ReadBytes( n );

			// n=1: removing trailing '\0'
			string ret = System.Text.Encoding.UTF8.GetString( bytes, 0, n-1 );
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadString n:" + n + " ret:" + ret );
#endif
			return ret;
		}
	}

	class UndumpException : Exception
	{
		public string Why;

		public UndumpException( string why )
		{
			Why = why;
		}
	}

	public class Undump
	{
		public static LuaProto LoadBinary( ILuaState lua,
			ILoadInfo loadinfo, string name )
		{
			try
			{
				var reader = new BinaryBytesReader( loadinfo );
				var undump = new Undump( reader );
				undump.LoadHeader();
				return undump.LoadFunction();
			}
			catch( UndumpException e )
			{
				var Lua = (LuaState)lua;
				Lua.O_PushString( string.Format(
					"{0}: {1} precompiled chunk", name, e.Why ) );
				Lua.D_Throw( ThreadStatus.LUA_ERRSYNTAX );
				return null;
			}
		}

		private BinaryBytesReader 	Reader;

		private Undump( BinaryBytesReader reader )
		{
			Reader 	= reader;
		}

		// private LuaProto Load()
		// {
		// 	LoadHeader();
		// 	return LoadFunction();
		// }

		private int LoadInt()
		{
			return Reader.ReadInt();
		}

		private byte LoadByte()
		{
			return Reader.ReadByte();
		}

		private byte[] LoadBytes( int count )
		{
			return Reader.ReadBytes( count );
		}

		private string LoadString()
		{
			return Reader.ReadString();
		}

		private bool LoadBoolean()
		{
			return LoadByte() != 0;
		}

		private double LoadNumber()
		{
			return Reader.ReadDouble();
		}

		private void LoadHeader()
		{
			LoadBytes( 4 // Signature
				+ 8 // version, format version, size of int ... etc
				+ 6 // Tail
			);
		}

		private Instruction LoadInstruction()
		{
			return (Instruction)Reader.ReadUInt();
		}

		private LuaProto LoadFunction()
		{
#if DEBUG_UNDUMP
			ULDebug.Log( "LoadFunction enter" );
#endif

			LuaProto proto = new LuaProto();
			proto.LineDefined = LoadInt();
			proto.LastLineDefined = LoadInt();
			proto.NumParams = LoadByte();
			proto.IsVarArg  = LoadBoolean();
			proto.MaxStackSize = LoadByte();

			LoadCode(proto);
			LoadConstants(proto);
			LoadUpvalues(proto);
			LoadDebug(proto);
			return proto;
		}

		private void LoadCode( LuaProto proto )
		{
			var n = LoadInt();
#if DEBUG_UNDUMP
			ULDebug.Log( "LoadCode n:" + n );
#endif
			proto.Code.Clear();
			for( int i=0; i<n; ++i )
			{
				proto.Code.Add( LoadInstruction() );
#if DEBUG_UNDUMP
				ULDebug.Log( "Count:" + proto.Code.Count );
				ULDebug.Log( "LoadInstruction:" + proto.Code[proto.Code.Count-1] );
#endif
			}
		}

		private void LoadConstants( LuaProto proto )
		{
			var n = LoadInt();
#if DEBUG_UNDUMP
			ULDebug.Log( "Load Constants:" + n );
#endif
			proto.K.Clear();
			for( int i=0; i<n; ++i )
			{
				int t = (int)LoadByte();
#if DEBUG_UNDUMP
				ULDebug.Log( "Constant Type:" + t );
#endif
				var v = new StkId();
				switch( t )
				{
					case (int)LuaType.LUA_TNIL:
						v.V.SetNilValue();
						proto.K.Add( v );
						break;

					case (int)LuaType.LUA_TBOOLEAN:
						v.V.SetBValue(LoadBoolean());
						proto.K.Add( v );
						break;

					case (int)LuaType.LUA_TNUMBER:
						v.V.SetNValue(LoadNumber());
						proto.K.Add( v );
						break;

					case (int)LuaType.LUA_TSTRING:
#if DEBUG_UNDUMP
						ULDebug.Log( "LuaType.LUA_TSTRING" );
#endif
						v.V.SetSValue(LoadString());
						proto.K.Add( v );
						break;

					default:
						throw new UndumpException(
							"LoadConstants unknown type: " + t );
				}
			}

			n = LoadInt();
#if DEBUG_UNDUMP
			ULDebug.Log( "Load Functions:" + n );
#endif
			proto.P.Clear();
			for( int i=0; i<n; ++i )
			{
				proto.P.Add( LoadFunction() );
			}
		}

		private void LoadUpvalues( LuaProto proto )
		{
			var n = LoadInt();
#if DEBUG_UNDUMP
			ULDebug.Log( "Load Upvalues:" + n );
#endif
			proto.Upvalues.Clear();
			for( int i=0; i<n; ++i )
			{
				proto.Upvalues.Add(
					new UpvalDesc()
					{
						Name = null,
						InStack = LoadBoolean(),
						Index = (int)LoadByte()
					} );
			}
		}

		private void LoadDebug( LuaProto proto )
		{
			int n;
			proto.Source = LoadString();

			// LineInfo
			n = LoadInt();
#if DEBUG_UNDUMP
			ULDebug.Log( "Load LineInfo:" + n );
#endif
			proto.LineInfo.Clear();
			for( int i=0; i<n; ++i )
			{
				proto.LineInfo.Add( LoadInt() );
			}

			// LocalVar
			n = LoadInt();
#if DEBUG_UNDUMP
			ULDebug.Log( "Load LocalVar:" + n );
#endif
			proto.LocVars.Clear();
			for( int i=0; i<n; ++i )
			{
				proto.LocVars.Add(
					new LocVar()
					{
						VarName = LoadString(),
						StartPc = LoadInt(),
						EndPc   = LoadInt(),
					} );
			}

			// Upvalues' name
			n = LoadInt();
			for( int i=0; i<n; ++i )
			{
				proto.Upvalues[i].Name = LoadString();
			}
		}
	}

}



// #define DEBUG_BINARY_READER
// #define DEBUG_UNDUMP

using ULDebug = UniLua.Tools.ULDebug;

namespace UniLua
{
  public class Undump
	{
		private BinaryBytesReader Reader;


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

		private Undump( BinaryBytesReader reader )
		{
			Reader 	= reader;
		}

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
			byte[] header = LoadBytes( 4 // Signature
				+ 8 // version, format version, size of int ... etc
				+ 6 // Tail
			);
			byte v = header[ 4 /* skip signature */
						   + 4 /* offset of sizeof(size_t) */
						   ];
#if DEBUG_UNDUMP
			ULDebug.Log(string.Format("sizeof(size_t): {0}", v));
#endif
			Reader.SizeOfSizeT = v ;
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


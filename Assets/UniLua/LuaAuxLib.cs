
namespace UniLua
{
	using System;
	using System.IO;
	using System.Collections.Generic;

	public struct NameFuncPair
	{
		public string Name;
		public CSharpFunctionDelegate Func;

		public NameFuncPair( string name, CSharpFunctionDelegate func )
		{
			Name = name;
			Func = func;
		}
	}

	public interface ILuaAuxLib
	{
		void 	L_Where( int level );
		int 	L_Error( string fmt, params object[] args );
		void	L_CheckStack( int size, string msg );
		void 	L_CheckAny( int narg );
		void 	L_CheckType( int index, LuaType t );
		double	L_CheckNumber( int narg );
		UInt64	L_CheckUInt64( int narg );
		int 	L_CheckInteger( int narg );
		string 	L_CheckString( int narg );
		uint	L_CheckUnsigned( int narg );
		void 	L_ArgCheck( bool cond, int narg, string extraMsg );
		int 	L_ArgError( int narg, string extraMsg );
		string 	L_TypeName( int index );

		string 	L_ToString( int index );
		bool 	L_GetMetaField( int index, string method );
		int 	L_GetSubTable( int index, string fname );

		void 	L_RequireF( string moduleName, CSharpFunctionDelegate openFunc, bool global );
		void 	L_OpenLibs();
		void 	L_NewLibTable( NameFuncPair[] define );
		void	L_NewLib( NameFuncPair[] define );
		void 	L_SetFuncs( NameFuncPair[] define, int nup );
		
		T 		L_Opt<T>( Func<int,T> f, int n, T def );
		int		L_OptInt( int narg, int def );
		string 	L_OptString( int narg, string def );
		bool 	L_CallMeta( int obj, string name );
		void	L_Traceback( ILuaState otherLua, string msg, int level );
		int		L_Len( int index );

		ThreadStatus L_LoadBuffer( string s, string name );
		ThreadStatus L_LoadBufferX( string s, string name, string mode );
		ThreadStatus L_LoadFile( string filename );
		ThreadStatus L_LoadFileX( string filename, string mode );

		ThreadStatus L_LoadString( string s );
		ThreadStatus L_LoadBytes( byte[] bytes, string name );
		ThreadStatus L_DoString( string s );
		ThreadStatus L_DoFile( string filename );
		

		string	L_Gsub( string src, string pattern, string rep );

		// reference system
		int		L_Ref( int t );
		void	L_Unref( int t, int reference );
	}

	class StringLoadInfo : ILoadInfo
	{
		public StringLoadInfo(string s )
		{
			Str = s;
			Pos = 0;
		}

		public int ReadByte()
		{
			if( Pos >= Str.Length )
				return -1;
			else
				return Str[Pos++];
		}

		public int PeekByte()
		{
			if( Pos >= Str.Length )
				return -1;
			else
				return Str[Pos];
		}

		private string 	Str;
		private int		Pos;
	}

	class BytesLoadInfo : ILoadInfo
	{
		public BytesLoadInfo( byte[] bytes )
		{
			Bytes 	= bytes;
			Pos 	= 0;
		}

		public int ReadByte()
		{
			if( Pos >= Bytes.Length )
				return -1;
			else
				return Bytes[Pos++];
		}

		public int PeekByte()
		{
			if( Pos >= Bytes.Length )
				return -1;
			else
				return Bytes[Pos];
		}

		private byte[] 	Bytes;
		private int 	Pos;
	}

	public partial class LuaState
	{
		private const int LEVELS1 = 12; // size of the first part of the stack
		private const int LEVELS2 = 10; // size of the second part of the stack

		public void L_Where( int level )
		{
			LuaDebug ar = new LuaDebug();
			if( API.GetStack( level, ar ) ) // check function at level
			{
				GetInfo( "Sl", ar ); // get info about it
				if( ar.CurrentLine > 0 ) // is there info?
				{
					API.PushString( string.Format( "{0}:{1}: ", ar.ShortSrc, ar.CurrentLine ) );
					return;
				}
			}
			API.PushString( "" ); // else, no information available...
		}

		public int L_Error( string fmt, params object[] args )
		{
			L_Where( 1 );
			API.PushString( string.Format( fmt, args ) );
			API.Concat( 2 );
			return API.Error();
		}

		public void L_CheckStack( int size, string msg )
		{
			// keep some extra space to run error routines, if needed
			if(!API.CheckStack(size + LuaDef.LUA_MINSTACK)) {
				if(msg != null)
					{ L_Error(string.Format("stack overflow ({0})", msg)); }
				else
					{ L_Error("stack overflow"); }
			}
		}

		public void L_CheckAny( int narg )
		{
			if( API.Type( narg ) == LuaType.LUA_TNONE )
				L_ArgError( narg, "value expected" );
		}

		public double L_CheckNumber( int narg )
		{
			bool isnum;
			double d = API.ToNumberX( narg, out isnum );
			if( !isnum )
				TagError( narg, LuaType.LUA_TNUMBER );
			return d;
		}

		public UInt64 L_CheckUInt64( int narg )
		{
			bool isnum;
			UInt64 v = API.ToUInt64X( narg, out isnum );
			if( !isnum )
				TagError( narg, LuaType.LUA_TUINT64 );
			return v;
		}

		public int L_CheckInteger( int narg )
		{
			bool isnum;
			int d = API.ToIntegerX( narg, out isnum );
			if( !isnum )
				TagError( narg, LuaType.LUA_TNUMBER );
			return d;
		}

		public string L_CheckString( int narg )
		{
			string s = API.ToString( narg );
			if( s == null ) TagError( narg, LuaType.LUA_TSTRING );
			return s;
		}

		public uint L_CheckUnsigned( int narg )
		{
			bool isnum;
			uint d = API.ToUnsignedX( narg, out isnum );
			if( !isnum )
				TagError( narg, LuaType.LUA_TNUMBER );
			return d;
		}

		public T L_Opt<T>( Func<int,T> f, int n, T def )
		{
			LuaType t = API.Type( n );
			if( t == LuaType.LUA_TNONE ||
				t == LuaType.LUA_TNIL )
			{
				return def;
			}
			else
			{
				return f( n );
			}
		}

		public int L_OptInt( int narg, int def )
		{
			LuaType t = API.Type( narg );
			if( t == LuaType.LUA_TNONE ||
				t == LuaType.LUA_TNIL )
			{
				return def;
			}
			else
			{
				return L_CheckInteger( narg );
			}
		}

		public string L_OptString( int narg, string def )
		{
			LuaType t = API.Type( narg );
			if( t == LuaType.LUA_TNONE ||
				t == LuaType.LUA_TNIL )
			{
				return def;
			}
			else
			{
				return L_CheckString( narg );
			}
		}

		private int TypeError( int index, string typeName )
		{
			string msg = string.Format( "{0} expected, got {1}",
				typeName, L_TypeName( index ) );
			API.PushString( msg );
			return L_ArgError( index, msg );
		}

		private void TagError( int index, LuaType t )
		{
			TypeError( index, API.TypeName( t ) );
		}

		public void L_CheckType( int index, LuaType t )
		{
			if( API.Type( index ) != t )
				TagError( index, t );
		}

		public void L_ArgCheck( bool cond, int narg, string extraMsg )
		{
			if( !cond )
				L_ArgError( narg, extraMsg );
		}

		public int L_ArgError( int narg, string extraMsg )
		{

			LuaDebug ar = new LuaDebug();
			if( !API.GetStack( 0, ar ) ) // no stack frame ?
				return L_Error( "bad argument {0} ({1})", narg, extraMsg );

			GetInfo( "n", ar );
			if( ar.NameWhat == "method" )
			{
				narg--; // do not count 'self'
				if( narg == 0 ) // error is in the self argument itself?
					return L_Error( "calling '{0}' on bad self", ar.Name );
			}
			if( ar.Name == null )
				ar.Name = PushGlobalFuncName( ar ) ? API.ToString(-1) : "?";
			return L_Error( "bad argument {0} to '{1}' ({2})",
				narg, ar.Name, extraMsg );
		}

		public string L_TypeName( int index )
		{
			return API.TypeName( API.Type( index ) );
		}

		public bool L_GetMetaField( int obj, string name )
		{
			if( !API.GetMetaTable(obj) ) // no metatable?
				return false;
			API.PushString( name );
			API.RawGet( -2 );
			if( API.IsNil( -1 ) )
			{
				API.Pop( 2 );
				return false;
			}
			else
			{
				API.Remove( -2 );
				return true;
			}
		}

		public bool L_CallMeta( int obj, string name )
		{
			obj = API.AbsIndex( obj );
			if( !L_GetMetaField( obj, name ) ) // no metafield?
				return false;

			API.PushValue( obj );
			API.Call( 1, 1 );
			return true;
		}

		private void PushFuncName( LuaDebug ar )
		{
			if( ar.NameWhat.Length > 0 && ar.NameWhat[0] != '\0' ) // is there a name?
				API.PushString( string.Format( "function '{0}'", ar.Name ) );
			else if( ar.What.Length > 0 && ar.What[0] == 'm' ) // main?
				API.PushString( "main chunk" );
			else if( ar.What.Length > 0 && ar.What[0] == 'C' )
			{
				if( PushGlobalFuncName( ar ) )
				{
					API.PushString( string.Format( "function '{0}'", API.ToString(-1) ) );
					API.Remove( -2 ); //remove name
				}
				else
					API.PushString( "?" );
			}
			else
				API.PushString( string.Format( "function <{0}:{1}>", ar.ShortSrc, ar.LineDefined ) );
		}

		private int CountLevels()
		{
			LuaDebug ar = new LuaDebug();
			int li = 1;
			int le = 1;
			// find an upper bound
			while( API.GetStack(le, ar) )
			{
				li = le;
				le *= 2;
			}
			// do a binary search
			while(li < le)
			{
				int m = (li + le)/2;
				if( API.GetStack( m, ar ) )
					li = m + 1;
				else
					le = m;
			}
			return le - 1;
		}

		public void L_Traceback( ILuaState otherLua, string msg, int level )
		{
			LuaState oLua = otherLua as LuaState;
			LuaDebug ar = new LuaDebug();
			int top = API.GetTop();
			int numLevels = oLua.CountLevels();
			int mark = (numLevels > LEVELS1 + LEVELS2) ? LEVELS1 : 0;
			if( msg != null )
				API.PushString( string.Format( "{0}\n", msg ) );
			API.PushString( "stack traceback:" );
			while( otherLua.GetStack( level++, ar ) )
			{
				if( level == mark ) // too many levels?
				{
					API.PushString( "\n\t..." );
					level = numLevels - LEVELS2; // and skip to last ones
				}
				else
				{
					oLua.GetInfo( "Slnt", ar );
					API.PushString( string.Format( "\n\t{0}:", ar.ShortSrc ) );
					if( ar.CurrentLine > 0 )
						API.PushString( string.Format( "{0}:", ar.CurrentLine ) );
					API.PushString(" in ");
					PushFuncName( ar );
					if( ar.IsTailCall )
						API.PushString( "\n\t(...tail calls...)" );
					API.Concat( API.GetTop() - top );
				}
			}
			API.Concat( API.GetTop() - top );
		}

		public int L_Len( int index )
		{
			API.Len( index );

			bool isnum;
			int l = (int)API.ToIntegerX( -1, out isnum );
			if( !isnum )
				L_Error( "object length is not a number" );
			API.Pop( 1 );
			return l;
		}

		public ThreadStatus L_LoadBuffer( string s, string name )
		{
			return L_LoadBufferX( s, name, null );
		}

		public ThreadStatus L_LoadBufferX( string s, string name, string mode )
		{
			var loadinfo = new StringLoadInfo( s );
			return API.Load( loadinfo, name, mode );
		}

		public ThreadStatus L_LoadBytes( byte[] bytes, string name )
		{
			var loadinfo = new BytesLoadInfo( bytes );
			return API.Load( loadinfo, name, null );
		}

		private ThreadStatus ErrFile( string what, int fnameindex )
		{
			return ThreadStatus.LUA_ERRFILE;
		}

		public ThreadStatus L_LoadFile( string filename )
		{
			return L_LoadFileX( filename, null );
		}

		public ThreadStatus L_LoadFileX( string filename, string mode )
		{
			var status = ThreadStatus.LUA_OK;
			if( filename == null )
			{
				// 暂不实现从 stdin 输入
				throw new System.NotImplementedException();
			}

			int fnameindex = API.GetTop() + 1;
			API.PushString( "@" + filename );
			try
			{
				using( var loadinfo = LuaFile.OpenFile( filename ) )
				{
					loadinfo.SkipComment();
					status = API.Load( loadinfo, API.ToString(-1), mode );
				}
			}
			catch( LuaRuntimeException e )
			{
				API.PushString( string.Format( "cannot open {0}: {1}",
					filename, e.Message ) );
				return ThreadStatus.LUA_ERRFILE;
			}

			API.Remove( fnameindex );
			return status;
		}

		public ThreadStatus L_LoadString( string s )
		{
			return L_LoadBuffer( s, s );
		}

		public ThreadStatus L_DoString( string s )
		{
			var status = L_LoadString( s );
			if( status != ThreadStatus.LUA_OK )
				return status;
			return API.PCall( 0, LuaDef.LUA_MULTRET, 0 );
		}

		public ThreadStatus L_DoFile( string filename )
		{
			var status = L_LoadFile( filename );
			if( status != ThreadStatus.LUA_OK )
				return status;
			return API.PCall( 0, LuaDef.LUA_MULTRET, 0 );
		}

		public string L_Gsub( string src, string pattern, string rep )
		{
			string res = src.Replace(pattern, rep);
			API.PushString( res );
			return res;
		}
		
		public string L_ToString( int index )
		{
			if( !L_CallMeta( index, "__tostring" ) ) // no metafield? // TODO L_CallMeta
			{
				switch( API.Type(index) )
				{
					case LuaType.LUA_TNUMBER:
					case LuaType.LUA_TSTRING:
						API.PushValue( index );
						break;

					case LuaType.LUA_TBOOLEAN:
						API.PushString( API.ToBoolean( index ) ? "true" : "false" );
						break;

					case LuaType.LUA_TNIL:
						API.PushString( "nil" );
						break;

					default:
						API.PushString( string.Format("{0}: {1:X}"
							, L_TypeName( index )
							, API.ToObject( index ).GetHashCode()
							) );
						break;
				}
			}
			return API.ToString( -1 );
		}

		// private static class LibLoadInfo
		// {
		// 	public static List<NameFuncPair> Items;

		// 	static LibLoadInfo()
		// 	{
		// 		Items = new List<NameFuncPair>();
		// 		Add( "_G", LuaState.LuaOpen_Base );
		// 	}

		// 	private static void Add( string name, CSharpFunctionDelegate loadFunc )
		// 	{
		// 		Items.Add( new NameFuncPair { Name=name, LoadFunc=loadFunc } );
		// 	}
		// }

		public void L_OpenLibs()
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "_G", 				LuaBaseLib.OpenLib  ),
				new NameFuncPair( LuaPkgLib.LIB_NAME,	LuaPkgLib.OpenLib 	),
				new NameFuncPair( LuaCoroLib.LIB_NAME,	LuaCoroLib.OpenLib  ),
				new NameFuncPair( LuaTableLib.LIB_NAME,	LuaTableLib.OpenLib ),
				new NameFuncPair( LuaIOLib.LIB_NAME,	LuaIOLib.OpenLib	),
				new NameFuncPair( LuaOSLib.LIB_NAME,	LuaOSLib.OpenLib	),
				// {LUA_OSLIBNAME, luaopen_os},
				new NameFuncPair( LuaStrLib.LIB_NAME, 	LuaStrLib.OpenLib   ),
				new NameFuncPair( LuaBitLib.LIB_NAME, 	LuaBitLib.OpenLib   ),
				new NameFuncPair( LuaMathLib.LIB_NAME, 	LuaMathLib.OpenLib  ),
				new NameFuncPair( LuaDebugLib.LIB_NAME, LuaDebugLib.OpenLib ),
				new NameFuncPair( LuaFFILib.LIB_NAME,	LuaFFILib.OpenLib	),
				new NameFuncPair( LuaEncLib.LIB_NAME,	LuaEncLib.OpenLib	),
			};

			for( var i=0; i<define.Length; ++i)
			{
				L_RequireF( define[i].Name, define[i].Func, true );
				API.Pop( 1 );
			}

			// LuaBaseLib.LuaOpen_Base( this );
		}

		public void L_RequireF( string moduleName, CSharpFunctionDelegate openFunc, bool global )
		{
			API.PushCSharpFunction( openFunc );
			API.PushString( moduleName );
			API.Call( 1, 1 );
			L_GetSubTable( LuaDef.LUA_REGISTRYINDEX, "_LOADED" );
			API.PushValue( -2 );
			API.SetField( -2, moduleName );
			API.Pop( 1 );
			if( global )
			{
				API.PushValue( -1 );
				API.SetGlobal( moduleName );
			}
		}

		public int L_GetSubTable( int index, string fname )
		{
			API.GetField( index, fname );
			if( API.IsTable( -1 ) )
				return 1;
			else
			{
				API.Pop( 1 );
				index = API.AbsIndex( index );
				API.NewTable();
				API.PushValue( -1 );
				API.SetField( index, fname );
				return 0;
			}
		}

		public void L_NewLibTable( NameFuncPair[] define )
		{
			API.CreateTable( 0, define.Length );
		}

		public void L_NewLib( NameFuncPair[] define )
		{
			L_NewLibTable( define );
			L_SetFuncs( define, 0 );
		}

		public void L_SetFuncs( NameFuncPair[] define, int nup )
		{
			// TODO: Check Version
			L_CheckStack(nup, "too many upvalues");
			for( var j=0; j<define.Length; ++j )
			{
				for( int i=0; i<nup; ++i )
					API.PushValue( -nup );
				API.PushCSharpClosure( define[j].Func, nup );
				API.SetField( -(nup + 2), define[j].Name );
			}
			API.Pop( nup );
		}

		private bool FindField( int objIndex, int level )
		{
			if( level == 0 || !API.IsTable(-1) )
				return false; // not found

			API.PushNil(); // start 'next' loop
			while( API.Next(-2) ) // for each pair in table
			{
				if( API.Type(-2) == LuaType.LUA_TSTRING ) // ignore non-string keys
				{
					if( API.RawEqual( objIndex, -1 ) ) // found object?
					{
						API.Pop(1); // remove value (but keep name)
						return true;
					}
					else if( FindField( objIndex, level-1 ) ) // try recursively
					{
						API.Remove( -2 ); // remove table (but keep name)
						API.PushString( "." );
						API.Insert( -2 ); // place '.' between the two names
						API.Concat( 3 );
						return true;
					}
				}
				API.Pop(1); // remove value
			}
			return false; // not found
		}

		private bool PushGlobalFuncName( LuaDebug ar )
		{
			int top = API.GetTop();
			GetInfo( "f", ar );
			API.PushGlobalTable();
			if( FindField( top+1, 2 ) )
			{
				API.Copy( -1, top+1 );
				API.Pop( 2 );
				return true;
			}
			else
			{
				API.SetTop( top ); // remove function and global table
				return false;
			}
		}

		private const int FreeList = 0;

		public int L_Ref( int t )
		{
			if( API.IsNil(-1) )
			{
				API.Pop(1); // remove from stack
				return LuaConstants.LUA_REFNIL; // `nil' has a unique fixed reference
			}

			t = API.AbsIndex(t);
			API.RawGetI(t, FreeList); // get first free element
			int reference = API.ToInteger(-1); // ref = t[freelist]
			API.Pop(1); // remove it from stack
			if( reference != 0 ) // any free element?
			{
				API.RawGetI(t, reference); // remove it from list
				API.RawSetI(t, FreeList); // t[freelist] = t[ref]
			}
			else // no free elements
				reference = API.RawLen(t) + 1; // get a new reference
			API.RawSetI(t, reference);
			return reference;
		}

		public void L_Unref( int t, int reference )
		{
			if( reference >= 0 )
			{
				t = API.AbsIndex(t);
				API.RawGetI(t, FreeList);
				API.RawSetI(t, reference); // t[ref] = t[freelist]
				API.PushInteger(reference);
				API.RawSetI(t, FreeList); // t[freelist] = ref
			}
		}

#if UNITY_IPHONE
        public void FEED_AOT_FOR_IOS(LuaState lua)
        {
            lua.L_Opt( lua.L_CheckInteger, 1, 1);
        }
#endif

	}

}


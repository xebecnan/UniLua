
// TODO

#define LUA_COMPAT_LOADERS

namespace UniLua
{
	using Environment = System.Environment;
	using StringBuilder = System.Text.StringBuilder;
	using String = System.String;
	using File = System.IO.File;
	using FileMode = System.IO.FileMode;
	using Exception = System.Exception;

	internal class LuaPkgLib
	{
		public const string LIB_NAME = "package";

		private const string CLIBS = "_CLIBS";

		private const string LUA_PATH = "LUA_PATH";
		private const string LUA_CPATH = "LUA_CPATH";
		private const string LUA_PATHSUFFIX = "_" + LuaDef.LUA_VERSION_MAJOR +
											  "_" + LuaDef.LUA_VERSION_MINOR;
		private const string LUA_PATHVERSION = LUA_PATH + LUA_PATHSUFFIX;
		private const string LUA_CPATHVERSION = LUA_CPATH + LUA_PATHSUFFIX;

		// private const string LUA_LDIR = "!\\lua\\";
		// private const string LUA_CDIR = "!\\";
		// private const string LUA_PATH_DEFAULT = LUA_LDIR + "?.lua;" + 
		// 										LUA_LDIR + "?\\init.lua;" +
		// 										LUA_CDIR + "?.lua;" +
		// 										LUA_CDIR + "?\\init.lua;" +
		// 										".\\?.lua";
		// private const string LUA_CPATH_DEFAULT = LUA_CDIR + "?.dll;" +
		// 										 LUA_CDIR + "loadall.dll;" +
		// 										 ".\\?.dll";

		private const string LUA_PATH_DEFAULT = "?.lua;";
		private const string LUA_CPATH_DEFAULT = "?.dll;loadall.dll;";

		private const string LUA_PATH_SEP	= ";";
		private const string LUA_PATH_MARK	= "?";
		private const string LUA_EXEC_DIR	= "!";
		private const string LUA_IGMARK		= "-";

		private static readonly string LUA_LSUBSEP = LuaConf.LUA_DIRSEP;

		// auxiliary mark (for internal use)
		private const string AUXMARK		= "\u0001";

		public static int OpenLib( ILuaState lua )
		{
			// NameFuncPair[] define = new NameFuncPair[]
			// {
			// 	new NameFuncPair( "module", PKG_Module ),
			// };

			// lua.L_NewLib( define );
			
			// // create table CLIBS to keep track of loaded C libraries
			// lua.L_GetSubTable( LuaDef.LUA_REGISTRYINDEX, CLIBS );
			// lua.CreateTable( 0, 1 ); // metatable for CLIBS
			// lua.PushCSharpFunction
			
			// create `package' table
			NameFuncPair[] pkg_define = new NameFuncPair[]
			{
				new NameFuncPair( "loadlib", 	PKG_LoadLib ),
				new NameFuncPair( "searchpath", PKG_SearchPath ),
				new NameFuncPair( "seeall", 	PKG_SeeAll ),
			};
			lua.L_NewLib( pkg_define );

			CreateSearchersTable( lua );
#if LUA_COMPAT_LOADERS
			lua.PushValue( -1 ); // make a copy of `searchers' table
			lua.SetField( -3, "loaders" ); // put it in field `loaders'
#endif
			lua.SetField( -2, "searchers" ); // put it in field `searchers'

			// set field `path'
			SetPath( lua, "path", LUA_PATHVERSION, LUA_PATH, LUA_PATH_DEFAULT );
			// set field `cpath'
			SetPath( lua, "cpath", LUA_CPATHVERSION, LUA_CPATH, LUA_CPATH_DEFAULT );

			// store config information
			lua.PushString( string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n",
				LuaConf.LUA_DIRSEP,
				LUA_PATH_SEP,
				LUA_PATH_MARK,
				LUA_EXEC_DIR,
				LUA_IGMARK ) );
			lua.SetField( -2, "config" );

			// set field `loaded'
			lua.L_GetSubTable( LuaDef.LUA_REGISTRYINDEX, "_LOADED" );
			lua.SetField( -2, "loaded" );

			// set field `preload'
			lua.L_GetSubTable( LuaDef.LUA_REGISTRYINDEX, "_PRELOAD" );
			lua.SetField( -2, "preload" );

			lua.PushGlobalTable();
			lua.PushValue( -2 ); // set `package' as upvalue for next lib

			NameFuncPair[] loadLibFuncs = new NameFuncPair[]
			{
				new NameFuncPair( "module", 	LL_Module ),
				new NameFuncPair( "require", 	LL_Require ),
			};
			lua.L_SetFuncs( loadLibFuncs, 1 ); // open lib into global table
			lua.Pop( 1 ); // pop global table

			return 1; // return `package' table
		}

		private static void CreateSearchersTable( ILuaState lua )
		{
			CSharpFunctionDelegate[] searchers = new CSharpFunctionDelegate[]
			{
				SearcherPreload,
				SearcherLua,
			};
			lua.CreateTable( searchers.Length, 0 );
			for( int i=0; i<searchers.Length; ++i )
			{
				lua.PushValue( -2 ); // set `package' as upvalue for all searchers
				lua.PushCSharpClosure( searchers[i], 1 );
				lua.RawSetI( -2, i+1 );
			}
		}

		private static void SetPath
			( ILuaState lua
			, string 	fieldName
			, string 	envName1
			, string	envName2
			, string 	def
			)
		{
            lua.PushString(def);
            /*
			string path = Environment.GetEnvironmentVariable( envName1 );
			if( path == null ) // no environment variable?
				path = Environment.GetEnvironmentVariable( envName2 ); // try alternative name
			if( path == null || noEnv( lua ) ) // no environment variable?
				lua.PushString( def );
			else
			{
				// replace ";;" by ";AUXMARK;" and then AUXMARK by default path
				lua.L_Gsub( path, LUA_PATH_SEP + LUA_PATH_SEP,
					LUA_PATH_SEP + AUXMARK + LUA_PATH_SEP );
				lua.L_Gsub( path, AUXMARK, def );
				lua.Remove( -2 );
			}*/
			SetProgDir( lua );
			lua.SetField( -2, fieldName );
		}

		private static bool noEnv( ILuaState lua )
		{
			lua.GetField( LuaDef.LUA_REGISTRYINDEX, "LUA_NOENV" );
			bool res = lua.ToBoolean( -1 );
			lua.Pop( 1 );
			return res;
		}

		private static void SetProgDir( ILuaState lua )
		{
			// TODO: unity 读本地文件?
			//
			// 下面的代码在编辑器中可以运行, 但是 build 后运行时会 crash
			// var pgs = System.Diagnostics.Process.GetCurrentProcess();
			// ULDebug.Log( pgs.MainModule.FileName );
		}

		private static int SearcherPreload( ILuaState lua )
		{
			string name = lua.L_CheckString( 1 );
			lua.GetField( LuaDef.LUA_REGISTRYINDEX, "_PRELOAD" );
			lua.GetField( -1, name );
			if( lua.IsNil(-1) ) // not found?
				lua.PushString( string.Format(
					"\n\tno field package.preload['{0}']", name ) );
			return 1;
		}

		private static bool Readable( string filename )
		{
			return LuaFile.Readable( filename );
		}

		private static bool PushNextTemplate( ILuaState lua,
			string path, ref int pos )
		{
			while( pos < path.Length && path[pos] == LUA_PATH_SEP[0])
				pos++; // skip separators
			if( pos >= path.Length )
				return false;
			int end = pos+1;
			while( end < path.Length && path[end] != LUA_PATH_SEP[0])
				end++;

			var template = path.Substring( pos, end-pos);
			lua.PushString( template );

			pos = end;
			return true;
		}

		private static string SearchPath( ILuaState lua,
			string name, string path, string sep, string dirsep )
		{
			var sb = new StringBuilder(); // to build error message
			if( !String.IsNullOrEmpty(sep) ) // non-empty separator?
				name = name.Replace( sep, dirsep ); // replace it by `dirsep'
			int pos = 0;
			while(PushNextTemplate(lua, path, ref pos))
			{
				var template = lua.ToString(-1);
				string filename = template.Replace( LUA_PATH_MARK, name );
				lua.Remove( -1 ); // remove path template
				if( Readable( filename ) ) // does file exist and is readable?
					return filename; // return that file name
				lua.PushString( string.Format( "\n\tno file '{0}'", filename) );
				lua.Remove( -2 ); // remove file name
				sb.Append( lua.ToString(-1) ); // concatenate error msg. entry
			}
			lua.PushString( sb.ToString() ); // create error message
			return null; // not found
		}

		private static string FindFile( ILuaState lua,
			string name, string pname, string dirsep )
		{
			lua.GetField( lua.UpvalueIndex(1), pname );
			string path = lua.ToString( -1 );
			if( path == null )
				lua.L_Error( "'package.{0}' must be a string", pname );
			return SearchPath( lua, name, path, ".", dirsep );
		}

		private static int CheckLoad( ILuaState lua,
			bool stat, string filename )
		{
			if( stat ) // module loaded successfully?
			{
				lua.PushString( filename ); // will be 2nd arg to module
				return 2; // return open function and file name
			}
			else return lua.L_Error(
				"error loading module '{0}' from file '{1}':\n\t{2}",
				lua.ToString(1), filename, lua.ToString(-1) );
		}

		private static int SearcherLua( ILuaState lua )
		{
			string name = lua.L_CheckString( 1 );
			string filename = FindFile( lua, name, "path", LUA_LSUBSEP );
			if( filename == null )
				return 1;
			return CheckLoad( lua,
				(lua.L_LoadFile(filename) == ThreadStatus.LUA_OK),
				filename );
		}

		private static int LL_Module( ILuaState lua )
		{
			// TODO
			return 0;
		}

		private static void FindLoader( ILuaState lua, string name )
		{
			// will be at index 3
			lua.GetField( lua.UpvalueIndex(1), "searchers" );
			if( ! lua.IsTable(3) )
				lua.L_Error("'package.searchers' must be a table");

			var sb = new StringBuilder();
			// iterator over available searchers to find a loader
			for( int i=1; ; ++i )
			{
				lua.RawGetI( 3, i ); // get a searcher
				if( lua.IsNil( -1 ) ) // no more searchers?
				{
					lua.Pop( 1 ); // remove nil
					lua.PushString( sb.ToString() );
					lua.L_Error( "module '{0}' not found:{1}",
						name, lua.ToString(-1));
					return;
				}

				lua.PushString( name );
				lua.Call( 1, 2 ); // call it
				if( lua.IsFunction(-2) ) // did it find a loader
					return; // module loader found
				else if( lua.IsString(-2) ) // searcher returned error message?
				{
					lua.Pop( 1 ); // return extra return
					sb.Append( lua.ToString(-1) );
				}
				else
					lua.Pop( 2 ); // remove both returns
			}
		}

		private static int LL_Require( ILuaState lua )
		{
			string name = lua.L_CheckString( 1 );
			lua.SetTop( 1 );
			// _LOADED table will be at index 2
			lua.GetField( LuaDef.LUA_REGISTRYINDEX, "_LOADED" );
			// _LOADED[name]
			lua.GetField( 2, name );
			// is it there?
			if( lua.ToBoolean( -1 ) )
				return 1; // package is already loaded
			// else must load package
			// remove `GetField' result
			lua.Pop( 1 );
			FindLoader( lua, name );
			lua.PushString( name ); // pass name as arg to module loader
			lua.Insert( -2 ); // name is 1st arg (before search data)
			lua.Call( 2, 1 ); // run loader to load module
			if( !lua.IsNil( -1 ) ) // non-nil return?
				lua.SetField( 2, name ); // _LOADED[name] = returned value
			lua.GetField( 2, name );
			if( lua.IsNil( -1 ) ) // module did not set a value?
			{
				lua.PushBoolean( true ); // use true as result
				lua.PushValue( -1 ); // extra copy to be returned
				lua.SetField( 2, name ); // _LOADED[name] = true
			}
			return 1;
		}

		private static int PKG_LoadLib( ILuaState lua )
		{
			// TODO
			return 0;
		}

		private static int PKG_SearchPath( ILuaState lua )
		{
			// TODO
			return 0;
		}

		private static int PKG_SeeAll( ILuaState lua )
		{
			// TODO
			return 0;
		}
	}

}



namespace UniLua
{
	using System.Collections.Generic;
	using ULDebug = UniLua.Tools.ULDebug;
	using StringBuilder = System.Text.StringBuilder;
	using Char = System.Char;
	using Int32 = System.Int32;

	internal static class LuaBaseLib
	{
		internal static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "assert", 		LuaBaseLib.B_Assert ),
				new NameFuncPair( "collectgarbage", LuaBaseLib.B_CollectGarbage ),
				new NameFuncPair( "dofile", 		LuaBaseLib.B_DoFile ),
				new NameFuncPair( "error", 			LuaBaseLib.B_Error ),
				new NameFuncPair( "ipairs", 		LuaBaseLib.B_Ipairs ),
				new NameFuncPair( "loadfile", 		LuaBaseLib.B_LoadFile ),
				new NameFuncPair( "load", 			LuaBaseLib.B_Load ),
				new NameFuncPair( "loadstring", 	LuaBaseLib.B_Load ),
				new NameFuncPair( "next", 			LuaBaseLib.B_Next ),
				new NameFuncPair( "pairs", 			LuaBaseLib.B_Pairs ),
				new NameFuncPair( "pcall", 			LuaBaseLib.B_PCall ),
				new NameFuncPair( "print", 			LuaBaseLib.B_Print ),
				new NameFuncPair( "rawequal", 		LuaBaseLib.B_RawEqual ),
				new NameFuncPair( "rawlen", 		LuaBaseLib.B_RawLen ),
				new NameFuncPair( "rawget", 		LuaBaseLib.B_RawGet ),
				new NameFuncPair( "rawset", 		LuaBaseLib.B_RawSet ),
				new NameFuncPair( "select", 		LuaBaseLib.B_Select ),
				new NameFuncPair( "getmetatable", 	LuaBaseLib.B_GetMetaTable ),
				new NameFuncPair( "setmetatable", 	LuaBaseLib.B_SetMetaTable ),
				new NameFuncPair( "tonumber", 		LuaBaseLib.B_ToNumber ),
				new NameFuncPair( "tostring", 		LuaBaseLib.B_ToString ),
				new NameFuncPair( "type", 			LuaBaseLib.B_Type ),
				new NameFuncPair( "xpcall", 		LuaBaseLib.B_XPCall ),
			};

			// set global _G
			lua.PushGlobalTable();
			lua.PushGlobalTable();
			lua.SetField( -2, "_G" );

			// open lib into global lib
			lua.L_SetFuncs( define, 0 );
			// lua.RegisterGlobalFunc( "type", 	LuaBaseLib.B_Type );
			// lua.RegisterGlobalFunc( "pairs", 	LuaBaseLib.B_Pairs );
			// lua.RegisterGlobalFunc( "ipairs", 	LuaBaseLib.B_Ipairs );
			// lua.RegisterGlobalFunc( "print",	LuaBaseLib.B_Print );
			// lua.RegisterGlobalFunc( "tostring",	LuaBaseLib.B_ToString );

			lua.PushString( LuaDef.LUA_VERSION );
			lua.SetField( -2, "_VERSION" );

			return 1;
		}

		public static int B_Assert( ILuaState lua )
		{
			if( !lua.ToBoolean( 1 ) )
				return lua.L_Error( "{0}", lua.L_OptString( 2, "assertion failed!" ) );
			return lua.GetTop();
		}

		public static int B_CollectGarbage( ILuaState lua )
		{
			// not implement gc
			string opt = lua.L_OptString( 1, "collect" );
			switch( opt )
			{
				case "count":
					lua.PushNumber( 0 );
					lua.PushNumber( 0 );
					return 2;

				case "step":
				case "isrunning":
					lua.PushBoolean( true );
					return 1;

				default:
					lua.PushInteger( 0 );
					return 1;
			}
		}

		private static int DoFileContinuation( ILuaState lua )
		{
			return lua.GetTop() - 1;
		}

		public static int B_DoFile( ILuaState lua )
		{
			string filename = lua.L_OptString( 1, null );
			lua.SetTop( 1 );
			if( lua.L_LoadFile( filename ) != ThreadStatus.LUA_OK )
				lua.Error();
			lua.CallK( 0, LuaDef.LUA_MULTRET, 0, DoFileContinuation );
			return DoFileContinuation( lua );
		}

		public static int B_Error( ILuaState lua )
		{
			int level = lua.L_OptInt( 2, 1 );
			lua.SetTop( 1 );
			if( lua.IsString( 1 ) && level > 0 )
			{
				lua.L_Where( level );
				lua.PushValue( 1 );
				lua.Concat( 2 );
			}
			return lua.Error();
		}

		private static int LoadAux( ILuaState lua, ThreadStatus status, int envidx )
		{
			if( status == ThreadStatus.LUA_OK )
			{
				if( envidx != 0 ) // `env' parameter?
				{
					lua.PushValue(envidx); // push `env' on stack
					if( lua.SetUpvalue(-2, 1) == null ) // set `env' as 1st upvalue of loaded function
					{
						lua.Pop(1); // remove `env' if not used by previous call
					}
				}
				return 1;
			}
			else // error (message is on top of the stack)
			{
				lua.PushNil();
				lua.Insert(-2); // put before error message
				return 2; // return nil plus error message
			}
		}

		public static int B_LoadFile( ILuaState lua )
		{
			string fname = lua.L_OptString( 1, null );
			string mode  = lua.L_OptString( 2, null );
			int env = (!lua.IsNone(3) ? 3 : 0); // `env' index or 0 if no `env'
			var status = lua.L_LoadFileX( fname, mode );
			return LoadAux(lua, status, env);
		}

		public static int B_Load( ILuaState lua )
		{
			ThreadStatus status;
			string s = lua.ToString(1);
			string mode = lua.L_OptString(3, "bt");
			int env = (! lua.IsNone(4) ? 4 : 0); // `env' index or 0 if no `env'
			if( s != null )
			{
				string chunkName = lua.L_OptString(2, s);
				status = lua.L_LoadBufferX( s, chunkName, mode );
			}
			else // loading from a reader function
			{
				throw new System.NotImplementedException(); // TODO
			}
			return LoadAux( lua, status, env );
		}

		private static int FinishPCall( ILuaState lua, bool status )
		{
			// no space for extra boolean?
			if(!lua.CheckStack(1)) {
				lua.SetTop(0); // create space for return values
				lua.PushBoolean(false);
				lua.PushString("stack overflow");
				return 2;
			}
			lua.PushBoolean( status );
			lua.Replace( 1 );
			return lua.GetTop();
		}

		private static int PCallContinuation( ILuaState lua )
		{
			int context;
			ThreadStatus status = lua.GetContext( out context );
			return FinishPCall( lua, status == ThreadStatus.LUA_YIELD );
		}
		private static CSharpFunctionDelegate DG_PCallContinuation = PCallContinuation;

		public static int B_PCall( ILuaState lua )
		{
			lua.L_CheckAny( 1 );
			lua.PushNil();
			lua.Insert( 1 ); // create space for status result

			ThreadStatus status = lua.PCallK( lua.GetTop() - 2,
				LuaDef.LUA_MULTRET, 0, 0, DG_PCallContinuation );

			return FinishPCall( lua, status == ThreadStatus.LUA_OK );
		}

		public static int B_XPCall( ILuaState lua )
		{
			int n = lua.GetTop();
			lua.L_ArgCheck( n>=2, 2, "value expected" );
			lua.PushValue( 1 ); // exchange function...
			lua.Copy( 2, 1); // ...and error handler
			lua.Replace( 2 );
			ThreadStatus status = lua.PCallK( n-2, LuaDef.LUA_MULTRET,
				1, 0, DG_PCallContinuation );
			return FinishPCall( lua, status == ThreadStatus.LUA_OK );
		}

		public static int B_RawEqual( ILuaState lua )
		{
			lua.L_CheckAny( 1 );
			lua.L_CheckAny( 2 );
			lua.PushBoolean( lua.RawEqual( 1, 2 ) );
			return 1;
		}

		public static int B_RawLen( ILuaState lua )
		{
			LuaType t = lua.Type( 1 );
			lua.L_ArgCheck( t == LuaType.LUA_TTABLE || t == LuaType.LUA_TSTRING,
				1, "table or string expected" );
			lua.PushInteger( lua.RawLen( 1 ) );
			return 1;
		}

		public static int B_RawGet( ILuaState lua )
		{
			lua.L_CheckType( 1, LuaType.LUA_TTABLE );
			lua.L_CheckAny( 2 );
			lua.SetTop( 2 );
			lua.RawGet( 1 );
			return 1;
		}

		public static int B_RawSet( ILuaState lua )
		{
			lua.L_CheckType( 1, LuaType.LUA_TTABLE );
			lua.L_CheckAny( 2 );
			lua.L_CheckAny( 3 );
			lua.SetTop( 3 );
			lua.RawSet( 1 );
			return 1;
		}

		public static int B_Select( ILuaState lua )
		{
			int n = lua.GetTop();
			if( lua.Type( 1 ) == LuaType.LUA_TSTRING &&
				lua.ToString( 1 )[0] == '#' )
			{
				lua.PushInteger( n-1 );
				return 1;
			}
			else
			{
				int i = lua.L_CheckInteger( 1 );
				if( i < 0 ) i = n + i;
				else if( i > n ) i = n;
				lua.L_ArgCheck( 1 <= i, 1, "index out of range" );
				return n - i;
			}
		}

		public static int B_GetMetaTable( ILuaState lua )
		{
			lua.L_CheckAny( 1 );
			if( !lua.GetMetaTable( 1 ) )
			{
				lua.PushNil();
				return 1; // no metatable
			}
			lua.L_GetMetaField( 1, "__metatable" );
			return 1;
		}

		public static int B_SetMetaTable( ILuaState lua )
		{
			LuaType t = lua.Type( 2 );
			lua.L_CheckType( 1, LuaType.LUA_TTABLE );
			lua.L_ArgCheck( t == LuaType.LUA_TNIL || t == LuaType.LUA_TTABLE,
				2, "nil or table expected" );
			if( lua.L_GetMetaField( 1, "__metatable" ) )
				return lua.L_Error( "cannot change a protected metatable" );
			lua.SetTop( 2 );
			lua.SetMetaTable( 1 );
			return 1;
		}

		public static int B_ToNumber( ILuaState lua )
		{
			LuaType t = lua.Type( 2 );
			if( t == LuaType.LUA_TNONE || t == LuaType.LUA_TNIL ) // standard conversion
			{
				bool isnum;
				double n = lua.ToNumberX( 1, out isnum );
				if( isnum )
				{
					lua.PushNumber( n );
					return 1;
				} // else not a number; must be something
				lua.L_CheckAny( 1 );
			}
			else
			{
				string s = lua.L_CheckString( 1 );
				int numBase = lua.L_CheckInteger( 2 );
				bool negative = false;
				lua.L_ArgCheck( (2 <= numBase && numBase <= 36), 2,
					"base out of range" );
				s = s.Trim( ' ', '\f', '\n', '\r', '\t', '\v' );
				s = s + '\0'; // guard
				int pos = 0;
				if(s[pos] == '-') { pos++; negative = true; }
				else if(s[pos] == '+') pos++;
				if( Char.IsLetterOrDigit( s, pos ) )
				{
					double n = 0.0;
					do
					{
						int digit;
						if( Char.IsDigit( s, pos ) )
							digit = Int32.Parse( s[pos].ToString() );
						else
							digit = Char.ToUpper( s[pos] ) - 'A' + 10;
						if( digit >= numBase )
							break; // invalid numeral; force a fail
						n = n * (double)numBase + (double)digit;
						pos++;
					} while( Char.IsLetterOrDigit( s, pos ) );
					if( pos == s.Length - 1 ) // except guard, no invalid trailing characters?
					{
						lua.PushNumber( negative ? -n : n );
						return 1;
					} // else not a number
				} // else not a number
			}
			lua.PushNil(); // not a number
			return 1;
		}

		public static int B_Type( ILuaState lua )
		{
			var t = lua.Type( 1 );
			var tname = lua.TypeName( t );
			lua.PushString( tname );
			return 1;
		}

		private static int PairsMeta( ILuaState lua, string method, bool isZero
			, CSharpFunctionDelegate iter )
		{
			if( !lua.L_GetMetaField( 1, method ) ) // no metamethod?
			{
				lua.L_CheckType( 1, LuaType.LUA_TTABLE );
				lua.PushCSharpFunction( iter );
				lua.PushValue( 1 );
				if( isZero )
					lua.PushInteger( 0 );
				else
					lua.PushNil();
			}
			else
			{
				lua.PushValue( 1 );
				lua.Call( 1, 3 );
			}
			return 3;
		}

		public static int B_Next( ILuaState lua )
		{
			lua.SetTop( 2 );
			if( lua.Next(1) )
			{
				return 2;
			}
			else
			{
				lua.PushNil();
				return 1;
			}
		}
		static CSharpFunctionDelegate DG_B_Next = B_Next;

		public static int B_Pairs( ILuaState lua )
		{
			return PairsMeta( lua, "__pairs", false, DG_B_Next );
		}

		private static int IpairsAux( ILuaState lua )
		{
			int i = lua.ToInteger( 2 );
			i++; // next value
			lua.PushInteger( i );
			lua.RawGetI( 1, i );
			return lua.IsNil( -1 ) ? 1 : 2;
		}
		static CSharpFunctionDelegate DG_IpairsAux = IpairsAux;

		public static int B_Ipairs( ILuaState lua )
		{
			return PairsMeta( lua, "__ipairs", true, DG_IpairsAux );
		}

		public static int B_Print( ILuaState lua )
		{
			StringBuilder sb = new StringBuilder();
			int n = lua.GetTop();
			lua.GetGlobal( "tostring" );
			for( int i=1; i<=n; ++i )
			{
				lua.PushValue( -1 );
				lua.PushValue( i );
				lua.Call( 1, 1 );
				string s = lua.ToString( -1 );
				if( s == null )
					return lua.L_Error("'tostring' must return a string to 'print'");
				if( i > 1 )
					sb.Append( "\t" );
				sb.Append( s );
				lua.Pop( 1 );
			}
			ULDebug.Log( sb.ToString() );
			return 0;
		}

		public static int B_ToString( ILuaState lua )
		{
			lua.L_CheckAny( 1 );
			lua.L_ToString( 1 );
			return 1;
		}

	}

}


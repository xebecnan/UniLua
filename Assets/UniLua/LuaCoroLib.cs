
namespace UniLua
{

	internal class LuaCoroLib
	{
		public const string LIB_NAME = "coroutine";

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "create", 	CO_Create	),
				new NameFuncPair( "resume", 	CO_Resume	),
				new NameFuncPair( "running", 	CO_Running	),
				new NameFuncPair( "status", 	CO_Status	),
				new NameFuncPair( "wrap", 		CO_Wrap		),
				new NameFuncPair( "yield", 		CO_Yield	),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static int CO_Create( ILuaState lua )
		{
			lua.L_CheckType( 1, LuaType.LUA_TFUNCTION );
			ILuaState newLua = lua.NewThread();
			lua.PushValue( 1 ); // move function to top
			lua.XMove( newLua, 1 ); // move function from lua to newLua
			return 1;
		}

		private static int AuxResume( ILuaState lua, ILuaState co, int narg )
		{
			if(!co.CheckStack(narg)) {
				lua.PushString("too many arguments to resume");
				return -1; // error flag
			}
			if( co.Status == ThreadStatus.LUA_OK && co.GetTop() == 0 )
			{
				lua.PushString( "cannot resume dead coroutine" );
				return -1; // error flag
			}
			lua.XMove( co, narg );
			ThreadStatus status = co.Resume( lua, narg );
			if( status == ThreadStatus.LUA_OK || status == ThreadStatus.LUA_YIELD )
			{
				int nres = co.GetTop();
				if(!lua.CheckStack(nres+1)) {
					co.Pop(nres); // remove results anyway;
					lua.PushString("too many results to resume");
					return -1; // error flag
				}
				co.XMove( lua, nres ); // move yielded values
				return nres;
			}
			else
			{
				co.XMove( lua, 1 ); // move error message
				return -1;
			}
		}

		private static int CO_Resume( ILuaState lua )
		{
			ILuaState co = lua.ToThread( 1 );
			lua.L_ArgCheck( co != null, 1, "coroutine expected" );
			int r = AuxResume( lua, co, lua.GetTop() - 1 );
			if( r < 0 )
			{
				lua.PushBoolean( false );
				lua.Insert( -2 );
				return 2; // return false + error message
			}
			else
			{
				lua.PushBoolean( true );
				lua.Insert( -(r+1) );
				return r+1; // return true + `resume' returns
			}
		}

		private static int CO_Running( ILuaState lua )
		{
			bool isMain = lua.PushThread();
			lua.PushBoolean( isMain );
			return 2;
		}

		private static int CO_Status( ILuaState lua )
		{
			ILuaState co = lua.ToThread( 1 );
			lua.L_ArgCheck( co != null, 1, "coroutine expected" );
			if( (LuaState)lua == (LuaState)co )
				lua.PushString( "running" );
			else switch( co.Status )
			{
				case ThreadStatus.LUA_YIELD:
					lua.PushString( "suspended" );
					break;
				case ThreadStatus.LUA_OK:
				{
					LuaDebug ar = new LuaDebug();
					if( co.GetStack( 0, ar ) ) // does it have frames?
						lua.PushString( "normal" );
					else if( co.GetTop() == 0 )
						lua.PushString( "dead" );
					else
						lua.PushString( "suspended" );
					break;
				}
				default: // some error occurred
					lua.PushString( "dead" );
					break;
			}
			return 1;
		}

		private static int CO_AuxWrap( ILuaState lua )
		{
			ILuaState co = lua.ToThread( lua.UpvalueIndex(1) );
			int r = AuxResume( lua, co, lua.GetTop() );
			if( r < 0 )
			{
				if( lua.IsString( -1 ) ) // error object is a string?
				{
					lua.L_Where( 1 ); // add extra info
					lua.Insert( -2 );
					lua.Concat( 2 );
				}
				lua.Error();
			}
			return r;
		}

		private static int CO_Wrap( ILuaState lua )
		{
			CO_Create( lua );
			lua.PushCSharpClosure( CO_AuxWrap, 1 );
			return 1;
		}

		private static int CO_Yield( ILuaState lua )
		{
			return lua.Yield( lua.GetTop() );
		}

	}

}


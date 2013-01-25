
namespace UniLua
{
	internal class LuaDebugLib
	{
		public const string LIB_NAME = "debug";

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "traceback", 	DBG_Traceback	),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static int DBG_Traceback( ILuaState lua )
		{
			return 0;
		}
	}
}


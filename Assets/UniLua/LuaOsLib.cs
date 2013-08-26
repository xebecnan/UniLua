
namespace UniLua
{
	using System.Diagnostics;

	internal class LuaOSLib
	{
		public const string LIB_NAME = "os";

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair("clock", 	OS_Clock),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static int OS_Clock( ILuaState lua )
		{
			lua.PushNumber( Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds );
			return 1;
		}
	}
}


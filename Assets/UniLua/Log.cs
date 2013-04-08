
// #define UNITY
#define UNILUA_STANDALONE

namespace UniLua.Tools
{
	internal class Debug
	{
		public static void Log( string msg )
		{
#if UNITY
			DebugAssist.Log( msg );
#elif UNILUA_STANDALONE
			UnityEngine.Debug.Log( msg );
#endif
		}

		public static void LogError( string msg )
		{
#if UNITY
			DebugAssist.LogError( msg );
#elif UNILUA_STANDALONE
			UnityEngine.Debug.LogError( msg );
#endif
		}
	}
}


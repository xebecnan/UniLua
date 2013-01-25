
//#define UNITY

namespace UniLua.Tools
{
	internal class Debug
	{
		public static void Log( string msg )
		{
#if UNITY
			UnityEngine.Debug.Log( msg );
#endif
		}

		public static void LogError( string msg )
		{
#if UNITY
			UnityEngine.Debug.LogError( msg );
#endif
		}
	}
}


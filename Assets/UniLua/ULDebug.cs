
#define UNITY

namespace UniLua.Tools
{
	public delegate void LogDelegate(string msg);

	// thanks to dharco
	// refer to https://github.com/dharco/UniLua/commit/2854ddf2500ab2f943f01a6d3c9af767c092ce75
	public class ULDebug
	{
		public static LogDelegate Log = NoAction;
		public static LogDelegate LogError = NoAction;

		private static void NoAction(string msg) { }

		static ULDebug()
		{
#if UNITY
			Log = UnityEngine.Debug.Log;
			LogError = UnityEngine.Debug.LogError;
#endif
		}
	}
}


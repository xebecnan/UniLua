
//#define UNITY

using System;

namespace UniLua.Tools
{
    public class Debug
    {
        public static Action<string> Log = NoAction;
        public static Action<string> LogError = NoAction;

        private static void NoAction(string msg) { }

        static Debug()
        {
            #if UNITY
			    LogAction = UnityEngine.Debug.Log;
                LogError = UnityEngine.Debug.LogError;
            #endif
        }
	}
}


using System;
using System.IO;
using UnityEngine;

namespace UniLua
{
  public class LuaFile
	{
		//private static readonly string LUA_ROOT = System.IO.Path.Combine(Application.streamingAssetsPath, "LuaRoot");
		private static PathHook pathhook = (s) => Path.Combine(Path.Combine(Application.streamingAssetsPath, "LuaScripts"), s);
		public static void SetPathHook(PathHook hook) {
			pathhook = hook;
		}

		public static FileLoadInfo OpenFile( string filename )
		{
			//var path = System.IO.Path.Combine(LUA_ROOT, filename);
			var path = pathhook(filename);
			return new FileLoadInfo( File.Open( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) );
		}

		public static bool Readable( string filename )
		{
			//var path = System.IO.Path.Combine(LUA_ROOT, filename);
			var path = pathhook(filename);
			try {
				using( var stream = File.Open( path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) ) {
					return true;
				}
			}
			catch( Exception ) {
				return false;
			}
		}
	}
}


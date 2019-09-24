using System;
using System.IO;
using UnityEngine;

namespace UniLua
{
  public class LuaFile
  {
    public static ILoadInfo OpenFile(string filename)
    {
      string path = _get_path(filename);

      var asset = Resources.Load<TextAsset>(path);
      if (asset == null) throw new System.ArgumentException("Text asset not found: " + path);

      return new StringLoadInfo(asset.text);
    }

    public static bool Readable(string filename)
    {
      try
      {
        string path = _get_path(filename);

        // FIXME Replace to better method
        return Resources.Load<TextAsset>(path) != null;
      }
      catch (Exception)
      {
        return false;
      }
    }

    private static string _get_path(string filename)
    {
      return Path.Combine("lua", Path.ChangeExtension(filename, null)).Replace('\\', '/');
    } // _get_path
  }
}


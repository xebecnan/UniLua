using UniLua;
using UnityEngine;

public static class LuaTableExtensions
{
  public static StkId GetFunction(this LuaTable table, string name)
  {
    var h = name.GetHashCode();
    for (var node = table.GetHashNode(h); node != null; node = node.Next)
    {
      if (node.Val.V.TtIsFunction()) return node.Val;
    }

    return LuaTable.TheNilValue;
  }

  public static StkId GetFunction(this LuaTable table, int key)
  {
    if (0 < key && key - 1 < table.ArrayPart.Length) return table.ArrayPart[key - 1];
    var k = new TValue();
    k.SetNValue(key);

    for (var node = table.GetHashNode(ref k); node != null; node = node.Next)
    {
      if (node.Val.V.TtIsFunction()) return node.Val;
    }
    return LuaTable.TheNilValue;
  }

  public static float GetFloat(this LuaTable table, string name)
  {
    var key = new TValue { OValue = name, Tt = (int)LuaType.LUA_TSTRING };
    var stk = table.Get(ref key);
    return (float)stk.V.NValue;
  }

  public static void SetFloat(this LuaTable table, string name, float value)
  {
    var key = new TValue { OValue = name, Tt = (int)LuaType.LUA_TSTRING };
    var val = new TValue { NValue = value, Tt = (int)LuaType.LUA_TNUMBER };
    table.Set(ref key, ref val);
  }

  public static Vector3 GetVector3(this LuaTable table, string name)
  {
    var vecTable = table.GetTable(name);
    var x = vecTable.GetFloat("x");
    var y = vecTable.GetFloat("y");
    var z = vecTable.GetFloat("z");
    return new Vector3(x, y, z);
  }

  public static Quaternion GetQuaternion(this LuaTable table, string name)
  {
    var vecTable = table.GetTable(name);
    var x = vecTable.GetFloat("x");
    var y = vecTable.GetFloat("y");
    var z = vecTable.GetFloat("z");
    var w = vecTable.GetFloat("w");
    return new Quaternion(x, y, z, w);
  }

  public static void SetTable(this LuaTable table, string name, LuaTable value)
  {
    var key = new TValue { OValue = name, Tt = (int)LuaType.LUA_TSTRING };
    var val = new TValue { OValue = value, Tt = (int)LuaType.LUA_TTABLE };
    table.Set(ref key, ref val);
  }

  public static LuaTable GetTable(this LuaTable table, string name)
  {
    var key = new TValue { OValue = name, Tt = (int)LuaType.LUA_TSTRING };
    var stk = table.Get(ref key);
    return stk.V.HValue();
  }

  public static void SetUnityObject(this LuaTable table, string name, Object value)
  {
    var key = new TValue { OValue = name, Tt = (int)LuaType.LUA_TSTRING };
    var val = new TValue { OValue = value, Tt = (int)LuaType.LUA_TLIGHTUSERDATA };
    table.Set(ref key, ref val);
  }

  public static GameObject GetGameObject(this LuaTable table)
  {
    var key = new TValue { OValue = "gameObject", Tt = (int)LuaType.LUA_TSTRING };
    var go = (GameObject)table.Get(ref key).V.OValue;
    key = new TValue { OValue = "transform", Tt = (int)LuaType.LUA_TSTRING };
    var transform = table.Get(ref key).V.HValue();
    go.transform.position = transform.GetVector3("position");
    go.transform.localScale = transform.GetVector3("localScale");
    go.transform.eulerAngles = transform.GetVector3("eulerAngles");
    return go;
  }

  public static void UpdateAllGoInTable(this LuaTable table)
  {
    foreach (var part in table.HashPart)
    {
      if (part.Val.V.TtIsTable())
      {
        foreach (var node in part.Val.V.HValue().HashPart)
        {
          if (node.Key.V.TtIsString() && (string)node.Key.V.OValue == "gameObject")
          {
            part.Val.V.HValue().GetGameObject();
          }
        }
      }
    }
  }
}

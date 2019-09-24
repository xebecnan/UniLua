using UniLua;
using UnityEngine;

public static class UnityExtension
{
  public static LuaTable GetTable(this Vector3 vec, LuaState lua)
  {
    var tbl = new LuaTable(lua);
    tbl.Set("x", vec.x);
    tbl.Set("y", vec.y);
    tbl.Set("z", vec.z);
    return tbl;
  }
  public static LuaTable GetTable(this Quaternion quat, LuaState lua)
  {
    var tbl = new LuaTable(lua);
    tbl.Set("x", quat.x);
    tbl.Set("y", quat.y);
    tbl.Set("z", quat.z);
    tbl.Set("w", quat.w);
    return tbl;
  }

  public static LuaTable GetTable(this Transform transform, LuaState lua)
  {
    var tbl = new LuaTable(lua);
    tbl.SetTable("position", transform.position.GetTable(lua));
    tbl.SetTable("rotation", transform.rotation.GetTable(lua));
    tbl.SetTable("localScale", transform.localScale.GetTable(lua));
    tbl.SetTable("eulerAngles", transform.eulerAngles.GetTable(lua));
    return tbl;
  }

  public static LuaTable GetTable(this GameObject go, LuaState lua)
  {
    var tbl = new LuaTable(lua);
    tbl.SetUnityObject("gameObject", go);
    tbl.SetTable("transform", go.transform.GetTable(lua));
    return tbl;
  }
}
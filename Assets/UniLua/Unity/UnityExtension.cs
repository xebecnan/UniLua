using UniLua;
using UnityEngine;

public static class UnityExtension {
  public static LuaTable GetTable(this Vector3 vec, LuaState lua) {
    var tbl = new LuaTable(lua);
    tbl.SetFloat("x", vec.x);
    tbl.SetFloat("y", vec.y);
    tbl.SetFloat("z", vec.z);
    return tbl;
  }
  public static LuaTable GetTable(this Quaternion quat, LuaState lua) {
    var tbl = new LuaTable(lua);
    tbl.SetFloat("x", quat.x);
    tbl.SetFloat("y", quat.y);
    tbl.SetFloat("z", quat.z);
    tbl.SetFloat("w", quat.w);
    return tbl;
  }

  public static LuaTable GetTable(this Transform transform, LuaState lua) {
    var tbl = new LuaTable(lua);
    tbl.SetTable("position", transform.position.GetTable(lua));
    tbl.SetTable("rotation", transform.rotation.GetTable(lua));
    tbl.SetTable("localScale", transform.localScale.GetTable(lua));
    tbl.SetTable("eulerAngles", transform.eulerAngles.GetTable(lua));
    return tbl;
  }
  
  public static LuaTable GetTable(this GameObject go, LuaState lua) {
    var tbl = new LuaTable(lua);
    tbl.SetUnityObject("gameObject", go);
    tbl.SetTable("transform", go.transform.GetTable(lua));
    return tbl;
  }
}
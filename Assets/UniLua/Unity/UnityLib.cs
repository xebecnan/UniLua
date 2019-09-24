using UniLua;
using UnityEngine;

public class UnityLib {
  public string LIB_NAME = "unitylib.cs";

  private LuaState Lua;

  public UnityLib(LuaState state) {
    Lua = state;
    Lua.L_RequireF(LIB_NAME, _OpenLib, false);
  }

  private int _OpenLib(LuaState lua) {
    var define = new NameFuncPair[] {
      new NameFuncPair("Rotate", Rotate),
      new NameFuncPair("Translate", Translate),
      new NameFuncPair("Debug", Log),
      new NameFuncPair("UpdateGo", UpdateGo),
    };

    lua.L_NewLib(define);
    return 1;
  }

  public int Rotate(LuaState lua) {
    var table = lua.ToTable(1);
    var go = table.GetGameObject();
    var x = (float) lua.ToNumber(2);
    var y = (float) lua.ToNumber(3);
    var z = (float) lua.ToNumber(4);
    var vec = new Vector3(x, y, z);

    go.transform.Rotate(vec);
    var angles = go.transform.eulerAngles.GetTable(lua);
    lua.PushTable(angles);
    return 1;
  }

  public int Translate(LuaState lua) {
    var table = lua.Last().V.HValue();
    var go = table.GetGameObject();
    go.transform.Translate(lua.GetVector());
    return 1;
  }

  public int Log(LuaState lua) {
    var x = (float) lua.L_CheckNumber(1);
    Debug.Log($"x = {x}");
    return 1;
  }

  public int UpdateGo(LuaState lua) {
    var table = lua.ToTable(1);
    table.GetGameObject();
    return 1;
  }
}

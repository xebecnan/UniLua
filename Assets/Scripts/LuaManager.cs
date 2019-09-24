using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UniLua;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class LuaManager : MonoBehaviour {
  public int maxCubes = 20;

  public float speedMin;
  public float speedMax;
  public string luaFile = "make.lua";

  public GameObject go1;
  public GameObject go2;

  private LuaState Lua;
  private int luaMain;

  private UnityLib _lib;

  private List<GameObject> objects = new List<GameObject>();
  private List<LuaTable> tables = new List<LuaTable>();

  private Vector3 _scale1;
  private Vector3 _scale2;

  void Start() {
    //for (var i = 0; i < maxCubes; i++) {
    //  for (var j = 0; j < maxCubes; j++) {
    //    for (var k = 0; k < maxCubes; k++) {
    //      var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //      cube.transform.position = new Vector3(i, j, k);
    //      objects.Add(cube);
    //    }
    //  }
    //}

    Lua = new LuaState();
    Lua.L_OpenLibs();
    _lib = new UnityLib(Lua);

    var status = Lua.L_DoFile(luaFile);
    if (status != ThreadStatus.LUA_OK) throw new Exception(Lua.ToString(-1));

    if (!Lua.IsTable(-1)) throw new Exception("Framework main's return value is not a table");

    Lua.GetField(-1, "make");
    if (!Lua.IsFunction(-1)) throw new Exception("Method make not found!");
    luaMain = Lua.L_Ref(LuaDef.LUA_REGISTRYINDEX);

    tables.Add(Lua.CallFunction(luaMain, new object[]{ go1.GetTable(Lua), go2.GetTable(Lua) }));

    _scale1 = Vector3.one;
    _scale2 = Vector3.one;
  }

  void Update() {
    foreach (var tbl in tables) {
      Lua.CallProcedure(tbl, "rotate", new object[]{ (Vector3.one * 0.01f).GetTable(Lua) });

      _scale1.x += 0.001f;
      _scale2.z += 0.001f;
      Lua.CallProcedure(tbl, "scale", new object[] { _scale1.GetTable(Lua), _scale2.GetTable(Lua) });
    }
  }
}

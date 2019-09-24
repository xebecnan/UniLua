using System;
using UniLua;
using UnityEngine;

public static class LuaStateExtensions
{
  public static int StoreFunction(this ILuaState state, string name)
  {
    state.GetField(-1, name);
    if (!state.IsFunction(-1)) throw new Exception($"Function '{name}' not found");

    return state.L_Ref(LuaDef.LUA_REGISTRYINDEX);
  } // StoreFunction

  public static Vector3 GetVector(this LuaState lua)
  {
    var tbl = lua.Last().V.HValue();
    var x = tbl.GetFloat("x");
    var y = tbl.GetFloat("y");
    var z = tbl.GetFloat("z");
    lua.Pop(1);
    return new Vector3(x, y, z);
  }

  public static StkId Last(this LuaState lua)
  {
    var top = lua.Top.Index - 1;
    return lua.Stack[top];
  }

  public static void PushFunctionFromTable(this LuaState lua, LuaTable tbl, string funcName)
  {
    var idx = tbl.GetFunction(funcName);
    idx.Index = lua.Top.Index;
    idx.SetList(lua.Stack);
    lua.Stack[lua.Top.Index] = idx;
    lua.ApiIncrTop();
  }

  public static void PushTable(this LuaState lua, LuaTable tbl)
  {
    lua.Top.V.SetHValue(tbl);
    lua.Top.SetList(lua.Stack);
    lua.ApiIncrTop();
  }

  public static LuaTable ToTable(this LuaState lua, int index, out bool isTable)
  {
    StkId addr;
    if (!lua.Index2Addr(index, out addr))
    {
      isTable = false;
      return null;
    }

    if (addr.V.TtIsTable())
    {
      isTable = true;
      return addr.V.HValue();
    }

    isTable = false;
    return null;
  }

  public static LuaTable ToTable(this LuaState lua, int index)
  {
    bool flag = false;
    return lua.ToTable(index, out flag);
  }

  public static void CallProcedure(this LuaState lua, LuaTable table, string name, object[] parameters)
  {
    lua.PushFunctionFromTable(table, name);
    lua.PushTable(table);                                 // self
    lua.PushParameters(parameters);                      // parameters
    var status = lua.PCall(parameters.Length + 1, 0, 0);      // call
    if (status != ThreadStatus.LUA_OK) Debug.LogError($"{lua.ToString(-1)}, status - {status}");

    table.UpdateAllGoInTable();
  }

  public static LuaTable CallFunction(this LuaState lua, LuaTable table, string name, object[] parameters)
  {
    lua.PushFunctionFromTable(table, name);
    lua.PushTable(table);                                     // self
    lua.PushParameters(parameters);                           // parameters
    var status = lua.PCall(parameters.Length + 1, 1, 0);      // call
    if (status != ThreadStatus.LUA_OK) Debug.LogError($"{lua.ToString(-1)}, status - {status}");

    var ret = LuaTable.Clone(lua.Last().V.HValue());
    lua.Pop(1);
    table.UpdateAllGoInTable();
    return ret;
  }

  public static LuaTable CallFunction(this LuaState lua, int index, params object[] parameters)
  {
    lua.RawGetI(LuaDef.LUA_REGISTRYINDEX, index);
    lua.PushParameters(parameters);
    var status = lua.PCall(parameters.Length, 1, 0);      // call
    if (status != ThreadStatus.LUA_OK) Debug.LogError($"{lua.ToString(-1)}, status - {status}");

    var ret = LuaTable.Clone(lua.Last().V.HValue());
    lua.Pop(1);
    return ret;
  }

  public static void PushParameters(this LuaState lua, object[] parameters)
  {
    foreach (var value in parameters)
    {
      if (value == null)
      {
        lua.PushNil();
        continue;
      }

      if (value is LuaTable)
      {
        lua.PushTable((LuaTable)value);
        continue;
      }

      if (value is float || value is double)
      {
        lua.PushNumber((double)value);
        continue;
      }

      if (value is int || value is long || value is short || value is byte)
      {
        lua.PushNumber((double)value);
        continue;
      }

      if (value is ulong || value is uint || value is ushort)
      {
        lua.PushUInt64((ulong)value);
        continue;
      }

      if (value is string)
      {
        lua.PushString((string)value);
        continue;
      }
    }
  }

}

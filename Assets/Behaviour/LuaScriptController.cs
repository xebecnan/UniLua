﻿using System;
using UnityEngine;
using UniLua;

public class LuaScriptController : MonoBehaviour {
	public	string		LuaScriptFile = "framework/main.lua";

	private ILuaState 	Lua;
	private int			AwakeRef;
	private int			StartRef;
	private int			UpdateRef;
	private int			LateUpdateRef;
	private int			FixedUpdateRef;

	void Awake() {
		Debug.Log("LuaScriptController Awake");

		if( Lua == null )
		{
			Lua = LuaAPI.NewState();
			Lua.L_OpenLibs();

			var status = Lua.L_DoFile( LuaScriptFile );
			if( status != ThreadStatus.LUA_OK )
			{
				throw new Exception( Lua.ToString(-1) );
			}

			if( ! Lua.IsTable(-1) )
			{
				throw new Exception(
					"framework main's return value is not a table" );
			}

			AwakeRef 		= StoreMethod( "awake" );
			StartRef 		= StoreMethod( "start" );
			UpdateRef 		= StoreMethod( "update" );
			LateUpdateRef 	= StoreMethod( "late_update" );
			FixedUpdateRef 	= StoreMethod( "fixed_update" );

			Lua.Pop(1);
			Debug.Log("Lua Init Done");
		}

		CallMethod( AwakeRef );
	}

	void Start() {
		CallMethod( StartRef );
	}

	void Update() {
		CallMethod( UpdateRef );
	}

	void LateUpdate() {
		CallMethod( LateUpdateRef );
	}

	void FixedUpdate() {
		CallMethod( FixedUpdateRef );
	}

	private int StoreMethod( string name )
	{
		Lua.GetField( -1, name );
		if( !Lua.IsFunction( -1 ) )
		{
			throw new Exception( string.Format(
				"method {0} not found!", name ) );
		}
		return Lua.L_Ref( LuaDef.LUA_REGISTRYINDEX );
	}

	private void CallMethod( int funcRef )
	{
		Lua.RawGetI( LuaDef.LUA_REGISTRYINDEX, funcRef );

		// insert `traceback' function
		var b = Lua.GetTop();
		Lua.PushCSharpFunction( Traceback );
		Lua.Insert(b);

		var status = Lua.PCall( 0, 0, b );
		if( status != ThreadStatus.LUA_OK )
		{
			Debug.LogError( Lua.ToString(-1) );
		}

		// remove `traceback' function
		Lua.Remove(b);
	}

	private static int Traceback(ILuaState lua) {
		var msg = lua.ToString(1);
		if(msg != null) {
			lua.L_Traceback(lua, msg, 1);
		}
		// is there an error object?
		else if(!lua.IsNoneOrNil(1)) {
			// try its `tostring' metamethod
			if(!lua.L_CallMeta(1, "__tostring")) {
				lua.PushString("(no error message)");
			}
		}
		return 1;
	}
}


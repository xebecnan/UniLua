# UniLua #

A pure C# implementation of Lua 5.2 focus on compatibility with Unity3D.

Being used in commercial MMORPG game development.

## Quick Start ##

Open Assets\Stages\GameMain.unity and run it.

You can use WSAD keys to control the icon in this sample project.

## Libraries ##

*   Base lib: done
*   Package lib: done
*   Coroutine lib: done
*   Table lib: done
*   IO lib: done
*   OS lib: not implemented
*   String lib: partially implemented
*   Debug lib: partially implemented

## Additional Libraries ##

*   FFI lib: basicly done
*   Encoding lib: support convert between UTF-8 and UTF-16(C# builtin)

## TODO ##

*   Complete string lib.
*   Complete debug lib.

## Known Issues ##

*   Metamethod '__gc' is not working.
*   Weak tables is not supported: '__mode' is not working.

## 一些简单的说明 ##

最朴素的从 C# 调用 lua 的一个全局函数的写法:

<pre>
Lua.GetGlobal( "foo" ); // 加载 lua 中定义的一个名叫 foo 的全局函数到堆栈
Debug.Assert( Lua.IsFunction(-1) ); // 确保加载成功了, 此时栈顶是函数 foo
Lua.PushString( "test" ); // 将第一个参数(字符串 "test")入栈
Lua.PushInteger( 42 ); //将第二个参数(整数 42)入栈
Lua.Call(2, 0); // 调用函数 foo, 指明有2个参数，没有返回值
// 上面的代码相当于 lua 里一个这样的调用 foo("test", 42)
</pre>


稍微复杂一点的例子可以参考实例程序里的一些简单写法：
参考这个文件 Assets/Behaviour/LuaScriptController.cs：

* <a href="https://github.com/xebecnan/UniLua/blob/master/Assets/Behaviour/LuaScriptController.cs">Assets/Behaviour/LuaScriptController.cs</a>
* <a href="https://github.com/xebecnan/UniLua/blob/master/Assets/StreamingAssets/LuaRoot/framework/main.lua">framework/main.lua</a>

<pre>
// 创建 Lua 虚拟机
var Lua = LuaAPI.NewState();

// 加载基本库
Lua.L_OpenLibs();

// 加载 Lua 脚本文件
var LuaScriptFile = "framework/main.lua";
var status = Lua.L_DoFile( LuaScriptFile );

// 捕获错误
if( status != ThreadStatus.LUA_OK )
{
    throw new Exception( Lua.ToString(-1) );
}

// 确保 framework/main.lua 执行结果是一个 Lua Table
if( ! Lua.IsTable(-1) )
{
  throw new Exception(
		"framework main's return value is not a table" );
}

// 从 framework/main.lua 返回的 table 中读取 awake 字段指向的函数
// 并保存到 AwakeRef 中 (可以将 AwakeRef 视为这个函数的句柄)
var AwakeRef = StoreMethod( "awake" );

// 不再需要 framework/main.lua 返回的 table 了，将其从栈上弹出
Lua.Pop(1);

//----------------------------------------------------

// 在需要的时候可以这样调用 AwakeRef 指向的 lua 函数
CallMethod( AwakeRef );

//----------------------------------------------------
// StoreMethod 和 CallMethod 的实现

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
	var status = Lua.PCall( 0, 0, 0 );
	if( status != ThreadStatus.LUA_OK )
	{
		Debug.LogError( Lua.ToString(-1) );
	}
}
</pre>

# UniLua #

A pure C# implementation of Lua 5.2 focus on compatibility with Unity3D.  
Being used in commercial MMORPG game development.

UniLua是一个纯C#的Lua 5.2实现，专注于与Unity3D的兼容性。  
目前正使用在我们大型多人在线角色扮演商业游戏的开发中。

## 示例工程 // Sample Project ##

To demonstrate the basic use of UniLua, a sample project is included.  
Open Assets\Stages\GameMain.unity with Unity3D, and just click the "Play" button.  
An icon will appear in the screen, and you can move it around with WSAD keys.

项目中包含了一个微型的示例工程，用来演示 UniLua 的基本使用。  
用 Unity3D 打开 Assets\Stages\GameMain.unity 然后直接点击播放按钮运行。  
屏幕上会显示一个小图标，你可以用 WSAD 键控制它四处移动。

## 开发状况 // Development Status ##

* 基本特性 // Basic features
  * 所有 Lua 的基本语言特性都已实现，包括`协程`和`元表`，并且与 Lua5.2 标准实现一致。部分 GC 相关的元方法如 `__gc` 和 `__mode` 未实现  
    // All language features are implemented exactly the same as the standard Lua 5.2, including `coroutine` and `metatable`, except some GC-related metamethods like `__gc` and `__mode`.
* 内置库 // Libraries
  * Base lib: done
  * Package lib: done
  * Coroutine lib: done
  * Table lib: done
  * IO lib: not implemented
     * 因为暂时没有需求 // not needed in our games right now
  * OS lib: not implemented
     * 因为暂时没有需求 // not needed in our games right now
  * String lib: partially implemented
     * 因为暂时没有需求 // not needed in our games right now
  * Debug lib: partially implemented
     * 勉强够用了 // barely enough

* 额外实现的库 // Additional Libraries
  * FFI lib: basicly done
     * 实验性质,不建议在要求性能的环境下使用 // experimental. not suggested to use in performance-critical situation
  * Encoding lib: basicly done
     * 支持在 UTF-8 编码和 UTF-16 编码间进行转换 // support convert between UTF-8 and UTF-16

* TODO
  * Complete string lib.
  * Complete debug lib.

* 已知的问题 // Known Issues
  * Metamethod '__gc' will not working.
     * 因为没有自己实现GC机制,而是依赖于C#的GC // for directly depending on C#'s GC mechanism
  * Weak tables is not supported: '__mode' will not working.
     * 原因同上 // the same reason mentioned above

## 一些简单的说明 // Quick Start ##

You could use UniLua in reference to the standard Lua.  
The syntax of Lua language is exactly the same as standard Lua.  
So you could make use of the standard [Lua 5.2 Reference Manual](http://www.lua.org/manual/5.2/).  

Most C API of standard Lua have a counterpart in C# API of UniLua.  
For example, instead of using lua_pushnumber(L, 42), you can use L.PushNumber(42)  
Interface functions defined in lua.h and lauxlib.h can be found in "**interface ILuaAPI**" defined in [LuaAPI.cs](https://github.com/xebecnan/UniLua/blob/master/Assets/UniLua/LuaAPI.cs) and "**interface ILuaAuxLib**" defined in [LuaAuxLib.cs](https://github.com/xebecnan/UniLua/blob/master/Assets/UniLua/LuaAuxLib.cs)

大部分的使用是可以参考标准的 Lua 官方文档和 Lua 教程的。  
Lua 本身的语法是完全一样的。你可以用[官方的文档](http://www.lua.org/manual/5.2/)来帮助理解和使用UniLua

C API 和 C# API 之间有个对应关系。  
例如 lua_pushnumber() 这个 C API 对应到 UniLua 里就是 lua.PushNumber()  
所有标准 lua 中 lua.h 和 lauxlib.h 里定义的接口，都对应 <a href="https://github.com/xebecnan/UniLua/blob/master/Assets/UniLua/LuaAPI.cs">LuaAPI.cs</a> 里定义的 ILuaAPI 和 <a href="https://github.com/xebecnan/UniLua/blob/master/Assets/UniLua/LuaAuxLib.cs">LuaAuxLib.cs</a> 里定义的 ILuaAuxLib 接口。


### 从 C# 调用 Lua // Calling Lua function from C# ###

The simplest way to call a lua global function from C#:  
最朴素的从 C# 调用 lua 的一个全局函数的写法:

<pre>
// 相当于 lua 里一个这样的调用 foo("test", 42)
// equal to lua code: foo("test", 42)

Lua.GetGlobal( "foo" ); // 加载 lua 中定义的一个名叫 foo 的全局函数到堆栈
Debug.Assert( Lua.IsFunction(-1) ); // 确保加载成功了, 此时栈顶是函数 foo
Lua.PushString( "test" ); // 将第一个参数(字符串 "test")入栈
Lua.PushInteger( 42 ); //将第二个参数(整数 42)入栈
Lua.Call(2, 0); // 调用函数 foo, 指明有2个参数，没有返回值

</pre>

More complicated examples can be found in:  
稍微复杂一点的例子可以参考实例程序里的一些简单写法。参考：

* [Assets/Behaviour/LuaScriptController.cs](https://github.com/xebecnan/UniLua/blob/master/Assets/Behaviour/LuaScriptController.cs)
* [framework/main.lua](https://github.com/xebecnan/UniLua/blob/master/Assets/StreamingAssets/LuaRoot/framework/main.lua)

<pre>
// 创建 Lua 虚拟机
// create Lua VM instance
var Lua = LuaAPI.NewState();

// 加载基本库
// load base libraries
Lua.L_OpenLibs();

// 加载并运行 Lua 脚本文件
// load and run Lua script file
var LuaScriptFile = "framework/main.lua";
var status = Lua.L_DoFile( LuaScriptFile );

// 捕获错误
// capture errors
if( status != ThreadStatus.LUA_OK )
{
    throw new Exception( Lua.ToString(-1) );
}

// 确保 framework/main.lua 执行结果是一个 Lua table
// ensuare the value returned by 'framework/main.lua' is a Lua table
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

### 从 Lua 调用 C# 函数 // Calling C# funcitons from Lua ###
( 使用 C# 来扩展 Lua 功能 // extending Lua with C# )

目前的示例程序是使用 FFI 库来实现的 从 Lua 调用 C# 函数。
FFI 因为用到了反射机制来调用 C# 函数，性能会比较低。
应该尽量避免使用，如果没有找到更好的办法，准备之后把这个FFI实现废弃掉。
其实直接用 C# 实现一个库的形式，来让 lua 调用这种传统的做法效率会比较高，也是推荐采用的方式。而且也并不会麻烦太多。

比如我现在要实现一个叫 libfoo 的库, 里面提供两个方法: add(a, b) 和 sub(a, b)

库的实现

<pre>
using UniLua;
public static class LibFoo
{
    public const string LIB_NAME = "libfoo.cs"; // 库的名称, 可以是任意字符串
    
    public static int OpenLib(ILuaState lua) // 库的初始化函数
    {
        var define = new NameFuncPair[]
        {
            new NameFuncPair("add", Add),
            new NameFuncPair("sub", Sub),
        };
        
        lua.L_NewLib(define);
        return 1;
    }
    
    public static int Add(ILuaState lua)
    {
        var a = lua.L_CheckNumber( 1 ); // 第一个参数
        var b = lua.L_CheckNumber( 2 ); // 第二个参数
        var c = a + b; // 执行加法操作
        lua.PushNumber( c ); // 将返回值入栈
        return 1; // 有一个返回值
    }
    
    public static int Sub(ILuaState lua)
    {
        var a = lua.L_CheckNumber( 1 ); // 第一个参数
        var b = lua.L_CheckNumber( 2 ); // 第二个参数
        var c = a - b; // 执行减法操作
        lua.PushNumber( c ); // 将返回值入栈
        return 1; // 有一个返回值
    }
}

</pre>

库的初始化

<pre>

// 创建 Lua 虚拟机
var Lua = LuaAPI.NewState();

// 加载基本库
Lua.L_OpenLibs();

Lua.L_RequireF( LibFoo.LIB_NAME  // 库的名字
              , LibFoo.OpenLib   // 库的初始化函数
              , false            // 不默认放到全局命名空间 (在需要的地方用require获取)
              );

</pre>

库的使用 (在 lua 代码中)

<pre>

// 获取库
local libfoo = require "libfoo.cs"

// 调用库的方法
print(libfoo.add(42, 1))
print(libfoo.sub(42, 22))

</pre>

### UTF-8 support ###

C# 采用 UTF-16 作为字符串的内部编码，而 Lua 本身没有实现比较完善的编码支持。
为了处理这个问题，我实现了一个简单的编码库 enc lib。使用方法如下：

<pre>
-- Assuming your source code is in utf-8.
-- convert from utf-8:
local utf8_str = '测试字符串'
local print_safe_str = enc.decode(utf8_str, 'utf8')
print(print_safe_str)

-- convert to utf-8:
local original_str = enc.encode(print_safe_str, 'utf8')
assert(utf8_str == original_str)
</pre>

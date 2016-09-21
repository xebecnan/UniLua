# UniLua #

A pure C# implementation of Lua 5.2 focus on compatibility with Unity3D.  

UniLua是一个纯C#的Lua 5.2实现，专注于与Unity3D的兼容性。  

## 一些补充说明

UniLua 主要关注的还是对 lua 本身的实现，而不是怎么把 Unity3D 引擎提供的功能都引入到 lua 里。

从 lua 调用 C# 函数不建议使用 FFI 库(虽然示例工程里用了，看起来方便，但是并不完善，效率也不好)。建议参考 [从 Lua 调用 C# 函数 ( Calling C# funcitons from Lua )](https://github.com/xebecnan/UniLua/wiki/%E4%BB%8E-Lua-%E8%B0%83%E7%94%A8-C%23-%E5%87%BD%E6%95%B0-%28-Calling-C%23-funcitons-from-Lua-%29) 来自己实现封装函数。


## 示例工程 ( Sample Project ) ##

To demonstrate the basic use of UniLua, a sample project is included.  
Open Assets\Stages\GameMain.unity with Unity3D, and just click the "Play" button.  
An icon will appear in the screen, and you can move it around with WSAD keys.

项目中包含了一个微型的示例工程，用来演示 UniLua 的基本使用。  
用 Unity3D 打开 Assets\Stages\GameMain.unity 然后直接点击播放按钮运行。  
屏幕上会显示一个小图标，你可以用 WSAD 键控制它四处移动。

## 开发状况 ( Development Status ) ##

* 基本特性 ( Basic features )
  * 所有 Lua 的基本语言特性都已实现，包括`协程`和`元表`，并且与 Lua5.2 标准实现一致。部分 GC 相关的元方法如 `__gc` 和 `__mode` 未实现  
    ( All language features are implemented exactly the same as the standard Lua 5.2, including `coroutine` and `metatable`, except some GC-related metamethods like `__gc` and `__mode`. )
* 内置库 ( Libraries )
  * Base lib: done
  * Package lib: done
  * Coroutine lib: done
  * Table lib: done
  * IO lib: not implemented
     * 因为暂时没有需求 ( not needed in our games right now )
  * OS lib: not implemented
     * 因为暂时没有需求 ( not needed in our games right now )
  * String lib: partially implemented
     * 因为暂时没有需求 ( not needed in our games right now )
  * Debug lib: partially implemented
     * 勉强够用了 ( barely enough )

* 额外实现的库 ( Additional Libraries )
  * FFI lib: basicly done
     * 实验性质,不建议在要求性能的环境下使用 ( experimental. not suggested to use in performance-critical situation )
  * Encoding lib: basicly done
     * 支持在 UTF-8 编码和 UTF-16 编码间进行转换 ( support convert between UTF-8 and UTF-16 )

* TODO
  * Complete string lib.
  * Complete debug lib.

* 已知的问题 ( Known Issues )
  * Metamethod '__gc' will not working.
     * 因为没有自己实现GC机制,而是依赖于C#的GC ( for directly depending on C#'s GC mechanism )
  * Weak tables is not supported: '__mode' will not working.
     * 原因同上 ( the same reason mentioned above )
  * full userdata is not supported

## SciMark ##

test on Unity3D 4.3.1, Windows 7, Intel i5-3470

* FFT 1.07  [1024]
* SOR 2.51  [100]
* MC 0.66
* SPARSE 1.59  [1000, 5000]
* LU 1.84  [100]
* SciMark 1.53  [small problem sizes]

## 常用链接 ( Links )##

* [Wiki首页 (Wiki Homepage)](https://github.com/xebecnan/UniLua/wiki)
  * [一些简单的说明 ( Quick Start )](https://github.com/xebecnan/UniLua/wiki/%E4%B8%80%E4%BA%9B%E7%AE%80%E5%8D%95%E7%9A%84%E8%AF%B4%E6%98%8E-%28-Quick-Start-%29)
  * [从 C# 调用 Lua ( Calling Lua function from C# )](https://github.com/xebecnan/UniLua/wiki/%E4%BB%8E-C%23-%E8%B0%83%E7%94%A8-Lua-%28-Calling-Lua-function-from-C%23-%29)
  * [从 Lua 调用 C# 函数 ( Calling C# funcitons from Lua )](https://github.com/xebecnan/UniLua/wiki/%E4%BB%8E-Lua-%E8%B0%83%E7%94%A8-C%23-%E5%87%BD%E6%95%B0-%28-Calling-C%23-funcitons-from-Lua-%29)
  * [从AssetBundle加载代码 ( Loading code from asset bundles )](https://github.com/xebecnan/UniLua/wiki/%E4%BB%8Eassetbundle%E5%8A%A0%E8%BD%BD%E4%BB%A3%E7%A0%81-%28-loading-code-from-asset-bundles-%29)
  * [UTF-8 support](https://github.com/xebecnan/UniLua/wiki/Utf-8-support)
  * [Reference: Lua functions](https://github.com/xebecnan/UniLua/wiki/Lua-functions)
  * [Reference: C# API](https://github.com/xebecnan/UniLua/wiki/C%23-API)
* 文章 Articles
  * [从零开始实现 Lua 虚拟机 ( UniLua 开发过程 )](https://zhuanlan.zhihu.com/p/22476315)
  * [Unity3D游戏开发之Lua与游戏的不解之缘](http://blog.csdn.net/qinyuanpei/article/details/40050225)
  * [unityでuniluaを使ってADV機能を実装する](http://qiita.com/masakam1/items/62d6e5968443836689c2)
  * [Создание игры на ваших глазах — часть 3: Прикручиваем скриптовый язык к Unity (UniLua)](https://habrahabr.ru/post/211576/)
* 替代品 Replacement
  * [Moon#](http://www.moonsharp.org/) Based on Lua 5.2
  * [KopiLua](http://www.ppl-pilot.com/KopiLua.aspx) based on Lua 5.1.4

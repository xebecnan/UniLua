# UniLua #

A pure C# implementation of Lua 5.2 focus on compatibility with Unity3D.

Being used in commercial MMORPG game development.

## Quick Start ##

Open Assets\Stages\GameMain.unity and run it.

You can use WSAD keys to control the icon in this sample project.

## Original Libraries Status ##

*   Base lib: done
*   Package lib: done
*   Coroutine lib: done
*   Table lib: done
*   IO lib: done
*   OS lib: not implemented
*   String lib: partially implemented
*   Debug lib: partially implemented

## Additional Libraries Status ##

*   FFI lib: basicly done
*   Encoding lib: support convert between UTF-8 and UTF-16(C# builtin)

## TODO ##

*   Complete string lib.
*   Complete debug lib.

## Known Issues ##

*   Metamethod '__gc' is not working.
*   Weak tables is not supported: '__mode' is not working.

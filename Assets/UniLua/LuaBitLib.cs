
namespace UniLua
{

	internal class LuaBitLib
	{
		public const string LIB_NAME = "bit32";
		private const int LUA_NBITS = 32;
		private const uint ALLONES = ~(((~(uint)0)<<(LUA_NBITS-1)) << 1);

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "arshift", 	LuaBitLib.B_ArithShift ),
				new NameFuncPair( "band", 		LuaBitLib.B_And ),
				new NameFuncPair( "bnot", 		LuaBitLib.B_Not ),
				new NameFuncPair( "bor", 		LuaBitLib.B_Or ),
				new NameFuncPair( "bxor", 		LuaBitLib.B_Xor ),
				new NameFuncPair( "btest", 		LuaBitLib.B_Test ),
				new NameFuncPair( "extract", 	LuaBitLib.B_Extract ),
				new NameFuncPair( "lrotate", 	LuaBitLib.B_LeftRotate ),
				new NameFuncPair( "lshift", 	LuaBitLib.B_LeftShift ),
				new NameFuncPair( "replace", 	LuaBitLib.B_Replace ),
				new NameFuncPair( "rrotate", 	LuaBitLib.B_RightRotate ),
				new NameFuncPair( "rshift", 	LuaBitLib.B_RightShift ),
			};

			lua.L_NewLib( define );
			return 1;
		}

		private static uint Trim( uint x )
		{
			return (x & ALLONES);
		}

		private static uint Mask( int n )
		{
			return ~((ALLONES << 1) << (n-1));
		}

		private static int B_Shift( ILuaState lua, uint r, int i )
		{
			if( i < 0 ) // shift right?
			{
				i = -i;
				r = Trim( r );
				if( i >= LUA_NBITS ) r = 0;
				else r >>= i;
			}
			else // shift left
			{
				if( i >= LUA_NBITS ) r = 0;
				else r <<= i;
				r = Trim(r);
			}
			lua.PushUnsigned( r );
			return 1;
		}

		private static int B_LeftShift( ILuaState lua )
		{
			return B_Shift( lua, lua.L_CheckUnsigned(1), lua.L_CheckInteger(2) );
		}

		private static int B_RightShift( ILuaState lua )
		{
			return B_Shift( lua, lua.L_CheckUnsigned(1), -lua.L_CheckInteger(2) );
		}

		private static int B_ArithShift( ILuaState lua )
		{
			uint r = lua.L_CheckUnsigned( 1 );
			int i = lua.L_CheckInteger( 2 );
			if( i < 0 || ((r & ((uint)1 << (LUA_NBITS-1))) == 0) )
				return B_Shift( lua, r, -i );
			else // arithmetic shift for `nagetive' number
			{
				if( i>= LUA_NBITS )
					r = ALLONES;
				else
					r = Trim((r >> i) | ~(~(uint)0 >> i)); // add signal bit
				lua.PushUnsigned( r );
			}
			return 1;
		}

		private static uint AndAux( ILuaState lua )
		{
			int n = lua.GetTop();
			uint r = ~(uint)0;
			for( int i=1; i<=n; ++i )
			{
				r &= lua.L_CheckUnsigned( i );
			}
			return Trim( r );
		}

		private static int B_And( ILuaState lua )
		{
			uint r = AndAux( lua );
			lua.PushUnsigned( r );
			return 1;
		}

		private static int B_Not( ILuaState lua )
		{
			uint r = ~lua.L_CheckUnsigned( 1 );
			lua.PushUnsigned( Trim(r) );
			return 1;
		}

		private static int B_Or( ILuaState lua )
		{
			int n = lua.GetTop();
			uint r = 0;
			for( int i=1; i<=n; ++i )
			{
				r |= lua.L_CheckUnsigned( i );
			}
			lua.PushUnsigned( Trim(r) );
			return 1;
		}

		private static int B_Xor( ILuaState lua )
		{
			int n = lua.GetTop();
			uint r = 0;
			for( int i=1; i<=n; ++i )
			{
				r ^= lua.L_CheckUnsigned( i );
			}
			lua.PushUnsigned( Trim(r) );
			return 1;
		}

		private static int B_Test( ILuaState lua )
		{
			uint r = AndAux( lua );
			lua.PushBoolean( r != 0 );
			return 1;
		}

		private static int FieldArgs( ILuaState lua, int farg, out int width )
		{
			int f = lua.L_CheckInteger( farg );
			int w = lua.L_OptInt( farg+1, 1 );
			lua.L_ArgCheck( 0 <= f, farg, "field cannot be nagetive" );
			lua.L_ArgCheck( 0 < w, farg+1, "width must be positive" );
			if( f + w > LUA_NBITS )
				lua.L_Error( "trying to access non-existent bits" );
			width = w;
			return f;
		}

		private static int B_Extract( ILuaState lua )
		{
			uint r = lua.L_CheckUnsigned( 1 );
			int w;
			int f = FieldArgs( lua, 2, out w );
			r = (r >> f) & Mask(w);
			lua.PushUnsigned( r );
			return 1;
		}

		private static int B_Rotate( ILuaState lua, int i )
		{
			uint r = lua.L_CheckUnsigned( 1 );
			i &= (LUA_NBITS-1); // i = i % NBITS
			r = Trim( r );
			r = (r << i) | (r >> (LUA_NBITS - i));
			lua.PushUnsigned( Trim(r) );
			return 1;
		}

		private static int B_LeftRotate( ILuaState lua )
		{
			return B_Rotate( lua, lua.L_CheckInteger(2) );
		}

		private static int B_RightRotate( ILuaState lua )
		{
			return B_Rotate( lua, -lua.L_CheckInteger(2) );
		}

		private static int B_Replace( ILuaState lua )
		{
			uint r = lua.L_CheckUnsigned( 1 );
			uint v = lua.L_CheckUnsigned( 2 );
			int w;
			int f = FieldArgs( lua, 3, out w );
			uint m = Mask( w );
			v &= m; //erase bits outside given width
			r = (r & ~(m << f)) | (v << f);
			lua.PushUnsigned( r );
			return 1;
		}

	}

}


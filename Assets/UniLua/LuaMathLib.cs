
namespace UniLua
{
	using Math = System.Math;
	using Double = System.Double;
	using Random = System.Random;
	using BitConverter = System.BitConverter;

	internal class LuaMathLib
	{
		public const string LIB_NAME = "math";

		private const double RADIANS_PER_DEGREE = Math.PI / 180.0;

		private static Random RandObj;

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "abs",   		Math_Abs ),
				new NameFuncPair( "acos",  		Math_Acos ),
				new NameFuncPair( "asin",  		Math_Asin ),
				new NameFuncPair( "atan2", 		Math_Atan2 ),
				new NameFuncPair( "atan",  		Math_Atan ),
				new NameFuncPair( "ceil",  		Math_Ceil ),
				new NameFuncPair( "cosh",  		Math_Cosh ),
				new NameFuncPair( "cos",   		Math_Cos ),
				new NameFuncPair( "deg",   		Math_Deg ),
				new NameFuncPair( "exp",   		Math_Exp ),
				new NameFuncPair( "floor", 		Math_Floor ),
				new NameFuncPair( "fmod",  		Math_Fmod ),
				new NameFuncPair( "frexp", 		Math_Frexp ),
				new NameFuncPair( "ldexp", 		Math_Ldexp ),
				new NameFuncPair( "log10", 		Math_Log10 ),
				new NameFuncPair( "log",   		Math_Log ),
				new NameFuncPair( "max",   		Math_Max ),
				new NameFuncPair( "min",   		Math_Min ),
				new NameFuncPair( "modf",  		Math_Modf ),
				new NameFuncPair( "pow",   		Math_Pow ),
				new NameFuncPair( "rad",   		Math_Rad ),
				new NameFuncPair( "random",     Math_Random ),
				new NameFuncPair( "randomseed", Math_RandomSeed ),
				new NameFuncPair( "sinh", 		Math_Sinh ),
				new NameFuncPair( "sin",   		Math_Sin ),
				new NameFuncPair( "sqrt",  		Math_Sqrt ),
				new NameFuncPair( "tanh",   	Math_Tanh ),
				new NameFuncPair( "tan",   		Math_Tan ),
			};

			lua.L_NewLib( define );

			lua.PushNumber( Math.PI );
			lua.SetField( -2, "pi" );

			lua.PushNumber( Double.MaxValue );
			lua.SetField( -2, "huge" );

			RandObj = new Random();

			return 1;
		}

		private static int Math_Abs( ILuaState lua )
		{
			lua.PushNumber( Math.Abs( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Acos( ILuaState lua )
		{
			lua.PushNumber( Math.Acos( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Asin( ILuaState lua )
		{
			lua.PushNumber( Math.Asin( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Atan2( ILuaState lua )
		{
			lua.PushNumber( Math.Atan2( lua.L_CheckNumber(1),
										lua.L_CheckNumber(2)));
			return 1;
		}

		private static int Math_Atan( ILuaState lua )
		{
			lua.PushNumber( Math.Atan( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Ceil( ILuaState lua )
		{
			lua.PushNumber( Math.Ceiling( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Cosh( ILuaState lua )
		{
			lua.PushNumber( Math.Cosh( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Cos( ILuaState lua )
		{
			lua.PushNumber( Math.Cos( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Deg( ILuaState lua )
		{
			lua.PushNumber( lua.L_CheckNumber(1) / RADIANS_PER_DEGREE );
			return 1;
		}

		private static int Math_Exp( ILuaState lua )
		{
			lua.PushNumber( Math.Exp( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Floor( ILuaState lua )
		{
			lua.PushNumber( Math.Floor( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Fmod( ILuaState lua )
		{
			lua.PushNumber( Math.IEEERemainder( lua.L_CheckNumber(1),
												lua.L_CheckNumber(2)));
			return 1;
		}

		private static int Math_Frexp( ILuaState lua )
		{
			double d = lua.L_CheckNumber(1);

			// Translate the double into sign, exponent and mantissa.
			long bits = BitConverter.DoubleToInt64Bits(d);
			// Note that the shift is sign-extended, hence the test against -1 not 1
			bool negative = (bits < 0);
			int exponent = (int) ((bits >> 52) & 0x7ffL);
			long mantissa = bits & 0xfffffffffffffL;

			// Subnormal numbers; exponent is effectively one higher,
			// but there's no extra normalisation bit in the mantissa
			if (exponent==0)
			{
				exponent++;
			}
			// Normal numbers; leave exponent as it is but add extra
			// bit to the front of the mantissa
			else
			{
				mantissa = mantissa | (1L<<52);
			}

			// Bias the exponent. It's actually biased by 1023, but we're
			// treating the mantissa as m.0 rather than 0.m, so we need
			// to subtract another 52 from it.
			exponent -= 1075;

			if (mantissa == 0) 
			{
				lua.PushNumber( 0.0 );
				lua.PushNumber( 0.0 );
				return 2;
			}

			/* Normalize */
			while((mantissa & 1) == 0) 
			{    /*  i.e., Mantissa is even */
				mantissa >>= 1;
				exponent++;
			}

			double m = (double)mantissa;
			double e = (double)exponent;
			while( m >= 1 )
			{
				m /= 2.0;
				e += 1.0;
			}

			if( negative ) m = -m;
			lua.PushNumber( m );
			lua.PushNumber( e );
			return 2;
		}

		private static int Math_Ldexp( ILuaState lua )
		{
			lua.PushNumber( lua.L_CheckNumber(1) * Math.Pow(2, lua.L_CheckNumber(2)) );
			return 1;
		}

		private static int Math_Log10( ILuaState lua )
		{
			lua.PushNumber( Math.Log10( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Log( ILuaState lua )
		{
			double x = lua.L_CheckNumber(1);
			double res;
			if( lua.IsNoneOrNil(2) )
				res = Math.Log(x);
			else
			{
				double logBase = lua.L_CheckNumber(2);
				if( logBase == 10.0 )
					res = Math.Log10(x);
				else
					res = Math.Log(x, logBase);
			}
			lua.PushNumber(res);
			return 1;
		}

		private static int Math_Max( ILuaState lua )
		{
			int n = lua.GetTop();
			double dmax = lua.L_CheckNumber(1);
			for( int i=2; i<=n; ++i )
			{
				double d = lua.L_CheckNumber(i);
				if( d > dmax )
					dmax = d;
			}
			lua.PushNumber(dmax);
			return 1;
		}

		private static int Math_Min( ILuaState lua )
		{
			int n = lua.GetTop();
			double dmin = lua.L_CheckNumber(1);
			for( int i=2; i<=n; ++i )
			{
				double d = lua.L_CheckNumber(i);
				if( d < dmin )
					dmin = d;
			}
			lua.PushNumber(dmin);
			return 1;
		}

		private static int Math_Modf( ILuaState lua )
		{
			double d = lua.L_CheckNumber(1);
			double c = Math.Ceiling(d);
			lua.PushNumber( c );
			lua.PushNumber( d-c );
			return 2;
		}

		private static int Math_Pow( ILuaState lua )
		{
			lua.PushNumber( Math.Pow( lua.L_CheckNumber(1),
									  lua.L_CheckNumber(2)));
			return 1;
		}

		private static int Math_Rad( ILuaState lua )
		{
			lua.PushNumber( lua.L_CheckNumber(1) * RADIANS_PER_DEGREE );
			return 1;
		}

		private static int Math_Random( ILuaState lua )
		{
			double r = RandObj.NextDouble();
			switch( lua.GetTop() )
			{
				case 0: // no argument
					lua.PushNumber( r );
					break;
				case 1:
				{
					double u = lua.L_CheckNumber(1);
					lua.L_ArgCheck( 1.0 <= u, 1, "interval is empty" );
					lua.PushNumber( Math.Floor(r*u) + 1.0 ); // int in [1, u]
					break;
				}
				case 2:
				{
					double l = lua.L_CheckNumber(1);
					double u = lua.L_CheckNumber(2);
					lua.L_ArgCheck( l <= u, 2, "interval is empty" );
					lua.PushNumber( Math.Floor(r*(u-l+1)) + l ); // int in [l, u]
					break;
				}
				default: return lua.L_Error( "wrong number of arguments" );
			}
			return 1;
		}

		private static int Math_RandomSeed( ILuaState lua )
		{
			RandObj = new Random( (int)lua.L_CheckUnsigned(1) );
			RandObj.Next();
			return 0;
		}

		private static int Math_Sinh( ILuaState lua )
		{
			lua.PushNumber( Math.Sinh( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Sin( ILuaState lua )
		{
			lua.PushNumber( Math.Sin( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Sqrt( ILuaState lua )
		{
			lua.PushNumber( Math.Sqrt( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Tanh( ILuaState lua )
		{
			lua.PushNumber( Math.Tanh( lua.L_CheckNumber(1) ) );
			return 1;
		}

		private static int Math_Tan( ILuaState lua )
		{
			lua.PushNumber( Math.Tan( lua.L_CheckNumber(1) ) );
			return 1;
		}

	}

}


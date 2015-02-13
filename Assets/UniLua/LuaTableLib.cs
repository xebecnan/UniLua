
#define LUA_COMPAT_UNPACK

namespace UniLua
{
	using StringBuilder = System.Text.StringBuilder;

	internal class LuaTableLib
	{
		public const string LIB_NAME = "table";

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "concat", 	TBL_Concat 	),
				new NameFuncPair( "maxn", 		TBL_MaxN 	),
				new NameFuncPair( "insert", 	TBL_Insert 	),
				new NameFuncPair( "pack", 		TBL_Pack 	),
				new NameFuncPair( "unpack", 	TBL_Unpack 	),
				new NameFuncPair( "remove", 	TBL_Remove 	),
				new NameFuncPair( "sort", 		TBL_Sort 	),
			};

			lua.L_NewLib( define );

#if LUA_COMPAT_UNPACK
			// _G.unpack = table.unpack
			lua.GetField( -1, "unpack" );
			lua.SetGlobal( "unpack" );
#endif

			return 1;
		}

		private static int TBL_Concat( ILuaState lua )
		{
			string sep = lua.L_OptString( 2, "" );
			lua.L_CheckType( 1, LuaType.LUA_TTABLE );
			int i = lua.L_OptInt( 3, 1 );
			int last = lua.L_Opt( lua.L_CheckInteger, 4, lua.L_Len(1) );

			StringBuilder sb = new StringBuilder();
			for( ; i<last; ++i )
			{
				lua.RawGetI( 1, i );
				if( !lua.IsString(-1) )
					lua.L_Error(
						"invalid value ({0}) at index {1} in table for 'concat'",
						lua.L_TypeName(-1), i );
				sb.Append( lua.ToString(-1) );
				sb.Append( sep );
				lua.Pop( 1 );
			}
			if( i == last ) // add last value (if interval was not empty)
			{
				lua.RawGetI( 1, i );
				if( !lua.IsString(-1) )
					lua.L_Error(
						"invalid value ({0}) at index {1} in table for 'concat'",
						lua.L_TypeName(-1), i );
				sb.Append( lua.ToString(-1) );
				lua.Pop( 1 );
			}
			lua.PushString( sb.ToString() );
			return 1;
		}

		private static int TBL_MaxN( ILuaState lua )
		{
			double max = 0.0;
			lua.L_CheckType( 1, LuaType.LUA_TTABLE );

			lua.PushNil(); // first key
			while( lua.Next(1) )
			{
				lua.Pop( 1 ); // remove value
				if( lua.Type( -1 ) == LuaType.LUA_TNUMBER ) {
					double v = lua.ToNumber( -1 );
					if( v > max ) max = v;
				}
			}
			lua.PushNumber( max );
			return 1;
		}

		private static int AuxGetN( ILuaState lua, int n )
		{
			lua.L_CheckType( n, LuaType.LUA_TTABLE );
			return lua.L_Len( n );
		}

		private static int TBL_Insert( ILuaState lua )
		{
			int e = AuxGetN(lua, 1) + 1; // first empty element
			int pos; // where to insert new element
			switch( lua.GetTop() )
			{
				case 2: // called with only 2 arguments
				{
					pos = e; // insert new element at the end
					break;
				}
				case 3:
				{
					pos = lua.L_CheckInteger(2); // 2nd argument is the position
					if( pos > e ) e = pos; // `grow' array if necessary
					for( int i=e; i>pos; --i ) // move up elements
					{
						lua.RawGetI( 1, i-1 );
						lua.RawSetI( 1, i ); // t[i] = t[i-1]
					}
					break;
				}
				default:
				{
					return lua.L_Error( "wrong number of arguments to 'insert'" );
				}
			}
			lua.RawSetI( 1, pos ); // t[pos] = v
			return 0;
		}

		private static int TBL_Remove( ILuaState lua )
		{
			int e = AuxGetN(lua, 1);
			int pos = lua.L_OptInt( 2, e );
			if( !(1 <= pos && pos <= e) ) // position is outside bounds?
				return 0; // nothing to remove
			lua.RawGetI(1, pos); /* result = t[pos] */
			for( ; pos<e; ++pos )
			{
				lua.RawGetI( 1, pos+1 );
				lua.RawSetI( 1, pos ); // t[pos] = t[pos+1]
			}
			lua.PushNil();
			lua.RawSetI( 1, e ); // t[2] = nil
			return 1;
		}

		private static int TBL_Pack( ILuaState lua )
		{
			int n = lua.GetTop(); // number of elements to pack
			lua.CreateTable( n, 1 ); // create result table
			lua.PushInteger( n );
			lua.SetField( -2, "n" ); // t.n = number of elements
			if( n > 0 ) // at least one element?
			{
				lua.PushValue( 1 );
				lua.RawSetI( -2, 1 ); // insert first element
				lua.Replace( 1 ); // move table into index 1
				for( int i=n; i>=2; --i ) // assign other elements
					lua.RawSetI( 1, i );
			}
			return 1; // return table
		}

		private static int TBL_Unpack( ILuaState lua )
		{
			lua.L_CheckType( 1, LuaType.LUA_TTABLE );
			int i = lua.L_OptInt( 2, 1 );
			int e = lua.L_OptInt( 3, lua.L_Len(1) );
			if( i > e ) return 0; // empty range
			int n = e - i + 1; // number of elements
			if( n <= 0 || !lua.CheckStack(n) ) // n <= 0 means arith. overflow
				return lua.L_Error( "too many results to unpack" );
			lua.RawGetI( 1, i ); // push arg[i] (avoiding overflow problems
			while( i++ < e ) // push arg[i + 1...e]
				lua.RawGetI( 1, i );
			return n;
		}

		// quick sort ////////////////////////////////////////////////////////

		private static void Set2( ILuaState lua, int i, int j )
		{
			lua.RawSetI( 1, i );
			lua.RawSetI( 1, j );
		}

		private static bool SortComp( ILuaState lua, int a, int b )
		{
			if( !lua.IsNil(2) ) // function?
			{
				lua.PushValue( 2 );
				lua.PushValue( a-1 ); // -1 to compensate function
				lua.PushValue( b-2 ); // -2 to compensate function add `a'
				lua.Call( 2, 1 );
				bool res = lua.ToBoolean( -1 );
				lua.Pop( 1 );
				return res;
			}
			else /// a < b?
				return lua.Compare( a, b, LuaEq.LUA_OPLT );
		}

		private static void AuxSort( ILuaState lua, int l, int u )
		{
			while( l < u ) // for tail recursion
			{
				// sort elements a[l], a[(l+u)/2] and a[u]
				lua.RawGetI( 1, l );
				lua.RawGetI( 1, u );
				if( SortComp( lua, -1, -2 ) ) // a[u] < a[l]?
					Set2( lua, l, u );
				else
					lua.Pop( 2 );
				if( u-l == 1 ) break; // only 2 elements
				int i = (l+u) / 2;
				lua.RawGetI( 1, i );
				lua.RawGetI( 1, l );
				if( SortComp( lua, -2, -1 ) ) // a[i] < a[l]?
					Set2( lua, i, l );
				else
				{
					lua.Pop( 1 ); // remove a[l]
					lua.RawGetI( 1, u );
					if( SortComp( lua, -1, -2 ) ) // a[u] < a[i]?
						Set2( lua, i, u );
					else
						lua.Pop( 2 );
				}
				if( u-l == 2 ) break; // only 3 arguments
				lua.RawGetI( 1, i ); // Pivot
				lua.PushValue( -1 );
				lua.RawGetI( 1, u-1 );
				Set2(lua, i, u-1);
				/* a[l] <= P == a[u-1] <= a[u], only need to sort from l+1 to u-2 */
				i = l;
				int j = u-1;
				for (;;) {  /* invariant: a[l..i] <= P <= a[j..u] */
					/* repeat ++i until a[i] >= P */
					lua.RawGetI( 1, ++i );
					while( SortComp(lua, -1, -2) )
					{
						if (i>=u) lua.L_Error( "invalid order function for sorting" );
						lua.Pop(1);  /* remove a[i] */
						lua.RawGetI( 1, ++i );
					}
					/* repeat --j until a[j] <= P */
					lua.RawGetI( 1, --j );
					while ( SortComp(lua, -3, -1) ) {
						if (j<=l) lua.L_Error( "invalid order function for sorting" );
						lua.Pop(1);  /* remove a[j] */
						lua.RawGetI( 1, --j );
					}
					if (j<i) {
						lua.Pop(3);  /* pop pivot, a[i], a[j] */
						break;
					}
					Set2(lua, i, j);
				}
				lua.RawGetI( 1, u-1 );
				lua.RawGetI( 1, i );
				Set2(lua, u-1, i);  /* swap pivot (a[u-1]) with a[i] */
				/* a[l..i-1] <= a[i] == P <= a[i+1..u] */
				/* adjust so that smaller half is in [j..i] and larger one in [l..u] */
				if (i-l < u-i) {
					j=l; i=i-1; l=i+2;
				}
				else {
					j=i+1; i=u; u=j-2;
				}
				AuxSort(lua, j, i);  /* call recursively the smaller one */
			}  /* repeat the routine for the larger one */
		}

		private static int TBL_Sort( ILuaState lua )
		{
			int n = AuxGetN(lua, 1);
			lua.L_CheckStack(40, "");  /* assume array is smaller than 2^40 */
			if (!lua.IsNoneOrNil(2))  /* is there a 2nd argument? */
				lua.L_CheckType( 2, LuaType.LUA_TFUNCTION );
			lua.SetTop(2);  /* make sure there is two arguments */
			AuxSort(lua, 1, n);
			return 0;
		}

	}
}


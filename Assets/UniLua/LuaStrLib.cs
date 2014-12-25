
namespace UniLua
{
	using StringBuilder = System.Text.StringBuilder;
	using Char = System.Char;
	using Convert = System.Convert;

	internal static class LuaStrLib
	{
		public const string LIB_NAME = "string";

		private const int CAP_UNFINISHED 	= -1;
		private const int CAP_POSITION		= -2;
		private const int LUA_MAXCAPTURES 	= 32;
		private const char L_ESC 			= '%';
		private const string FLAGS			= "-+ #0";
		private static readonly char[] SPECIALS;

		static LuaStrLib()
		{
			SPECIALS = "^$*+?.([%-".ToCharArray();
		}

		public static int OpenLib( ILuaState lua )
		{
			NameFuncPair[] define = new NameFuncPair[]
			{
				new NameFuncPair( "byte", 		Str_Byte ),
				new NameFuncPair( "char", 		Str_Char ),
				new NameFuncPair( "dump", 		Str_Dump ),
				new NameFuncPair( "find", 		Str_Find ),
				new NameFuncPair( "format", 	Str_Format ),
				new NameFuncPair( "gmatch", 	Str_Gmatch ),
				new NameFuncPair( "gsub", 		Str_Gsub ),
				new NameFuncPair( "len", 		Str_Len ),
				new NameFuncPair( "lower", 		Str_Lower ),
				new NameFuncPair( "match", 		Str_Match ),
				new NameFuncPair( "rep", 		Str_Rep ),
				new NameFuncPair( "reverse", 	Str_Reverse ),
				new NameFuncPair( "sub", 		Str_Sub ),
				new NameFuncPair( "upper", 		Str_Upper ),
			};

			lua.L_NewLib( define );
			CreateMetaTable( lua );

			return 1;
		}

		private static void CreateMetaTable( ILuaState lua )
		{
			lua.CreateTable(0, 1); // table to be metatable for strings
			lua.PushString( "" ); // dummy string
			lua.PushValue( -2 ); // copy table
			lua.SetMetaTable( -2 ); // set table as metatable for strings
			lua.Pop( 1 );
			lua.PushValue( -2 ); // get string library
			lua.SetField( -2, "__index" ); // metatable.__index = string
			lua.Pop( 1 ); // pop metatable
		}

		private static int PosRelative( int pos, int len )
		{
			if( pos >= 0 ) return pos;
			else if( 0 - pos > len ) return 0;
			else return len - (-pos) + 1;
		}

		private static int Str_Byte( ILuaState lua )
		{
			string s = lua.L_CheckString(1);
			int posi = PosRelative( lua.L_OptInt(2, 1), s.Length );
			int pose = PosRelative( lua.L_OptInt(3, posi), s.Length );
			if( posi < 1 ) posi = 1;
			if( pose > s.Length ) pose = s.Length;
			if( posi > pose ) return 0; // empty interval; return no values
			int n = pose - posi + 1;
			if( posi + n <= pose) // overflow?
				return lua.L_Error( "string slice too long" );
			lua.L_CheckStack(n, "string slice too long");
			for( int i=0; i<n; ++i )
				lua.PushInteger( (byte)s[(int)posi+i-1] );
			return n;
		}

		private static int Str_Char( ILuaState lua )
		{
			int n = lua.GetTop();
			StringBuilder sb = new StringBuilder();
			for( int i=1; i<=n; ++i )
			{
				int c = lua.L_CheckInteger(i);
				lua.L_ArgCheck( (char)c == c, i, "value out of range" );
				sb.Append( (char)c );
			}
			lua.PushString( sb.ToString() );
			return 1;
		}

		private static int Str_Dump( ILuaState lua )
		{
			lua.L_CheckType( 1, LuaType.LUA_TFUNCTION );
			lua.SetTop( 1 );
			var bsb = new ByteStringBuilder();
			LuaWriter writeFunc = 
				delegate(byte[] bytes, int start, int length)
			{
				bsb.Append(bytes, start, length);
				return DumpStatus.OK;
			};
			if( lua.Dump( writeFunc ) != DumpStatus.OK )
				return lua.L_Error( "unable to dump given function" );
			lua.PushString( bsb.ToString() );
			return 1;
		}

		class CaptureInfo
		{
			public int Len;
			public int Init;
		}

		class MatchState
		{
			public ILuaState 		Lua;
			public int				Level;
			public string			Src;
			public int				SrcInit;
			public int				SrcEnd;
			public string			Pattern;
			public int				PatternEnd;
			public CaptureInfo[]	Capture;

			public MatchState()
			{
				Capture = new CaptureInfo[LUA_MAXCAPTURES];
				for(int i =0; i < LUA_MAXCAPTURES; ++i)
					Capture[i] = new CaptureInfo();
			}
		}

		private static int ClassEnd( MatchState ms, int p )
		{
			var lua = ms.Lua;
			switch( ms.Pattern[p++] )
			{
				case L_ESC:
				{
					if( p == ms.PatternEnd )
						lua.L_Error( "malformed pattern (ends with '%')" );
					return p+1;
				}
				case '[':
				{
					if( ms.Pattern[p] == '^' ) p++;
					do {
						if( p == ms.PatternEnd )
							lua.L_Error( "malformed pattern (missing ']')" );
						if( ms.Pattern[p++] == L_ESC && p < ms.PatternEnd )
							p++; // skip escapes (e.g. `%]')
					} while( ms.Pattern[p] != ']' );
					return p+1;
				}
				default: return p;
			}
		}

		private static bool IsXDigit( char c )
		{
			switch(c) {
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
					return true;
				default:
					return false;
			}
		}

		private static bool MatchClass( char c, char cl )
		{
			bool res;
			switch(cl)
			{
				case 'a': res = Char.IsLetter(c); break;
				case 'c': res = Char.IsControl(c); break;
				case 'd': res = Char.IsDigit(c); break;
				case 'g': throw new System.NotImplementedException();
				case 'l': res = Char.IsLower(c); break;
				case 'p': res = Char.IsPunctuation(c); break;
				case 's': res = Char.IsWhiteSpace(c); break;
				case 'u': res = Char.IsUpper(c); break;
				case 'w': res = Char.IsLetterOrDigit(c); break;
				case 'x': res = IsXDigit(c); break;
				case 'z': res = (c == '\0'); break;  /* deprecated option */
				default: return (cl == c);
			}
			return res;
		}

		private static bool MatchBreaketClass( MatchState ms, char c, int p, int ec )
		{
			bool sig = true;
			if( ms.Pattern[p+1] == '^' )
			{
				sig = false;
				p++; // skip the `^'
			}
			while( ++p < ec )
			{
				if( ms.Pattern[p] == L_ESC )
				{
					p++;
					if( MatchClass( c, ms.Pattern[p] ) )
						return sig;
				}
				else if( ms.Pattern[p+1] == '-' && (p+2 < ec) )
				{
					p += 2;
					if( ms.Pattern[p-2] <= c && c <= ms.Pattern[p] )
						return sig;
				}
				else if( ms.Pattern[p] == c ) return sig;
			}
			return !sig;
		}

		private static bool SingleMatch( MatchState ms, char c, int p, int ep )
		{
			switch( ms.Pattern[p] )
			{
				case '.': 	return true; // matches any char
				case L_ESC: return MatchClass( c, ms.Pattern[p+1] );
				case '[': 	return MatchBreaketClass( ms, c, p, ep-1 );
				default: 	return ms.Pattern[p] == c;
			}
		}

		private static int MatchBalance( MatchState ms, int s, int p )
		{
			var lua = ms.Lua;
			if( p >= ms.PatternEnd - 1 )
				lua.L_Error( "malformed pattern (missing arguments to '%b')" );
			if( ms.Src[s] != ms.Pattern[p] ) return -1;
			else
			{
				char b = ms.Pattern[p];
				char e = ms.Pattern[p+1];
				int count = 1;
				while( ++s < ms.SrcEnd )
				{
					if( ms.Src[s] == e )
					{
						if( --count == 0 ) return s+1;
					}
					else if( ms.Src[s] == b ) count++;
				}
			}
			return -1; //string ends out of balance
		}

		private static int MaxExpand( MatchState ms, int s, int p, int ep )
		{
			int i = 0; // counts maximum expand for item
			while( (s+i) < ms.SrcEnd && SingleMatch( ms, ms.Src[s+i], p, ep ) )
				i++;
			// keeps trying to match with the maximum repetitions
			while( i >= 0 )
			{
				int res = Match( ms, (s+i), (ep+1) );
				if( res >= 0 ) return res;
				i--; // else didn't match; reduce 1 repetition to try again
			}
			return -1;
		}

		private static int MinExpand( MatchState ms, int s, int p, int ep )
		{
			for(;;)
			{
				int res = Match( ms, s, ep+1 );
				if( res >= 0 )
					return res;
				else if( s < ms.SrcEnd && SingleMatch( ms, ms.Src[s], p, ep ) )
					s++; // try with one more repetition
				else return -1;
			}
		}

		private static int CaptureToClose( MatchState ms )
		{
			var lua = ms.Lua;
			int level=ms.Level;
			for( level--; level>=0; level-- )
			{
				if( ms.Capture[level].Len == CAP_UNFINISHED )
					return level;
			}
			return lua.L_Error( "invalid pattern capture" );
		}

		private static int StartCapture( MatchState ms, int s, int p, int what )
		{
			var lua = ms.Lua;
			int level = ms.Level;
			if( level >= LUA_MAXCAPTURES )
				lua.L_Error( "too many captures" );
			ms.Capture[level].Init = s;
			ms.Capture[level].Len = what;
			ms.Level = level + 1;
			int res = Match( ms, s, p );
			if( res == -1 ) // match failed?
				ms.Level--;
			return res;
		}

		private static int EndCapture( MatchState ms, int s, int p )
		{
			int l = CaptureToClose( ms );
			ms.Capture[l].Len = s - ms.Capture[l].Init; // close capture
			int res = Match( ms, s, p );
			if( res == -1 ) // match failed?
				ms.Capture[l].Len = CAP_UNFINISHED; // undo capture
			return res;
		}

		private static int CheckCapture( MatchState ms, char l )
		{
			var lua = ms.Lua;
			int i = (int)(l - '1');
			if( i < 0 || i >= ms.Level || ms.Capture[i].Len == CAP_UNFINISHED )
				return lua.L_Error( "invalid capture index %d", i+1 );
			return i;
		}

		private static int MatchCapture( MatchState ms, int s, char l )
		{
			int i = CheckCapture( ms, l );
			int len = ms.Capture[i].Len;
			if( ms.SrcEnd - s >= len &&
				string.Compare(ms.Src, ms.Capture[i].Init, ms.Src, s, len) == 0 )
				return s + len;
			else
				return -1;
		}

		private static int Match( MatchState ms, int s, int p )
		{
			var lua = ms.Lua;
			init: // using goto's to optimize tail recursion
			if( p == ms.PatternEnd )
				return s;
			switch( ms.Pattern[p] )
			{
				case '(': // start capture
				{
					if( ms.Pattern[p+1] == ')' ) // position capture?
						return StartCapture( ms, s, p+2, CAP_POSITION );
					else
						return StartCapture( ms, s, p+1, CAP_UNFINISHED );
				}
				case ')': // end capture
				{
					return EndCapture( ms, s, p+1 );
				}
				case '$':
				{
					if( p+1 == ms.PatternEnd ) // is the `$' the last char in pattern?
						return (s == ms.SrcEnd) ? s : -1; // check end of string
					else goto default;
				}
				case L_ESC: // escaped sequences not in the format class[*+?-]?
				{
					switch( ms.Pattern[p+1] )
					{
						case 'b': // balanced string?
						{
							s = MatchBalance( ms, s, p+2 );
							if( s == -1 ) return -1;
							p += 4; goto init; // else return match(ms, s, p+4);
						}
						case 'f': // frontier?
						{
							p += 2;
							if( ms.Pattern[p] != '[' )
								lua.L_Error( "missing '[' after '%f' in pattern" );
							int ep = ClassEnd( ms, p ); //points to what is next
							char previous = (s == ms.SrcInit) ? '\0' : ms.Src[s-1];
							if( MatchBreaketClass(ms, previous, p, ep-1) ||
								!MatchBreaketClass(ms, ms.Src[s], p, ep-1) ) return -1;
							p = ep; goto init; // else return match( ms, s, ep );
						}
						case '0':
						case '1':
						case '2':
						case '3':
						case '4':
						case '5':
						case '6':
						case '7':
						case '8':
						case '9': // capture results (%0-%9)?
						{
							s = MatchCapture( ms, s, ms.Pattern[p+1] );
							if( s == -1 ) return -1;
							p+=2; goto init; // else return match(ms, s, p+2);
						}
						default: goto dflt;
					}
				}
				default: dflt: // pattern class plus optional suffix
				{
					int ep = ClassEnd( ms, p );
					bool m = s < ms.SrcEnd && SingleMatch(ms, ms.Src[s], p, ep);
				    if(ep < ms.PatternEnd){
						switch(ms.Pattern[ep]) //fix gmatch bug patten is [^a]
						{
							case '?': // optional
							{
								if( m )
								{
									int res = Match(ms, s+1, ep+1);
									if( res != -1 )
										return res;
								}
								p=ep+1; goto init; // else return match(ms, s, ep+1);
							}
							case '*': // 0 or more repetitions
							{
								return MaxExpand(ms, s, p, ep);
							}
							case '+': // 1 or more repetitions
							{
								return (m ? MaxExpand(ms, s+1, p, ep) : -1);
							}
							case '-': // 0 or more repetitions (minimum)
							{
								return MinExpand(ms, s, p, ep);
							}
						}
				    }
				    if(!m) return -1;
					s++; p=ep; goto init; // else return match(ms, s+1, ep);
				}
			}
		}

		private static void PushOneCapture
			( MatchState ms
			, int i
			, int start
			, int end
			)
		{
			var lua = ms.Lua;
			if( i >= ms.Level )
			{
				if( i == 0 ) // ms.Level == 0, too
					lua.PushString( ms.Src.Substring( start, end-start ) );
				else
					lua.L_Error( "invalid capture index" );
			}
			else
			{
				int l = ms.Capture[i].Len;
				if( l == CAP_UNFINISHED )
					lua.L_Error( "unfinished capture" );
				if( l == CAP_POSITION )
					lua.PushInteger( ms.Capture[i].Init - ms.SrcInit + 1 );
				else
					lua.PushString( ms.Src.Substring( ms.Capture[i].Init, l ) );
			}
		}

		private static int PushCaptures(ILuaState lua, MatchState ms, int spos, int epos )
		{
			int nLevels = (ms.Level == 0 && spos >= 0) ? 1 : ms.Level;
			lua.L_CheckStack(nLevels, "too many captures");
			for( int i=0; i<nLevels; ++i )
				PushOneCapture( ms, i, spos, epos );
			return nLevels; // number of strings pushed
		}

		private static bool NoSpecials( string pattern )
		{
			return pattern.IndexOfAny( SPECIALS ) == -1;
		}

		private static int StrFindAux( ILuaState lua, bool find )
		{
			string s = lua.L_CheckString( 1 );
			string p = lua.L_CheckString( 2 );
			int init = PosRelative( lua.L_OptInt(3, 1), s.Length );
			if( init < 1 ) init = 1;
			else if( init > s.Length + 1 ) // start after string's end?
			{
				lua.PushNil(); // cannot find anything
				return 1;
			}
			// explicit request or no special characters?
			if( find && (lua.ToBoolean(4) || NoSpecials(p)) )
			{
				// do a plain search
				int pos = s.IndexOf( p, init-1 );
				if( pos >= 0 )
				{
					lua.PushInteger( pos+1 );
					lua.PushInteger( pos+p.Length );
					return 2;
				}
			}
			else
			{
				int s1 = init-1;
				int ppos = 0;
				bool anchor = p[ppos] == '^';
				if( anchor )
					ppos++; // skip anchor character

				MatchState ms = new MatchState();
				ms.Lua = lua;
				ms.Src = s;
				ms.SrcInit = s1;
				ms.SrcEnd = s.Length;
				ms.Pattern = p;
				ms.PatternEnd = p.Length;

				do
				{
					ms.Level = 0;
					int res = Match( ms, s1, ppos );
					if( res != -1 )
					{
						if(find)
						{
							lua.PushInteger( s1+1 ); // start
							lua.PushInteger( res );  // end
							return PushCaptures(lua, ms, -1, 0) + 2;
						}
						else return PushCaptures(lua, ms, s1, res);
					}
				} while( s1++ < ms.SrcEnd && !anchor );
			}
			lua.PushNil(); // not found
			return 1;
		}

		private static int Str_Find( ILuaState lua )
		{
			return StrFindAux( lua, true );
		}

		private static int ScanFormat( ILuaState lua, string format, int s, out string form )
		{
			int p = s;
			// skip flags
			while( p < format.Length && format[p] != '\0' && FLAGS.IndexOf(format[p]) != -1 )
				p++;
			if( p - s > FLAGS.Length )
				lua.L_Error( "invalid format (repeat flags)" );
			if( Char.IsDigit( format[p] ) ) p++; // skip width
			if( Char.IsDigit( format[p] ) ) p++; // (2 digits at most)
			if( format[p] == '.' )
			{
				p++;
				if( Char.IsDigit( format[p] ) ) p++; // skip precision
				if( Char.IsDigit( format[p] ) ) p++; // (2 digits at most)
			}
			if( Char.IsDigit( format[p] ) )
				lua.L_Error( "invalid format (width of precision too long)" );
			form = "%" + format.Substring( s, (p-s+1) );
			return p;
		}

		private static int Str_Format( ILuaState lua )
		{
			int top = lua.GetTop();
			StringBuilder sb = new StringBuilder();
			int arg = 1;
			string format = lua.L_CheckString( arg );
			int s = 0;
			int e = format.Length;
			while(s < e)
			{
				if( format[s] != L_ESC )
				{
					sb.Append( format[s++] );
					continue;
				}

				if( format[++s] == L_ESC )
				{
					sb.Append( format[s++] );
					continue;
				}

				// else format item
				if( ++arg > top )
					lua.L_ArgError( arg, "no value" );

				string form;
				s = ScanFormat( lua, format, s, out form );
				switch( format[s++] ) // TODO: properly handle form
				{
					case 'c':
					{
						sb.Append( (char)lua.L_CheckInteger(arg) );
						break;
					}
					case 'd': case 'i':
					{
						int n = lua.L_CheckInteger(arg);
						sb.Append( n.ToString() );
						break;
					}
					case 'u':
					{
						int n = lua.L_CheckInteger(arg);
						lua.L_ArgCheck( n >= 0, arg,
							"not a non-negative number is proper range" );
						sb.Append( n.ToString() );
						break;
					}
					case 'o':
					{
						int n = lua.L_CheckInteger(arg);
						lua.L_ArgCheck( n >= 0, arg,
							"not a non-negative number is proper range" );
						sb.Append( Convert.ToString(n, 8) );
						break;
					}
					case 'x':
					{
						int n = lua.L_CheckInteger(arg);
						lua.L_ArgCheck( n >= 0, arg,
							"not a non-negative number is proper range" );
						// sb.Append( string.Format("{0:x}", n) );
						sb.AppendFormat("{0:x}", n);
						break;
					}
					case 'X':
					{
						int n = lua.L_CheckInteger(arg);
						lua.L_ArgCheck( n >= 0, arg,
							"not a non-negative number is proper range" );
						// sb.Append( string.Format("{0:X}", n) );
						sb.AppendFormat("{0:X}", n);
						break;
					}
					case 'e':  case 'E':
					{
						sb.AppendFormat("{0:E}", lua.L_CheckNumber(arg));
						break;
					}
					case 'f':
					{
						sb.AppendFormat("{0:F}", lua.L_CheckNumber(arg));
						break;
					}
#if LUA_USE_AFORMAT
					case 'a': case 'A':
#endif
					case 'g': case 'G':
					{
						sb.AppendFormat("{0:G}", lua.L_CheckNumber(arg));
						break;
					}
					case 'q':
					{
						AddQuoted(lua, sb, arg);
						break;
					}
					case 's':
					{
						sb.Append(lua.L_CheckString(arg));
						break;
					}
					default: // also treat cases `pnLlh'
					{
						return lua.L_Error( "invalid option '{0}' to 'format'",
							format[s-1] );
					}
				}
			}
			lua.PushString( sb.ToString() );
			return 1;
		}

		private static void AddQuoted(ILuaState lua, StringBuilder sb, int arg)
		{
			var s = lua.L_CheckString(arg);
			sb.Append('"');
			for(var i=0; i<s.Length; ++i) {
				var c = s[i];
				if(c == '"' || c == '\\' || c == '\n') {
					sb.Append('\\').Append(c);
				}
				else if(c == '\0' || Char.IsControl(c)) {
					if(i+1>=s.Length || !Char.IsDigit(s[i+1])) {
						sb.AppendFormat("\\{0:D}", (int)c);
					}
					else {
						sb.AppendFormat("\\{0:D3}", (int)c);
					}
				}
				else {
					sb.Append(c);
				}
			}
			sb.Append('"');
		}

		private static int GmatchAux( ILuaState lua )
		{
			MatchState ms = new MatchState();
			string src = lua.ToString( lua.UpvalueIndex(1) );
			string pattern = lua.ToString( lua.UpvalueIndex(2) );
			ms.Lua = lua;
			ms.Src = src;
			ms.SrcInit = 0;
			ms.SrcEnd = src.Length;
			ms.Pattern = pattern;
			ms.PatternEnd = pattern.Length;
			for( int s = lua.ToInteger( lua.UpvalueIndex(3) )
			   ; s <= ms.SrcEnd
			   ; s++ )
			{
				ms.Level = 0;
				int e = Match( ms, s, 0 );
				if( e != -1 )
				{
					int newStart = (e == 0) ? e+1: e;
					lua.PushInteger( newStart );
					lua.Replace( lua.UpvalueIndex(3) );
					return PushCaptures(lua, ms, s, e);
				}
			}
			return 0; // not found
		}

		private static int Str_Gmatch( ILuaState lua )
		{
			lua.L_CheckString(1);
			lua.L_CheckString(2);
			lua.SetTop(2);
			lua.PushInteger(0);
			lua.PushCSharpClosure( GmatchAux, 3 );
			return 1;
		}
		
		private static void Add_S (MatchState ms, StringBuilder b, int s, int e) {
		  string news = ms.Lua.ToString(3);
		  for (int i = 0; i < news.Length; i++) {
			if (news[i] != L_ESC)
			  b.Append(news[i]);
			else {
			  i++;  /* skip ESC */
			  if (!Char.IsDigit((news[i])))
			      b.Append(news[i]);
			  else if (news[i] == '0')
				  b.Append(ms.Src.Substring(s, (e - s))); 
			  else {
				PushOneCapture(ms, news[i] - '1', s, e);
				b.Append(ms.Lua.ToString(-1));  /* add capture to accumulated result */
			  }
			}
		  }
		}
		
		private static void Add_Value (MatchState ms, StringBuilder b, int s, int e) {
		  ILuaState lua = ms.Lua;
		  switch (lua.Type(3)) {
			case LuaType.LUA_TNUMBER:
			case LuaType.LUA_TSTRING: {
			  Add_S(ms, b, s, e);
			  return;
			}
			case LuaType.LUA_TFUNCTION: {
			  int n;
			  lua.PushValue(3);
			  n = PushCaptures(lua, ms, s, e);
			  lua.Call(n, 1);
			  break;
			}
			case LuaType.LUA_TTABLE: {
			  PushOneCapture(ms, 0, s, e);
			  lua.GetTable(3);
			  break;
			}
		  }
		  if (lua.ToBoolean(-1)==false) {  /* nil or false? */
			lua.Pop(1);
			b.Append(ms.Src.Substring(s, (e - s)));  /* keep original text */
		  }
		  else if (!lua.IsString(-1))
			lua.L_Error("invalid replacement value (a %s)", lua.L_TypeName(-1));
	      else
			b.Append(lua.ToString(-1));
		}

		private static int Str_Gsub( ILuaState lua )
		{
			
			string src = lua.L_CheckString(1);
			int srcl = src.Length;
			string p = lua.L_CheckString(2);
			LuaType tr = lua.Type(3);
			int max_s = lua.L_OptInt(4, srcl + 1);
			int anchor = 0;
			if (p[0] == '^')
			{
                p = p.Substring(1);
                anchor = 1;
			}
			int n = 0;
			MatchState ms = new MatchState();
			StringBuilder b = new StringBuilder(srcl);
			lua.L_ArgCheck(tr == LuaType.LUA_TNUMBER || tr == LuaType.LUA_TSTRING ||
						   tr == LuaType.LUA_TFUNCTION || tr == LuaType.LUA_TTABLE, 3,
							  "string/function/table expected");
			ms.Lua = lua;
			ms.Src = src;
			ms.SrcInit = 0;
			ms.SrcEnd = srcl;
			ms.Pattern = p;
			ms.PatternEnd = p.Length;
			int s = 0;
			while (n < max_s) {
			   ms.Level = 0;
			   int e = Match(ms, s, 0);
			   if (e != -1) {
			       n++;
				   Add_Value(ms, b, s, e);
			   }
			   if ((e != -1) && e > s) /* non empty match? */
			      s = e;  /* skip it */
			   else if (s < ms.SrcEnd)
			   {
			       char c = src[s];
				   ++s;
			       b.Append(c);
			   }
			   else break;
			   if (anchor != 0) break;
		    }
			b.Append(src.Substring(s, ms.SrcEnd - s));
			lua.PushString(b.ToString());
		    lua.PushInteger(n);  /* number of substitutions */
		    return 2;
		}

		private static int Str_Len( ILuaState lua )
		{
			string s = lua.L_CheckString(1);
			lua.PushInteger( s.Length );
			return 1;
		}

		private static int Str_Lower( ILuaState lua )
		{
			string s = lua.L_CheckString(1);
			lua.PushString( s.ToLower() );
			return 1;
		}

		private static int Str_Match( ILuaState lua )
		{
			return StrFindAux( lua, false );
		}

		private static int Str_Rep( ILuaState lua )
		{
			// TODO
			throw new System.NotImplementedException();
		}

		private static int Str_Reverse( ILuaState lua )
		{
			string s = lua.L_CheckString(1);
			StringBuilder sb = new StringBuilder(s.Length);
			for( int i=s.Length-1; i>=0; --i )
				sb.Append( s[i] );
			lua.PushString( sb.ToString() );
			return 1;
		}

		private static int Str_Sub( ILuaState lua )
		{
			string s = lua.L_CheckString(1);
			int start = PosRelative( lua.L_CheckInteger(2), s.Length );
			int end = PosRelative( lua.L_OptInt(3, -1), s.Length );
			if( start < 1 ) start = 1;
			if( end > s.Length ) end = s.Length;
			if( start <= end )
				lua.PushString( s.Substring(start-1, end-start+1) );
			else
				lua.PushString( "" );
			return 1;
		}

		private static int Str_Upper( ILuaState lua )
		{
			string s = lua.L_CheckString(1);
			lua.PushString( s.ToUpper() );
			return 1;
		}

	}

}


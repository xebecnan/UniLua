
using System;
using System.IO;
using StringBuilder = System.Text.StringBuilder;

namespace UniLua.Tools
{
	public class BytesLoadInfo : ILoadInfo
	{
		private byte[] 	Bytes;
		private int		Pos;

		public BytesLoadInfo( byte[] bytes )
		{
			Bytes = bytes;
			Pos = 0;
		}

		public int ReadByte()
		{
			if( Pos >= Bytes.Length )
				return -1;
			else
				return Bytes[Pos++];
		}

		public int PeekByte()
		{
			if( Pos >= Bytes.Length )
				return -1;
			else
				return Bytes[Pos];
		}
	}

	public static class Compiler
	{
		static void Fatal( string msg )
		{
			throw new Exception( msg );
		}

		public static LuaProto CompileFile( string filename )
		{
			// var lua = LuaAPI.NewState();
			// var reader = new BinaryReader( File.Open(filename, FileMode.Open) );
			// var p = new Parser( lua );
			// return p.Parse( reader, "@" + filename );

			var lua = LuaAPI.NewState();
			var status = lua.L_LoadFileX( filename, null );
			if( status != ThreadStatus.LUA_OK )
			{
				Fatal( lua.ToString( -1 ) );
			}
			var cl = ((LuaState)lua).Top.V.ClLValue();
			return cl.Proto;
		}

		public static void ListingToFile( LuaProto proto, string filename )
		{
			using( var writer = new StreamWriter( filename ) )
			{
				_ListFunc( proto, (output) => {
					// Debug.Log( output );
					writer.Write( output );
				});
			}
		}

		public static void ListingToFile( string inFilename, string outFilename )
		{
			ListingToFile( CompileFile(inFilename), outFilename );
		}

		public static void DumpingToFile( LuaProto proto, string filename, bool strip )
		{
			using( var writer = new BinaryWriter( File.Open(
				filename, FileMode.Create ) ) )
			{
				LuaWriter writeFunc =
				delegate(byte[] bytes, int start, int length)
				{
					try
					{
						writer.Write( bytes, start, length );
						return DumpStatus.OK;
					}
					catch( Exception )
					{
						return DumpStatus.ERROR;
					}
				};
				DumpState.Dump( proto, writeFunc, strip );
			}
		}

		public static void DumpingToFile( string inFilename, string outFilename, bool strip )
		{
			DumpingToFile( CompileFile(inFilename), outFilename, strip );
		}

		// private static string _GetSource( string filename )
		// {
		// 	using( var reader = new StreamReader( filename ) )
		// 	{
		// 		return reader.ReadToEnd();
		// 	}
		// }

		private delegate void ListFuncDelegate( string output );

		private static void _ListFunc( LuaProto p, ListFuncDelegate outputEvent )
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();

			string s = (p.Source != null) ? p.Source : "=?";
			if( s[0] == '@' || s[0] == '=' )
				s = s.Substring(1);
			else if( (int)s[0] == 27 )
				s = "(bstring)";
			else
				s = "(string)";
			sb.Append( string.Format("{0} <{1}:{2},{3}> ({4} instructions)",
				p.LineDefined==0 ? "main" : "function",
				s,
				p.LineDefined,
				p.LastLineDefined,
				p.Code.Count) ).Append("\n");
			sb.Append( string.Format(
				"{0}{1} params, {2} slots, {3} upvalue, {4} locals, {5} constants, {6} functions",
				p.NumParams,
				p.IsVarArg ? "+" : "",
				p.MaxStackSize,
				p.Upvalues.Count,
				p.LocVars.Count,
				p.K.Count,
				p.P.Count ) ).Append("\n");
			for( int i=0; i<p.Code.Count; ++i )
			{
				var ins = p.Code[i];
				var line = p.LineInfo[i];
				sb.Append( (i+1).ToString() ).Append( "\t" )
				  .Append( "["+line+"]" ).Append( "\t" )
				  .Append( ins.ToString() ).Append( "\t" )
				  .Append( "; " ).Append(line).Append("\n");
			}
			if( outputEvent != null )
				outputEvent( sb.ToString() );

			foreach( var child in p.P )
			{
				_ListFunc( child, outputEvent );
			}
		}

	}
}


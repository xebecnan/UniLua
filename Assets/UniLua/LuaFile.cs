﻿using System;
using System.IO;
using System.Collections.Generic;

namespace UniLua
{
	internal class LuaFile
	{
		public static FileLoadInfo OpenFile( string filePath )
		{
            return new FileLoadInfo(File.Open(filePath, FileMode.Open));
		}

        public static bool Readable(string filePath)
		{
			try {
                using (var stream = File.Open(filePath, FileMode.Open))
                {
					return true;
				}
			}
			catch( Exception ) {
				return false;
			}
		}
	}

	internal class FileLoadInfo : ILoadInfo, IDisposable
	{
		public FileLoadInfo( FileStream stream )
		{
			Stream = stream;
			Buf = new Queue<byte>();
		}

		public int ReadByte()
		{
			if( Buf.Count > 0 )
				return (int)Buf.Dequeue();
			else
				return Stream.ReadByte();
		}

		public int PeekByte()
		{
			if( Buf.Count > 0 )
				return (int)Buf.Peek();
			else
			{
				var c = Stream.ReadByte();
				if( c == -1 )
					return c;
				Save( (byte)c );
				return c;
			}
		}

		public void Dispose()
		{
			Stream.Dispose();
		}

		private const string UTF8_BOM = "\u00EF\u00BB\u00BF";
		private FileStream 	Stream;
		private Queue<byte>	Buf;

		private void Save( byte b )
		{
			Buf.Enqueue( b );
		}

		private void Clear()
		{
			Buf.Clear();
		}

		private int SkipBOM()
		{
			foreach( var b in UTF8_BOM )
			{
				var c = Stream.ReadByte();
				if(c == -1 || c != b)
					return c;
				Save( (byte)c );
			}
			// perfix matched; discard it
			Clear();
			return Stream.ReadByte();
		}

		public void SkipComment()
		{
			var c = SkipBOM();

			// first line is a comment (Unix exec. file)?
			if( c == '#' )
			{
				do {
					c = Stream.ReadByte();
				} while( c != -1 && c != '\n' );
				Save( (byte)'\n' ); // fix line number
			}
			else if( c != -1 )
			{
				Save( (byte)c );
			}
		}
	}

}


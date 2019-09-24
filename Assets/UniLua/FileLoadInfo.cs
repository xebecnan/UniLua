using System;
using System.Collections.Generic;
using System.IO;

namespace UniLua {
  public class FileLoadInfo : ILoadInfo, IDisposable {
    public FileLoadInfo(FileStream stream) {
      Stream = stream;
      Reader = new StreamReader(Stream, System.Text.Encoding.UTF8);
      Buf = new Queue<char>();
    }

    public int ReadByte() {
      if (Buf.Count > 0)
        return (int) Buf.Dequeue();
      else
        return Reader.Read();
    }

    public int PeekByte() {
      if (Buf.Count > 0)
        return (int) Buf.Peek();
      else {
        var c = Reader.Read();
        if (c == -1)
          return c;
        Save((char) c);
        return c;
      }
    }

    public void Dispose() {
      Reader.Dispose();
      Stream.Dispose();
    }

    private const string UTF8_BOM = "\u00EF\u00BB\u00BF";
    private FileStream Stream;
    private StreamReader Reader;
    private Queue<char> Buf;

    private void Save(char b) {
      Buf.Enqueue(b);
    }

    private void Clear() {
      Buf.Clear();
    }

#if false
		private int SkipBOM()
		{
			for( var i = 0; i<UTF8_BOM.Length; ++i )
			{
				var c = Stream.ReadByte();
				if(c == -1 || c != (byte)UTF8_BOM[i])
					return c;
				Save( (char)c );
			}
			// perfix matched; discard it
			Clear();
			return Stream.ReadByte();
		}
#endif

    public void SkipComment() {
      var c = Reader.Read(); //SkipBOM();

      // first line is a comment (Unix exec. file)?
      if (c == '#') {
        do {
          c = Reader.Read();
        } while (c != -1 && c != '\n');

        Save((char) '\n'); // fix line number
      }
      else if (c != -1) {
        Save((char) c);
      }
    }
  }
}

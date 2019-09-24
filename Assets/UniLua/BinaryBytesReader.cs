using System;

namespace UniLua {
  public class BinaryBytesReader {
    private ILoadInfo LoadInfo;
    public int SizeOfSizeT;


    public BinaryBytesReader(ILoadInfo loadinfo) {
      LoadInfo = loadinfo;
      SizeOfSizeT = 0;
    }

    public byte[] ReadBytes(int count) {
      byte[] ret = new byte[count];
      for (int i = 0; i < count; ++i) {
        var c = LoadInfo.ReadByte();
        if (c == -1)
          throw new UndumpException("truncated");
        ret[i] = (byte) c;
      }
#if DEBUG_BINARY_READER
			var sb = new System.Text.StringBuilder();
			sb.Append("ReadBytes:");
			for( var i = 0; i<ret.Length; ++i )
			{
				sb.Append( string.Format(" {0:X02}", ret[i]) );
			}
			ULDebug.Log( sb.ToString() );
#endif
      return ret;
    }

    public int ReadInt() {
      var bytes = ReadBytes(4);
      int ret = BitConverter.ToInt32(bytes, 0);
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadInt: " + ret );
#endif
      return ret;
    }

    public uint ReadUInt() {
      var bytes = ReadBytes(4);
      uint ret = BitConverter.ToUInt32(bytes, 0);
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadUInt: " + ret );
#endif
      return ret;
    }

    public int ReadSizeT() {
      if (SizeOfSizeT <= 0) {
        throw new Exception("sizeof(size_t) is not valid:" + SizeOfSizeT);
      }

      var bytes = ReadBytes(SizeOfSizeT);
      UInt64 ret;
      switch (SizeOfSizeT) {
        case 4:
          ret = BitConverter.ToUInt32(bytes, 0);
          break;
        case 8:
          ret = BitConverter.ToUInt64(bytes, 0);
          break;
        default:
          throw new NotImplementedException();
      }

#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadSizeT: " + ret );
#endif

      if (ret > Int32.MaxValue)
        throw new NotImplementedException();

      return (int) ret;
    }

    public double ReadDouble() {
      var bytes = ReadBytes(8);
      double ret = BitConverter.ToDouble(bytes, 0);
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadDouble: " + ret );
#endif
      return ret;
    }

    public byte ReadByte() {
      var c = LoadInfo.ReadByte();
      if (c == -1)
        throw new UndumpException("truncated");
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadBytes: " + c );
#endif
      return (byte) c;
    }

    public string ReadString() {
      var n = ReadSizeT();
      if (n == 0)
        return null;

      var bytes = ReadBytes(n);

      // n=1: removing trailing '\0'
      string ret = System.Text.Encoding.UTF8.GetString(bytes, 0, n - 1);
#if DEBUG_BINARY_READER
			ULDebug.Log( "ReadString n:" + n + " ret:" + ret );
#endif
      return ret;
    }
  }
}

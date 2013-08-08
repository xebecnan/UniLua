using System;
using System.Collections.Generic;

namespace UniLua
{
	internal class ByteStringBuilder
	{
		public ByteStringBuilder()
		{
			BufList = new LinkedList<byte[]>();
			TotalLength = 0;
		}

		public override string ToString()
		{
			if( TotalLength <= 0 )
				return String.Empty;

			var result = new char[TotalLength];
			var i = 0;
			var node = BufList.First;
			while(node != null)
			{
				var buf = node.Value;
				for(var j=0; j<buf.Length; ++j)
				{
					result[i++] = (char)buf[j];
				}
				node = node.Next;
			}
			return new string(result);
		}

		public ByteStringBuilder Append(byte[] bytes, int start, int length)
		{
			var buf = new byte[length];
			Array.Copy(bytes, start, buf, 0, length);
			BufList.AddLast( buf );
			TotalLength += length;
			return this;
		}

		private LinkedList<byte[]> 	BufList;
		private int					TotalLength;
	}
}


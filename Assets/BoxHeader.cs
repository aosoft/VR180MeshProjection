using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshProjectionBoxParser
{
	public class BoxHeader
	{
		public long PositionHead;
		public long PositionBody;
		public long Size;
		public string BoxType;

		public BoxHeader MoveNext(Stream s)
		{
			s.Seek(PositionHead + Size, SeekOrigin.Begin);
			return GetBoxHeader(s);
		}

		public byte[] GetBox(Stream s)
		{
			var ret = new byte[Size];

			s.Seek(PositionHead, SeekOrigin.Begin);
			s.Read(ret, 0, ret.Length);

			return ret;
		}

		public BoxHeader Enter(Stream s, int offset = 0)
		{
			s.Seek(PositionBody + offset, SeekOrigin.Begin);
			return GetBoxHeader(s);
		}

		public BoxHeader Find(Stream s, string boxType)
		{
			var box = this;
			while (box != null && box.BoxType != boxType)
			{
				box = box.MoveNext(s);
			}
			return box;
		}

		public static BoxHeader GetBoxHeader(Stream s)
		{
			var pos = s.Position;
			var bsize = new byte[4];
			var bboxtype = new byte[4];
			if (s.Read(bsize, 0, bsize.Length) < bsize.Length)
			{
				return null;
			}
			if (s.Read(bboxtype, 0, bboxtype.Length) < bboxtype.Length)
			{
				return null;
			}

			var ret = new BoxHeader();

			ret.PositionHead = pos;
			ret.Size = BitConverter.ToUInt32(bsize.Reverse().ToArray(), 0);
			ret.BoxType = Encoding.ASCII.GetString(bboxtype);

			if (ret.Size == 1)
			{
				var bsize8 = new byte[8];
				if (s.Read(bsize8, 0, bsize8.Length) < bsize8.Length)
				{
					return null;
				}

				bsize8 = bsize8.Reverse().ToArray();
				ret.Size = BitConverter.ToInt64(bsize8, 0);
			}

			ret.PositionBody = s.Position;

			return ret;
		}
	}
}

//	VR180 Mesh Projection Box Parser
//	Copyright(c) Yasuhiro Taniuchi

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MeshProjectionBoxParser
{
	public struct MeshBoxVertex
	{
		public int x_index_delta;
		public int y_index_delta;
		public int z_index_delta;
		public int u_index_delta;
		public int v_index_delta;
	}

	public enum MeshBoxIndexType : int
	{
		Triangles = 0,
		TriangleStrip = 1,
		TriangleFan = 2
	}

	public class MeshBoxVertexList
	{
		public int texture_id;
		public MeshBoxIndexType index_type;
		public int[] index_as_delta;
	}

	public class MeshBox
	{
		public float[] coordinates;
		public MeshBoxVertex[] verticies;
		public MeshBoxVertexList[] vertex_lists;
	}

	public class MeshProjectionBox
	{
		/// <summary>
		/// Mesh Projection Box のバイナリをパースする。
		/// </summary>
		/// <param name="bytes">size, boxtype 込みのバイナリ</param>
		/// <returns></returns>
		public static MeshBox[] Parse(byte[] bytes)
		{
			using (var s = new MemoryStream(bytes, false))
			{
				var boxSize = ReadUInt32(s);
				var boxHeader = ReadUInt32(s);
				var boxVerFlag = ReadUInt32(s);

				var ret = new MeshProjectionBox();

				var crc = ReadUInt32(s);
				var encoding_four_cc = ReadUInt32(s);

				if (encoding_four_cc == 0x64666c38)
				{
					using (var s2 = new DeflateStream(s, CompressionMode.Decompress, true))
					{
						return ParseMeshBoxes(s2);
					}
				}
				else
				{
					return ParseMeshBoxes(s);
				}
			}
		}

		private static MeshBox[] ParseMeshBoxes(Stream s)
		{
			var ret = new List<MeshBox>();

			try
			{
				while (true)
				{
					var box = ParseMeshBox(s);
					if (box == null)
					{
						break;
					}
					ret.Add(box);
				}
			}
			catch
			{
			}

			return ret.ToArray();
		}

		private static MeshBox ParseMeshBox(Stream s)
		{
			var ret = new MeshBox();

			var boxSize = ReadUInt32(s);
			var boxHeader = ReadUInt32(s);

			var coordinate_count = ReadUInt32(s);
			ret.coordinates = new float[coordinate_count];
			for (uint i = 0; i < coordinate_count; i++)
			{
				ret.coordinates[i] = ReadSingle(s);
			}

			var ccsb = (int)Math.Ceiling(Math.Log(coordinate_count * 2, 2));

			var vertex_count = ReadUInt32(s);
			ret.verticies = new MeshBoxVertex[vertex_count];

			{
				var br = new BitStreamReader(s);
				for (uint i = 0; i < vertex_count; i++)
				{
					ret.verticies[i] = new MeshBoxVertex()
					{
						x_index_delta = FromEncodedUInt(br.ReadUInt32(ccsb)),
						y_index_delta = FromEncodedUInt(br.ReadUInt32(ccsb)),
						z_index_delta = FromEncodedUInt(br.ReadUInt32(ccsb)),
						u_index_delta = FromEncodedUInt(br.ReadUInt32(ccsb)),
						v_index_delta = FromEncodedUInt(br.ReadUInt32(ccsb))
					};
				}
			}

			var vertex_list_count = ReadUInt32(s);
			ret.vertex_lists = new MeshBoxVertexList[vertex_list_count];
			var vcsb = (int)Math.Ceiling(Math.Log(vertex_count * 2, 2));

			{
				var br = new BitStreamReader(s);
				for (uint i = 0; i < vertex_list_count; i++)
				{
					var vertex_list = new MeshBoxVertexList()
					{
						texture_id = s.ReadByte(),
						index_type = (MeshBoxIndexType)s.ReadByte()
					};

					var index_count = ReadUInt32(s);
					vertex_list.index_as_delta = new int[index_count];
					for (uint n = 0; n < index_count; n++)
					{
						vertex_list.index_as_delta[n] = FromEncodedUInt(br.ReadUInt32(vcsb));
					}

					ret.vertex_lists[i] = vertex_list;
				}
			}


			return ret;
		}

		private static UInt32 ReadUInt32(Stream s)
		{
			return (ReadByte(s) << 24) | (ReadByte(s) << 16) | (ReadByte(s) << 8) | ReadByte(s);
		}

		private static UInt32 ReadByte(Stream s)
		{
			var ret = s.ReadByte();
			if (ret < 0)
			{
				throw new Exception();
			}
			return (UInt32)ret;
		}

		private static int FromEncodedUInt(uint value)
		{
			return ((value & 1) == 0) ? (int)(value >> 1) : -(int)(value >> 1) - 1;
		}

		private static float ReadSingle(Stream s)
		{
			var bytes = new byte[4];
			if (s.Read(bytes, 0, bytes.Length) < bytes.Length)
			{
				throw new Exception();
			}
			bytes = bytes.Reverse().ToArray();
			return BitConverter.ToSingle(bytes, 0);
		}


		class BitStreamReader
		{
			private Stream _stream;
			private int _lastBits;
			private int _currentByte;

			public BitStreamReader(Stream s)
			{
				_stream = s;
				_lastBits = 0;
				_currentByte = 0;
			}

			public int ReadBit()
			{
				if (_lastBits == 0)
				{
					_currentByte = _stream.ReadByte() & 0xff;
					_lastBits = 8;
				}
				var ret = (_currentByte >> 7) & 1;
				_currentByte <<= 1;
				_lastBits--;
				return ret;
			}

			public uint ReadUInt32(int bitcount)
			{
				uint ret = 0;
				for (int i = 0; i < bitcount; i++)
				{
					ret = (ret << 1) | ((uint)ReadBit());
				}
				return ret;
			}
		}
	}
}

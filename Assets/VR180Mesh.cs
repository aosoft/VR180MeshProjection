using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.Video;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class VR180Mesh : MonoBehaviour
{
	private void Awake()
	{
		var meshFilter = GetComponent<MeshFilter>();
		var vp = GetComponent<VideoPlayer>();

		var bytes = GetMshpBin(vp.url);
		var meshes = MeshProjectionBoxParser.MeshProjectionBox.Parse(bytes);

		if (meshes != null && meshes.Length > 0)
		{
			//	VR180 では MeshBox はおそらく 2 個きます (左右で 1 つずつ) がここでは左側決め打ちで。
			var unityMesh = new Mesh();
			var mesh = meshes[0];

			{
				var unityVerticies = new Vector3[mesh.verticies.Length];
				var unityUVs = new Vector2[mesh.verticies.Length];

				var vertIndex = new Vector3Int();
				var uvIndex = new Vector2Int();
				for (int i = 0; i < mesh.verticies.Length; i++)
				{
					vertIndex.x += mesh.verticies[i].x_index_delta;
					vertIndex.y += mesh.verticies[i].y_index_delta;
					vertIndex.z += mesh.verticies[i].z_index_delta;
					uvIndex.x += mesh.verticies[i].u_index_delta;
					uvIndex.y += mesh.verticies[i].v_index_delta;

					//	Mesh Box は右手系なので左手系にする。
					var vert = new Vector3(mesh.coordinates[vertIndex.x], mesh.coordinates[vertIndex.y], -mesh.coordinates[vertIndex.z]);

					var uv = new Vector2(mesh.coordinates[uvIndex.x], mesh.coordinates[uvIndex.y]);
					unityVerticies[i] = vert;
					unityUVs[i] = uv;
				}

				unityMesh.vertices = unityVerticies;
				unityMesh.uv = unityUVs;
			}

			{
				var indicies = new int[mesh.vertex_lists[0].index_as_delta.Length];
				int index = 0;
				for (int i = 0; i < mesh.vertex_lists[0].index_as_delta.Length; i++)
				{
					index += mesh.vertex_lists[0].index_as_delta[i];
					indicies[i] = index;
				}

				//	Triangle Strip 決め打ち
				var unityIndicies = new List<int>();
				for (int i = 0; i < indicies.Length - 2; i++)
				{
					if ((i & 1) != 0)
					{
						unityIndicies.Add(indicies[i + 1]);
						unityIndicies.Add(indicies[i + 2]);
						unityIndicies.Add(indicies[i]);
					}
					else
					{
						unityIndicies.Add(indicies[i + 2]);
						unityIndicies.Add(indicies[i + 1]);
						unityIndicies.Add(indicies[i]);
					}
				}

				unityMesh.triangles = unityIndicies.ToArray();
			}
			meshFilter.mesh = unityMesh;
		}

	}

	// Use this for initialization
	void Start()
	{
		var vp = GetComponent<VideoPlayer>();

		vp.Play();
	}

	// Update is called once per frame
	void Update()
	{

	}

	private byte[] GetMshpBin(string fileName)
	{
		using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
		using (var s = new BufferedStream(fs))
		{
			//	途中で Box が見つからないと NullReferenceException
			//	.NET 4 にして ?. を使う方がよい
			var box = BoxHeader.GetBoxHeader(s)
				.Find(s, "moov").Enter(s)
				.Find(s, "trak").Enter(s)
				.Find(s, "mdia").Enter(s)
				.Find(s, "minf").Enter(s)
				.Find(s, "stbl").Enter(s)
				.Find(s, "stsd").Enter(s, 8)
				.Find(s, "avc1").Enter(s, 0x4e)
				.Find(s, "sv3d").Enter(s)
				.Find(s, "proj").Enter(s)
				.Find(s, "mshp");
			if (box != null)
			{
				return box.GetBox(s);
			}

			return null;
		}
	}


	private class BoxHeader
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
			ret.Size = System.BitConverter.ToUInt32(bsize.Reverse().ToArray(), 0);
			ret.BoxType = System.Text.Encoding.ASCII.GetString(bboxtype);

			if (ret.Size == 1)
			{
				var bsize8 = new byte[8];
				if (s.Read(bsize8, 0, bsize8.Length) < bsize8.Length)
				{
					return null;
				}

				bsize8 = bsize8.Reverse().ToArray();
				ret.Size = System.BitConverter.ToInt64(bsize8, 0);
			}

			ret.PositionBody = s.Position;

			return ret;
		}
	}

}

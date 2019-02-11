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
			var box = MeshProjectionBoxParser.BoxHeader.GetBoxHeader(s).Find(s, "moov").Enter(s);
			while (box.IsValid)
			{
				box = box.Find(s, "trak");
				var mshp = box.Enter(s)
					.Find(s, "mdia").Enter(s)
					.Find(s, "minf").Enter(s)
					.Find(s, "stbl").Enter(s)
					.Find(s, "stsd").Enter(s, 8)
					.Find(s, "avc1").Enter(s, 0x4e)
					.Find(s, "sv3d").Enter(s)
					.Find(s, "proj").Enter(s)
					.Find(s, "mshp");

				if (mshp.IsValid)
				{
					return mshp.GetBox(s);
				}

				box = box.MoveNext(s);
			}

			return null;
		}
	}



}

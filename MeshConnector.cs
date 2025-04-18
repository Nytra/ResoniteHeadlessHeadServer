using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class MeshConnector : IMeshConnector
{
	public MeshX Mesh = null;
	public string LocalPath = null;
	public Asset Asset;
	public BoundingBox Bounds;
	public MeshUploadHint Hint;
	public bool firstRender = true;
	public ulong ownerId;
	public void Initialize(Asset asset)
	{
		Asset = asset;
	}

	public void Unload()
	{
	}

	public void UpdateMeshData(MeshX meshx, MeshUploadHint uploadHint, BoundingBox bounds, AssetIntegrated onUpdated)
	{
		Mesh = meshx;
		LocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";
		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			LocalPath = LocalPath.Substring(0, Math.Min(LocalPath.Length, Thundagun.MAX_STRING_LENGTH));
		var elem = Asset?.Owner as IWorldElement;
		if (elem is null && LocalPath == "NULL") 
		{ 
			onUpdated(true);
			return;
		}
		if (elem != null && elem.IsLocalElement) // this skips stuff like UIX which is VERY LAGGY to send over sometimes
		{
			onUpdated(true);
			return;
		}
		ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
		Bounds = bounds;
		Hint = uploadHint;

		//UniLog.Log($"UpdateMeshConnector: {ownerId.ToString()} {Asset?.AssetURL?.LocalPath ?? "NULL"}");

		Thundagun.QueuePacket(new ApplyChangesMeshConnector(this));

		onUpdated(firstRender);
		firstRender = false;
	}
}

public struct UnityMeshIndicies
{
	public int topology;

	public int[] indicies;
}

public class ApplyChangesMeshConnector : UpdatePacket<MeshConnector>
{
	List<float3> verts = new();
	List<float3> normals = new();
	List<float4> tangents = new();
	List<color> colors = new();
	//List<int> triangleIndices = new();
	List<UnityMeshIndicies> submeshes = new();
	List<BoneBinding> boneBindings = new();
	List<Bone> bones = new();
	List<BlendShapeFrame> blendShapeFrames = new();
	float2[][] uv2d;
	float3[][] uv3d;
	float4[][] uv4d;
	string localPath;
	ulong ownerId;
	BoundingBox bounds;
	public ApplyChangesMeshConnector(MeshConnector owner) : base(owner)
	{
		localPath = owner.LocalPath;
		ownerId = owner.ownerId;
		MeshX mesh = owner.Mesh;
		bounds = owner.Bounds;

		if (mesh != null)
		{
			if (mesh.RawPositions != null)
			{
				//UniLog.Log($"Mesh has {mesh.RawPositions.Length} verts");
				foreach (var vert in mesh.RawPositions)
				{
					verts.Add(new float3(vert.x, vert.y, vert.z));
				}
			}

			if (mesh.RawNormals != null)
			{
				foreach (var normal in mesh.RawNormals)
				{
					normals.Add(new float3(normal.x, normal.y, normal.z));
				}
			}

			if (mesh.RawTangents != null)
			{
				foreach (var tangent in mesh.RawTangents)
				{
					tangents.Add(new float4(tangent.x, tangent.y, tangent.z, tangent.w));
				}
			}

			if (mesh.RawColors != null)
			{
				foreach (var color in mesh.RawColors)
				{
					if (mesh.Profile != ColorProfile.Linear)
						colors.Add(color.ToLinear(mesh.Profile));
					else
						colors.Add(new color(color.r, color.g, color.b, color.a));
				}
			}

			//var triSms = mesh.Submeshes.Where(sm2 => sm2 is TriangleSubmesh);
			//foreach (var sm in triSms)
			//{
			//var triSm = (TriangleSubmesh)sm;
			//for (int i = 0; i < triSm.Count; i++)
			//{
			//var tri = triSm.GetTriangle(i);
			//triangleIndices.Add(tri.Vertex0Index);
			//triangleIndices.Add(tri.Vertex1Index);
			//triangleIndices.Add(tri.Vertex2Index);
			//}
			//}

			for (int i = 0; i < mesh.SubmeshCount; i++)
			{
				Submesh submesh = mesh.GetSubmesh(i);
				if (submesh == null)
				{
					UnityMeshIndicies u2 = new UnityMeshIndicies();
					submeshes.Add(u2);
					continue;
				}
				UnityMeshIndicies unity = new();
				unity.topology = (int)submesh.Topology;
				if (submesh.RawIndicies != null && submesh.RawIndicies.Length > 0)
				{
					unity.indicies = new int[submesh.RawIndicies.Length];
					int j = 0;
					foreach (var index in submesh.RawIndicies)
					{
						unity.indicies[j] = index;
						j++;
					}
				}
				submeshes.Add(unity);
			}


			if (mesh.RawBoneBindings != null)
			{
				foreach (var boneBinding in mesh.RawBoneBindings)
				{
					boneBindings.Add(boneBinding);
				}
			}

			if (mesh.Bones != null)
			{
				foreach (var bone in mesh.Bones)
				{
					bones.Add(bone);
				}
			}

			if (mesh.BlendShapes != null)
			{
				foreach (var blendShape in mesh.BlendShapes)
				{
					foreach (var frame in blendShape.Frames)
					{
						blendShapeFrames.Add(frame);
					}
				}
			}

			int num = MathX.Min(mesh.UV_ChannelCount, 8);
			uv2d = new float2[num][];
			uv3d = new float3[num][];
			uv4d = new float4[num][];
			for (int i = 0; i < num; i++)
			{
				switch (mesh.GetUV_Dimension(i))
				{
					case 0:
						uv2d[i] = null;
						uv3d[i] = null;
						uv4d[i] = null;
						break;
					case 2:
						uv2d[i] = new float2[verts.Count];
						uv3d[i] = null;
						uv4d[i] = null;
						break;
					case 3:
						uv2d[i] = null;
						uv3d[i] = new float3[verts.Count];
						uv4d[i] = null;
						break;
					case 4:
						uv2d[i] = null;
						uv3d[i] = null;
						uv4d[i] = new float4[verts.Count];
						break;
				}
			}

			if (verts.Count > 0)
			{
				for (int k = 0; k < num; k++)
				{
					switch (mesh.GetUV_Dimension(k))
					{
						case 2:
							mesh.GetRawUVs(k).UnsafeCopyTo(uv2d[k], verts.Count);
							break;
						case 3:
							mesh.GetRawUVs_3D(k).UnsafeCopyTo(uv3d[k], verts.Count);
							break;
						case 4:
							mesh.GetRawUVs_4D(k).UnsafeCopyTo(uv4d[k], verts.Count);
							break;
					}
				}
			}
		}
	}

	public override int Id => (int)PacketTypes.ApplyChangesMesh;

	public override void Deserialize(CircularBuffer buffer)
	{
		var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes2);
		localPath = Encoding.UTF8.GetString(bytes2);

		buffer.Read(out ownerId);

		int vertCount;
		buffer.Read(out vertCount);
		for (int i = 0; i < vertCount; i++)
		{
			float x;
			buffer.Read(out x);
			float y;
			buffer.Read(out y);
			float z;
			buffer.Read(out z);
		}

		int normalCount;
		buffer.Read(out normalCount);
		for (int i = 0; i < normalCount; i++)
		{
			float x;
			buffer.Read(out x);
			float y;
			buffer.Read(out y);
			float z;
			buffer.Read(out z);
		}

		int tangentCount;
		buffer.Read(out tangentCount);
		for (int i = 0; i < tangentCount; i++)
		{
			float x;
			buffer.Read(out x);
			float y;
			buffer.Read(out y);
			float z;
			buffer.Read(out z);
			float w;
			buffer.Read(out w);
		}

		int colorCount;
		buffer.Read(out colorCount);
		for (int i = 0; i < colorCount; i++)
		{
			float r;
			buffer.Read(out r);
			float g;
			buffer.Read(out g);
			float b;
			buffer.Read(out b);
			float a;
			buffer.Read(out a);
		}

		//int triangleIndexCount;
		//buffer.Read(out triangleIndexCount);
		//for (int i = 0; i < triangleIndexCount / 3; i++)
		//{
		//int i0;
		//buffer.Read(out i0);
		//int i1;
		//buffer.Read(out i1);
		//int i2;
		//buffer.Read(out i2);
		//}

		int submeshCount;
		buffer.Read(out submeshCount);
		for (int i = 0; i < submeshCount; i++)
		{
			int topology;
			buffer.Read(out topology);

			int indexCount;
			buffer.Read(out indexCount);

			for (int j = 0;  j < indexCount; j++)
			{
				int ind2;
				buffer.Read(out ind2);
			}
		}

		int boneBindingCount;
		buffer.Read(out boneBindingCount);
		for (int i = 0; i < boneBindingCount; i++)
		{
			int i0;
			buffer.Read(out i0);
			int i1;
			buffer.Read(out i1);
			int i2;
			buffer.Read(out i2);
			int i3;
			buffer.Read(out i3);

			float w0;
			buffer.Read(out w0);
			float w1;
			buffer.Read(out w1);
			float w2;
			buffer.Read(out w2);
			float w3;
			buffer.Read(out w3);
		}

		int boneCount;
		buffer.Read(out boneCount);
		for (int i = 0; i < boneCount; i++)
		{
			float f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;

			buffer.Read(out f0);
			buffer.Read(out f1);
			buffer.Read(out f2);
			buffer.Read(out f3);

			buffer.Read(out f4);
			buffer.Read(out f5);
			buffer.Read(out f6);
			buffer.Read(out f7);

			buffer.Read(out f8);
			buffer.Read(out f9);
			buffer.Read(out f10);
			buffer.Read(out f11);

			buffer.Read(out f12);
			buffer.Read(out f13);
			buffer.Read(out f14);
			buffer.Read(out f15);
		}

		float cx, cy, cz;
		buffer.Read(out cx);
		buffer.Read(out cy);
		buffer.Read(out cz);

		float sx, sy, sz;
		buffer.Read(out sx);
		buffer.Read(out sy);
		buffer.Read(out sz);

		int blendShapeFrameCount;
		buffer.Read(out blendShapeFrameCount);
		for (int i = 0; i < blendShapeFrameCount; i++)
		{
			string name;
			var bytes3 = new byte[Thundagun.MAX_STRING_LENGTH];
			buffer.Read(bytes3);
			name = Encoding.UTF8.GetString(bytes3);

			float weight;
			buffer.Read(out weight);

			int positionsCount;
			buffer.Read(out positionsCount);
			for (int i2 = 0; i2 < positionsCount; i2++)
			{
				float px, py, pz;
				buffer.Read(out px);
				buffer.Read(out py);
				buffer.Read(out pz);
			}

			int normalsCount;
			buffer.Read(out normalsCount);
			for (int i2 = 0; i2 < normalsCount; i2++)
			{
				float nx, ny, nz;
				buffer.Read(out nx);
				buffer.Read(out ny);
				buffer.Read(out nz);
			}

			int tangentsCount;
			buffer.Read(out tangentsCount);
			for (int i2 = 0; i2 < tangentsCount; i2++)
			{
				float tx, ty, tz;
				buffer.Read(out tx);
				buffer.Read(out ty);
				buffer.Read(out tz);
			}
		}

		int uv2dcount;
		buffer.Read(out uv2dcount);
		for (int uv2d = 0; uv2d < uv2dcount; uv2d++)
		{
			int uvId;
			buffer.Read(out uvId);
			if (uvId == -999) continue;
			for (int x2 = 0; x2 < vertCount; x2++)
			{
				float x;
				buffer.Read(out x);
				float y;
				buffer.Read(out y);
			}
		}

		int uv3dcount;
		buffer.Read(out uv3dcount);
		for (int uv3d = 0; uv3d < uv3dcount; uv3d++)
		{
			int uvId;
			buffer.Read(out uvId);
			if (uvId == -999) continue;
			for (int x2 = 0; x2 < vertCount; x2++)
			{
				float x;
				buffer.Read(out x);
				float y;
				buffer.Read(out y);
				float z;
				buffer.Read(out z);
			}
		}

		int uv4dcount;
		buffer.Read(out uv4dcount);
		for (int uv4d = 0; uv4d < uv4dcount; uv4d++)
		{
			int uvId;
			buffer.Read(out uvId);
			if (uvId == -999) continue;
			for (int x2 = 0; x2 < vertCount; x2++)
			{
				float x;
				buffer.Read(out x);
				float y;
				buffer.Read(out y);
				float z;
				buffer.Read(out z);
				float w;
				buffer.Read(out w);
			}
		}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(Encoding.UTF8.GetBytes(localPath));

		buffer.Write(ref ownerId);

		int vertCount = verts.Count;
		buffer.Write(ref vertCount);
		//UniLog.Log($"Writing {vertCount * 3} vertex values to the buffer");
		foreach (var vert in verts)
		{
			float x = vert.x;
			buffer.Write(ref x);
			float y = vert.y;
			buffer.Write(ref y);
			float z = vert.z;
			buffer.Write(ref z);
		}

		int normalCount = normals.Count;
		buffer.Write(ref normalCount);
		foreach (var normal in normals)
		{
			float x = normal.x;
			buffer.Write(ref x);
			float y = normal.y;
			buffer.Write(ref y);
			float z = normal.z;
			buffer.Write(ref z);
		}

		int tangentCount = tangents.Count;
		buffer.Write(ref tangentCount);
		foreach (var tangent in tangents)
		{
			float x = tangent.x;
			buffer.Write(ref x);
			float y = tangent.y;
			buffer.Write(ref y);
			float z = tangent.z;
			buffer.Write(ref z);
			float w = tangent.w;
			buffer.Write(ref w);
		}

		int colorCount = colors.Count;
		buffer.Write(ref colorCount);
		foreach (var color in colors)
		{
			float r = color.r;
			buffer.Write(ref r);
			float g = color.g;
			buffer.Write(ref g);
			float b = color.b;
			buffer.Write(ref b);
			float a = color.a;
			buffer.Write(ref a);
		}

		//int triangleIndexCount = triangleIndices.Count;
		//buffer.Write(ref triangleIndexCount);
		//foreach (var idx in triangleIndices)
		//{
			//int idx2 = idx;
			//buffer.Write(ref idx2);
		//}

		int submeshCount = submeshes.Count;
		buffer.Write(ref submeshCount);
		foreach (var submesh in submeshes)
		{
			int topology = submesh.topology;
			buffer.Write(ref topology);

			int indexCount = submesh.indicies?.Length ?? 0;
			buffer.Write(ref indexCount);

			if (submesh.indicies != null)
			{
				foreach (var ind in submesh.indicies)
				{
					int ind2 = ind;
					buffer.Write(ref ind2);
				}
			}
		}

		int boneBindingCount = boneBindings.Count;
		buffer.Write(ref boneBindingCount);
		foreach (var boneBinding in boneBindings)
		{
			int i0 = boneBinding.boneIndex0;
			buffer.Write(ref i0);
			int i1 = boneBinding.boneIndex1;
			buffer.Write(ref i1);
			int i2 = boneBinding.boneIndex2;
			buffer.Write(ref i2);
			int i3 = boneBinding.boneIndex3;
			buffer.Write(ref i3);

			float w0 = boneBinding.weight0;
			buffer.Write(ref w0);
			float w1 = boneBinding.weight1;
			buffer.Write(ref w1);
			float w2 = boneBinding.weight2;
			buffer.Write(ref w2);
			float w3 = boneBinding.weight3;
			buffer.Write(ref w3);
		}

		int boneCount = bones.Count;
		buffer.Write(ref boneCount);
		foreach (var bone in bones)
		{
			float f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;

			f0 = bone.BindPose.m00;
			buffer.Write(ref f0);
			f1 = bone.BindPose.m01;
			buffer.Write(ref f1);
			f2 = bone.BindPose.m02;
			buffer.Write(ref f2);
			f3 = bone.BindPose.m03;
			buffer.Write(ref f3);

			f4 = bone.BindPose.m10;
			buffer.Write(ref f4);
			f5 = bone.BindPose.m11;
			buffer.Write(ref f5);
			f6 = bone.BindPose.m12;
			buffer.Write(ref f6);
			f7 = bone.BindPose.m13;
			buffer.Write(ref f7);

			f8 = bone.BindPose.m20;
			buffer.Write(ref f8);
			f9 = bone.BindPose.m21;
			buffer.Write(ref f9);
			f10 = bone.BindPose.m22;
			buffer.Write(ref f10);
			f11 = bone.BindPose.m23;
			buffer.Write(ref f11);

			f12 = bone.BindPose.m30;
			buffer.Write(ref f12);
			f13 = bone.BindPose.m31;
			buffer.Write(ref f13);
			f14 = bone.BindPose.m32;
			buffer.Write(ref f14);
			f15 = bone.BindPose.m33;
			buffer.Write(ref f15);
		}

		float cx, cy, cz;
		cx = bounds.Center.x;
		cy = bounds.Center.y;
		cz = bounds.Center.z;
		buffer.Write(ref cx);
		buffer.Write(ref cy);
		buffer.Write(ref cz);

		float sx, sy, sz;
		sx = bounds.Size.x;
		sy = bounds.Size.y;
		sz = bounds.Size.z;
		buffer.Write(ref sx);
		buffer.Write(ref sy);
		buffer.Write(ref sz);

		int blendShapeFrameCount = blendShapeFrames.Count;
		buffer.Write(ref blendShapeFrameCount);
		foreach (var blendShapeFrame in blendShapeFrames)
		{
			string name = blendShapeFrame.BlendShape.Name ?? "NULL";
			if (name.Length > Thundagun.MAX_STRING_LENGTH)
				name = name.Substring(0, Math.Min(name.Length, Thundagun.MAX_STRING_LENGTH));
			buffer.Write(Encoding.UTF8.GetBytes(name));

			float weight = blendShapeFrame.Weight;
			buffer.Write(ref weight);

			if (blendShapeFrame.RawPositions != null)
			{
				int positionsCount = blendShapeFrame.RawPositions.Length;
				buffer.Write(ref positionsCount);
				foreach (var pos in blendShapeFrame.RawPositions)
				{
					float px = pos.x;
					float py = pos.y;
					float pz = pos.z;
					buffer.Write(ref px);
					buffer.Write(ref py);
					buffer.Write(ref pz);
				}
			}
			else
			{
				int positionsCount = 0;
				buffer.Write(ref positionsCount);
			}

			if (blendShapeFrame.RawNormals != null)
			{
				int normalsCount = blendShapeFrame.RawNormals.Length;
				buffer.Write(ref normalsCount);
				foreach (var norm in blendShapeFrame.RawNormals)
				{
					float nx = norm.x;
					float ny = norm.y;
					float nz = norm.z;
					buffer.Write(ref nx);
					buffer.Write(ref ny);
					buffer.Write(ref nz);
				}
			}
			else
			{
				int normalsCount = 0;
				buffer.Write(ref normalsCount);
			}

			if (blendShapeFrame.RawTangents != null)
			{
				int tangentsCount = blendShapeFrame.RawTangents.Length;
				buffer.Write(ref tangentsCount);
				foreach (var tang in blendShapeFrame.RawTangents)
				{
					float tx = tang.x;
					float ty = tang.y;
					float tz = tang.z;
					buffer.Write(ref tx);
					buffer.Write(ref ty);
					buffer.Write(ref tz);
				}
			}
			else
			{
				int tangentsCount = 0;
				buffer.Write(ref tangentsCount);
			}
		}

		int uv2dcount = uv2d.Length;
		buffer.Write(ref uv2dcount);
		int i = 0;
		foreach (var arr in uv2d)
		{
			if (arr != null)
			{
				buffer.Write(ref i);
				foreach (var num in arr)
				{
					var x = num.x;
					buffer.Write(ref x);
					var y = num.y;
					buffer.Write(ref y);
				}
			}
			else
			{
				int dummy = -999;
				buffer.Write(ref dummy);
			}
			i++;
		}

		int uv3dcount = uv3d.Length;
		buffer.Write(ref uv3dcount);
		int j = 0;
		foreach (var arr in uv3d)
		{
			if (arr != null)
			{
				buffer.Write(ref j);
				foreach (var num in arr)
				{
					var x = num.x;
					buffer.Write(ref x);
					var y = num.y;
					buffer.Write(ref y);
					var z = num.z;
					buffer.Write(ref z);
				}
			}
			else
			{
				int dummy = -999;
				buffer.Write(ref dummy);
			}
			j++;
		}

		int uv4dcount = uv4d.Length;
		buffer.Write(ref uv4dcount);
		int k = 0;
		foreach (var arr in uv4d)
		{
			if (arr != null)
			{
				buffer.Write(ref k);
				foreach (var num in arr)
				{
					var x = num.x;
					buffer.Write(ref x);
					var y = num.y;
					buffer.Write(ref y);
					var z = num.z;
					buffer.Write(ref z);
					var w = num.w;
					buffer.Write(ref w);
				}
			}
			else
			{
				int dummy = -999;
				buffer.Write(ref dummy);
			}
			k++;
		}
	}
}
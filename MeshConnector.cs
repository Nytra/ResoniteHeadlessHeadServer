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
	public bool lastReadable;
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
		//if (elem != null && elem.IsLocalElement) // this skips stuff like UIX which is VERY LAGGY to send over sometimes
		//{
			//onUpdated(true);
			//return;
		//}
		ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
		Bounds = bounds;
		Hint = uploadHint;

		//UniLog.Log($"UpdateMeshConnector: {ownerId.ToString()} {Asset?.AssetURL?.LocalPath ?? "NULL"}");

		Thundagun.QueuePacket(new ApplyChangesMeshConnector(this));

		onUpdated(firstRender || !lastReadable);
		firstRender = false;
		lastReadable = uploadHint[MeshUploadHint.Flag.Readable];
	}
}

public struct MeshIndicies
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
	List<MeshIndicies> submeshes = new();
	List<BoneBinding> boneBindings = new();
	List<Bone> bones = new();
	List<BlendShapeFrame> blendShapeFrames = new();
	float2[][] uv2d;
	float3[][] uv3d;
	float4[][] uv4d;
	string localPath;
	ulong ownerId;
	BoundingBox bounds;
	MeshUploadHint uploadHint;
	//int blendShapeCount;
	//int boneCount;
	//bool hasBoneBindings;
	//int vertCount;
	//MeshX mesh;
	//int submeshCount;
	public ApplyChangesMeshConnector(MeshConnector owner) : base(owner)
	{
		localPath = owner.LocalPath;
		ownerId = owner.ownerId;
		var mesh = owner.Mesh;
		bounds = owner.Bounds;
		uploadHint = owner.Hint;

		if (mesh != null)
		{
			//vertCount = mesh.RawPositions?.Length ?? 0;
			//submeshCount = mesh.SubmeshCount;
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
					MeshIndicies defaultIndices = new MeshIndicies();
					submeshes.Add(defaultIndices);
					continue;
				}
				MeshIndicies indicies = new();
				indicies.topology = (int)submesh.Topology;
				if (submesh.RawIndicies != null && submesh.RawIndicies.Length > 0)
				{
					indicies.indicies = new int[submesh.RawIndicies.Length];
					int j = 0;
					foreach (var index in submesh.RawIndicies)
					{
						indicies.indicies[j] = index;
						j++;
					}
				}
				submeshes.Add(indicies);
			}

			//hasBoneBindings = mesh.HasBoneBindings;
			if (mesh.RawBoneBindings != null)
			{
				foreach (var boneBinding in mesh.RawBoneBindings)
				{
					boneBindings.Add(boneBinding);
				}
			}

			//boneCount = mesh.BoneCount;
			if (mesh.Bones != null)
			{
				foreach (var bone in mesh.Bones)
				{
					bones.Add(bone);
				}
			}

			//blendShapeCount = mesh.BlendShapeCount;
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
				if (uploadHint.GetUVChannel(i))
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
				
			}

			if (verts.Count > 0)
			{
				for (int k = 0; k < num; k++)
				{
					if (uploadHint.GetUVChannel(k))
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
	}

	public override int Id => (int)PacketTypes.ApplyChangesMesh;

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.WriteString2(localPath);

		buffer.Write(ownerId);

		buffer.Write(uploadHint[MeshUploadHint.Flag.Geometry]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.Positions]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.Normals]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.Tangents]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.Colors]);

		buffer.Write(uploadHint[MeshUploadHint.Flag.UV0s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV1s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV2s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV3s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV4s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV5s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV6s]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.UV7s]);

		buffer.Write(uploadHint[MeshUploadHint.Flag.BindPoses]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.BoneWeights]);

		buffer.Write(uploadHint[MeshUploadHint.Flag.Dynamic]);
		buffer.Write(uploadHint[MeshUploadHint.Flag.Readable]);

		//int vertCount = verts.Count;
		buffer.Write(verts.Count);
		//buffer.Write(submeshCount);

		//buffer.Write(blendShapeCount);
		//buffer.Write(boneCount);
		//buffer.Write(hasBoneBindings);

		if (uploadHint[MeshUploadHint.Flag.Positions])
		{
			//UniLog.Log($"Writing {vertCount * 3} vertex values to the buffer");
			//buffer.Write(verts.Count);
			foreach (var vert in verts)
			{
				float x = vert.x;
				buffer.Write(x);
				float y = vert.y;
				buffer.Write(y);
				float z = vert.z;
				buffer.Write(z);
			}
		}

		if (uploadHint[MeshUploadHint.Flag.Normals])
		{
			int normalCount = normals.Count;
			buffer.Write(normalCount);
			foreach (var normal in normals)
			{
				float x = normal.x;
				buffer.Write(x);
				float y = normal.y;
				buffer.Write(y);
				float z = normal.z;
				buffer.Write(z);
			}
		}
		
		if (uploadHint[MeshUploadHint.Flag.Tangents])
		{
			int tangentCount = tangents.Count;
			buffer.Write(tangentCount);
			foreach (var tangent in tangents)
			{
				float x = tangent.x;
				buffer.Write(x);
				float y = tangent.y;
				buffer.Write(y);
				float z = tangent.z;
				buffer.Write(z);
				float w = tangent.w;
				buffer.Write(w);
			}
		}
		
		if (uploadHint[MeshUploadHint.Flag.Colors])
		{
			int colorCount = colors.Count;
			buffer.Write(colorCount);
			foreach (var color in colors)
			{
				float r = color.r;
				buffer.Write(r);
				float g = color.g;
				buffer.Write(g);
				float b = color.b;
				buffer.Write(b);
				float a = color.a;
				buffer.Write(a);
			}
		}
		

		//int triangleIndexCount = triangleIndices.Count;
		//buffer.Write(ref triangleIndexCount);
		//foreach (var idx in triangleIndices)
		//{
			//int idx2 = idx;
			//buffer.Write(ref idx2);
		//}

		if (uploadHint[MeshUploadHint.Flag.Geometry])
		{
			int submeshCount = submeshes.Count;	
			buffer.Write(submeshCount);
			foreach (var submesh in submeshes)
			{
				int topology = submesh.topology;
				buffer.Write(topology);

				int indexCount = submesh.indicies?.Length ?? 0;
				buffer.Write(indexCount);

				if (submesh.indicies != null)
				{
					foreach (var ind in submesh.indicies)
					{
						int ind2 = ind;
						buffer.Write(ind2);
					}
				}
			}
		}

		if (uploadHint[MeshUploadHint.Flag.BoneWeights])
		{
			int boneBindingCount = boneBindings.Count;
			buffer.Write(boneBindingCount);
			foreach (var boneBinding in boneBindings)
			{
				int i0 = boneBinding.boneIndex0;
				buffer.Write(i0);
				int i1 = boneBinding.boneIndex1;
				buffer.Write(i1);
				int i2 = boneBinding.boneIndex2;
				buffer.Write(i2);
				int i3 = boneBinding.boneIndex3;
				buffer.Write(i3);

				float w0 = boneBinding.weight0;
				buffer.Write(w0);
				float w1 = boneBinding.weight1;
				buffer.Write(w1);
				float w2 = boneBinding.weight2;
				buffer.Write(w2);
				float w3 = boneBinding.weight3;
				buffer.Write(w3);
			}
		}
		
		if (uploadHint[MeshUploadHint.Flag.BindPoses])
		{
			int boneCount = bones.Count;
			buffer.Write(boneCount);
			foreach (var bone in bones)
			{
				float f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;

				f0 = bone.BindPose.m00;
				buffer.Write(f0);
				f1 = bone.BindPose.m01;
				buffer.Write(f1);
				f2 = bone.BindPose.m02;
				buffer.Write(f2);
				f3 = bone.BindPose.m03;
				buffer.Write(f3);

				f4 = bone.BindPose.m10;
				buffer.Write(f4);
				f5 = bone.BindPose.m11;
				buffer.Write(f5);
				f6 = bone.BindPose.m12;
				buffer.Write(f6);
				f7 = bone.BindPose.m13;
				buffer.Write(f7);

				f8 = bone.BindPose.m20;
				buffer.Write(f8);
				f9 = bone.BindPose.m21;
				buffer.Write(f9);
				f10 = bone.BindPose.m22;
				buffer.Write(f10);
				f11 = bone.BindPose.m23;
				buffer.Write(f11);

				f12 = bone.BindPose.m30;
				buffer.Write(f12);
				f13 = bone.BindPose.m31;
				buffer.Write(f13);
				f14 = bone.BindPose.m32;
				buffer.Write(f14);
				f15 = bone.BindPose.m33;
				buffer.Write(f15);
			}
		}

		float cx, cy, cz;
		cx = bounds.Center.x;
		cy = bounds.Center.y;
		cz = bounds.Center.z;
		buffer.Write(cx);
		buffer.Write(cy);
		buffer.Write(cz);

		float sx, sy, sz;
		sx = bounds.Size.x;
		sy = bounds.Size.y;
		sz = bounds.Size.z;
		buffer.Write(sx);
		buffer.Write(sy);
		buffer.Write(sz);

		int blendShapeFrameCount = blendShapeFrames.Count;
		buffer.Write(blendShapeFrameCount);
		foreach (var blendShapeFrame in blendShapeFrames)
		{
			string name = blendShapeFrame.BlendShape.Name ?? "NULL";
			if (name.Length > Thundagun.MAX_STRING_LENGTH)
				name = name.Substring(0, Math.Min(name.Length, Thundagun.MAX_STRING_LENGTH));
			buffer.WriteString2(name);

			float weight = blendShapeFrame.Weight;
			buffer.Write(weight);

			if (blendShapeFrame.RawPositions != null)
			{
				int positionsCount = blendShapeFrame.RawPositions.Length;
				buffer.Write(positionsCount);
				foreach (var pos in blendShapeFrame.RawPositions)
				{
					float px = pos.x;
					float py = pos.y;
					float pz = pos.z;
					buffer.Write(px);
					buffer.Write(py);
					buffer.Write(pz);
				}
			}
			else
			{
				int positionsCount = 0;
				buffer.Write(positionsCount);
			}

			if (blendShapeFrame.RawNormals != null)
			{
				int normalsCount = blendShapeFrame.RawNormals.Length;
				buffer.Write(normalsCount);
				foreach (var norm in blendShapeFrame.RawNormals)
				{
					float nx = norm.x;
					float ny = norm.y;
					float nz = norm.z;
					buffer.Write(nx);
					buffer.Write(ny);
					buffer.Write(nz);
				}
			}
			else
			{
				int normalsCount = 0;
				buffer.Write(normalsCount);
			}

			if (blendShapeFrame.RawTangents != null)
			{
				int tangentsCount = blendShapeFrame.RawTangents.Length;
				buffer.Write(tangentsCount);
				foreach (var tang in blendShapeFrame.RawTangents)
				{
					float tx = tang.x;
					float ty = tang.y;
					float tz = tang.z;
					buffer.Write(tx);
					buffer.Write(ty);
					buffer.Write(tz);
				}
			}
			else
			{
				int tangentsCount = 0;
				buffer.Write(tangentsCount);
			}
		}

		int uv2dcount = uv2d.Length;
		buffer.Write(uv2dcount);
		int i = 0;
		foreach (var arr in uv2d)
		{
			if (arr != null)
			{
				buffer.Write(i);
				foreach (var num in arr)
				{
					var x = num.x;
					buffer.Write(x);
					var y = num.y;
					buffer.Write(y);
				}
			}
			else
			{
				int dummy = -999;
				buffer.Write(dummy);
			}
			i++;
		}

		int uv3dcount = uv3d.Length;
		buffer.Write(uv3dcount);
		int j = 0;
		foreach (var arr in uv3d)
		{
			if (arr != null)
			{
				buffer.Write(j);
				foreach (var num in arr)
				{
					var x = num.x;
					buffer.Write(x);
					var y = num.y;
					buffer.Write(y);
					var z = num.z;
					buffer.Write(z);
				}
			}
			else
			{
				int dummy = -999;
				buffer.Write(dummy);
			}
			j++;
		}

		int uv4dcount = uv4d.Length;
		buffer.Write(uv4dcount);
		int k = 0;
		foreach (var arr in uv4d)
		{
			if (arr != null)
			{
				buffer.Write(k);
				foreach (var num in arr)
				{
					var x = num.x;
					buffer.Write(x);
					var y = num.y;
					buffer.Write(y);
					var z = num.z;
					buffer.Write(z);
					var w = num.w;
					buffer.Write(w);
				}
			}
			else
			{
				int dummy = -999;
				buffer.Write(dummy);
			}
			k++;
		}
	}
}
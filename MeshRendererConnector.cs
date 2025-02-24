using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class MeshRendererConnectorBase<T> : Connector<T> where T : MeshRenderer
{
	public override void ApplyChanges()
	{
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector<T>(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		Thundagun.QueuePacket(new DestroyMeshRendererConnector<T>(this, destroyingWorld));
	}

	public override void Initialize()
	{
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector<T>(this));
	}
}

public class MeshRendererConnector : MeshRendererConnectorBase<MeshRenderer>
{
}

public class SkinnedMeshRendererConnector : MeshRendererConnectorBase<SkinnedMeshRenderer>
{
}

public class ApplyChangesMeshRendererConnector<T> : UpdatePacket<MeshRendererConnectorBase<T>> where T: MeshRenderer
{
	List<float3> verts = new();
	List<float3> normals = new();
	List<float4> tangents = new();
	List<color> colors = new();
	List<int> triangleIndices = new();
	List<BoneBinding> boneBindings = new();
	List<Bone> bones = new();
	List<ulong> boneRefIds = new();
	ulong slotRefId;
	long worldId;
	string shaderPath;
	bool isSkinned;
	public ApplyChangesMeshRendererConnector(MeshRendererConnectorBase<T> owner) : base(owner)
	{
		slotRefId = owner.Owner.Slot.ReferenceID.Position;
		worldId = owner.Owner.World.LocalWorldHandle;
		isSkinned = owner.Owner is SkinnedMeshRenderer;
		var asset = owner.Owner.Material.Target?.Asset;
		shaderPath = "NULL";
		var matprovider = asset?.Owner as MaterialProvider;
		if (matprovider != null)
		{
			HashSet<StaticShader> hashset = matprovider.GetProviders<StaticShader>();
			var shad = hashset.FirstOrDefault();
			if (shad != null)
			{
				shaderPath = shad.Asset?.AssetURL?.LocalPath ?? "NULL";
				shaderPath = shaderPath.Substring(0, Math.Min(shaderPath.Length, 128));
				//UniLog.Log($"StaticShader LocalPath: {shaderPath}");
			}
		}
		MeshX mesh = owner.Owner.Mesh?.Asset?.Data;
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
					colors.Add(new color(color.r, color.g, color.b, color.a));
				}
			}
			
			var triSms = mesh.Submeshes.Where(sm2 => sm2 is TriangleSubmesh);
			foreach (var sm in triSms)
			{ 
				var triSm = (TriangleSubmesh)sm;
				for (int i = 0; i < triSm.Count; i++)
				{
					var tri = triSm.GetTriangle(i);
					triangleIndices.Add(tri.Vertex0Index);
					triangleIndices.Add(tri.Vertex1Index);
					triangleIndices.Add(tri.Vertex2Index);
				}
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

			if (isSkinned)
			{
				var skinned = owner.Owner as SkinnedMeshRenderer;
				foreach (var bone in skinned!.Bones)
				{
					if (bone == null) continue;
					boneRefIds.Add(bone.ReferenceID.Position);
				}
			}
		}
	}

	public override int Id => (int)PacketTypes.ApplyChangesMeshRenderer;

	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out slotRefId);
		buffer.Read(out worldId);
		buffer.Read(out isSkinned);

		var bytes2 = new byte[512];
		buffer.Read(bytes2);
		shaderPath = Encoding.UTF8.GetString(bytes2);

		int vertCount;
		buffer.Read(out vertCount);
		for(int i = 0; i < vertCount; i++)
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

		int triangleIndexCount;
		buffer.Read(out triangleIndexCount);
		for (int i = 0; i < triangleIndexCount / 3; i++)
		{
			int i0;
			buffer.Read(out i0);
			int i1;
			buffer.Read(out i1);
			int i2;
			buffer.Read(out i2);
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

		if (isSkinned)
		{
			int boneRefIdsCount;
			buffer.Read(out boneRefIdsCount);
			for (int i = 0; i < boneRefIdsCount; i++)
			{
				ulong refId;
				buffer.Read(out refId);
			}
		}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref slotRefId);
		buffer.Write(ref worldId);
		buffer.Write(ref isSkinned);

		buffer.Write(Encoding.UTF8.GetBytes(shaderPath));

		int vertCount = verts.Count;
		buffer.Write(ref vertCount);
		UniLog.Log($"Writing {vertCount * 3} vertex values to the buffer");
		//var arr = new float[vertCount * 3];
		//int i = 0;

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

		int triangleIndexCount = triangleIndices.Count;
		buffer.Write(ref triangleIndexCount);
		foreach (var idx in triangleIndices)
		{
			int idx2 = idx;
			buffer.Write(ref idx2);
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
			float f0,f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15;

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

		if (isSkinned)
		{
			int boneRefIdsCount = boneRefIds.Count;
			buffer.Write(ref boneRefIdsCount);
			foreach (var boneRefId in boneRefIds)
			{
				ulong refId = boneRefId;
				buffer.Write(ref refId);
			}
		}
	}
}

public class DestroyMeshRendererConnector<T> : UpdatePacket<MeshRendererConnectorBase<T>> where T : MeshRenderer
{
	public DestroyMeshRendererConnector(MeshRendererConnectorBase<T> owner, bool destroyingWorld) : base(owner)
	{
	}

	public override int Id => (int)PacketTypes.DestroyMeshRenderer;

	public override void Deserialize(CircularBuffer buffer)
	{
	}

	public override void Serialize(CircularBuffer buffer)
	{
	}
}
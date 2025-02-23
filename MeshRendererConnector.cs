using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using SharedMemory;

namespace Thundagun;

public class MeshRendererConnector : Connector<MeshRenderer>
{
	public override void ApplyChanges()
	{
		//if (Owner is SkinnedMeshRenderer) return;
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		//if (Owner is SkinnedMeshRenderer) return;
		Thundagun.QueuePacket(new DestroyMeshRendererConnector(this, destroyingWorld));
	}

	public override void Initialize()
	{
		//if (Owner is SkinnedMeshRenderer) return;
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector(this));
	}
}

public class ApplyChangesMeshRendererConnector : UpdatePacket<MeshRendererConnector>
{
	List<float3> verts = new();
	List<float3> normals = new();
	List<float4> tangents = new();
	List<int> triangleIndices = new();
	ulong slotRefId;
	long worldId;
	public ApplyChangesMeshRendererConnector(MeshRendererConnector owner) : base(owner)
	{
		slotRefId = owner.Owner.Slot.ReferenceID.Position;
		worldId = owner.Owner.World.LocalWorldHandle;
		MeshX mesh = owner.Owner.Mesh.Asset?.Data;
		if (mesh != null)
		{
			foreach (var vert in mesh.RawPositions)
			{
				verts.Add(new float3(vert.x, vert.y, vert.z));
			}
			foreach (var normal in mesh.RawNormals)
			{
				normals.Add(new float3(normal.x, normal.y, normal.z));
			}
			foreach (var tangent in mesh.RawTangents)
			{
				tangents.Add(new float4(tangent.x, tangent.y, tangent.z, tangent.w));
			}
			foreach (var sm in mesh.Submeshes.Where(sm2 => sm2 is TriangleSubmesh))
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
		}
	}

	public override int Id => (int)PacketTypes.ApplyChangesMeshRenderer;

	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out slotRefId);
		buffer.Read(out worldId);

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
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref slotRefId);
		buffer.Write(ref worldId);

		int vertCount = verts.Count;
		buffer.Write(ref vertCount);
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

		int triangleIndexCount = triangleIndices.Count;
		buffer.Write(ref triangleIndexCount);
		foreach (var idx in triangleIndices)
		{
			int idx2 = idx;
			buffer.Write(ref idx2);
		}
	}
}

public class DestroyMeshRendererConnector : UpdatePacket<MeshRendererConnector>
{
	public DestroyMeshRendererConnector(MeshRendererConnector owner, bool destroyingWorld) : base(owner)
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
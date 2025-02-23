using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class MeshRendererConnector : Connector<MeshRenderer>
{
	public override void ApplyChanges()
	{
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		Thundagun.QueuePacket(new DestroyMeshRendererConnector(this, destroyingWorld));
	}

	public override void Initialize()
	{
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector(this));
	}
}

public class ApplyChangesMeshRendererConnector : UpdatePacket<MeshRendererConnector>
{
	List<float3> verts = new();
	List<float3> normals = new();
	List<float4> tangents = new();
	List<color> colors = new();
	List<int> triangleIndices = new();
	ulong slotRefId;
	long worldId;
	string shaderPath;
	public ApplyChangesMeshRendererConnector(MeshRendererConnector owner) : base(owner)
	{
		slotRefId = owner.Owner.Slot.ReferenceID.Position;
		worldId = owner.Owner.World.LocalWorldHandle;
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
		}
	}

	public override int Id => (int)PacketTypes.ApplyChangesMeshRenderer;

	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out slotRefId);
		buffer.Read(out worldId);

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
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref slotRefId);
		buffer.Write(ref worldId);
		buffer.Write(Encoding.UTF8.GetBytes(shaderPath));

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
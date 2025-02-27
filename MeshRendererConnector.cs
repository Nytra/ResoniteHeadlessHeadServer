using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class MeshRendererConnectorBase<T> : Connector<T> where T : MeshRenderer
{
	public ulong OwnerId;
	public string LocalPath;
	public override void ApplyChanges()
	{
		var elem = Owner.Mesh.Asset?.Owner as IWorldElement;
		var localPath = Owner.Mesh.Asset?.AssetURL?.LocalPath ?? "NULL";
		if (elem is null && localPath == "NULL") return;
		OwnerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
		LocalPath = localPath;
		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector<T>(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		Thundagun.QueuePacket(new DestroyMeshRendererConnector<T>(this, destroyingWorld));
	}

	public override void Initialize()
	{
		//Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector<T>(this));
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
	ulong slotRefId;
	long worldId;
	string shaderPath;
	bool isSkinned;
	List<ulong> boneRefIds = new();
	string meshPath;
	ulong ownerId;
	List<float> blendShapeWeights = new();

	public ApplyChangesMeshRendererConnector(MeshRendererConnectorBase<T> owner) : base(owner)
	{
		slotRefId = (owner.Owner.Slot.ReferenceID.Position << 8) | (owner.Owner.Slot.ReferenceID.User & 0xFFul);
		worldId = owner.Owner.World.LocalWorldHandle;
		isSkinned = owner.Owner is SkinnedMeshRenderer;
		var asset = owner.Owner.Material.Target?.Asset;
		shaderPath = "NULL";
		ownerId = owner.OwnerId;
		meshPath = owner.LocalPath;
		meshPath = meshPath.Substring(0, Math.Min(meshPath.Length, Thundagun.MAX_STRING_LENGTH));

		//UniLog.Log($"ApplyChangesMeshRenderer: {ownerId} {meshPath}");

		var matprovider = asset?.Owner as MaterialProvider;
		if (matprovider != null)
		{
			HashSet<StaticShader> hashset = matprovider.GetProviders<StaticShader>();
			var shad = hashset.FirstOrDefault();
			if (shad != null)
			{
				shaderPath = shad.Asset?.AssetURL?.LocalPath ?? "NULL";
				shaderPath = shaderPath.Substring(0, Math.Min(shaderPath.Length, Thundagun.MAX_STRING_LENGTH));
				//UniLog.Log($"StaticShader LocalPath: {shaderPath}");
			}
		}
		if (isSkinned)
		{
			var skinned = owner.Owner as SkinnedMeshRenderer;
			foreach (var bone in skinned!.Bones)
			{
				if (bone == null)
				{
					boneRefIds.Add(default);
				}
				else
				{
					boneRefIds.Add((bone.ReferenceID.Position << 8) | (bone.ReferenceID.User & 0xFFul));
				}
			}

			foreach (var blendShapeWeight in skinned.BlendShapeWeights)
			{
				blendShapeWeights.Add(blendShapeWeight);
			}
		}
	}

	public override int Id => (int)PacketTypes.ApplyChangesMeshRenderer;

	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out slotRefId);
		buffer.Read(out worldId);
		buffer.Read(out isSkinned);

		var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes2);
		shaderPath = Encoding.UTF8.GetString(bytes2);

		var bytes3 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes3);
		meshPath = Encoding.UTF8.GetString(bytes3);

		buffer.Read(out ownerId);

		if (isSkinned)
		{
			int boneRefIdsCount;
			buffer.Read(out boneRefIdsCount);
			for (int i = 0; i < boneRefIdsCount; i++)
			{
				ulong refId;
				buffer.Read(out refId);
			}

			int blendShapeWeightCount;
			buffer.Read(out blendShapeWeightCount);
			for (int i = 0; i < blendShapeWeightCount; i++)
			{
				float weight;
				buffer.Read(out weight);
			}
		}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref slotRefId);
		buffer.Write(ref worldId);
		buffer.Write(ref isSkinned);

		buffer.Write(Encoding.UTF8.GetBytes(shaderPath));

		buffer.Write(Encoding.UTF8.GetBytes(meshPath));

		buffer.Write(ref ownerId);

		if (isSkinned)
		{
			int boneRefIdsCount = boneRefIds.Count;
			buffer.Write(ref boneRefIdsCount);
			foreach (var boneRefId in boneRefIds)
			{
				ulong refId = boneRefId;
				buffer.Write(ref refId);
			}

			int blendShapeWeightCount = blendShapeWeights.Count;
			buffer.Write(ref blendShapeWeightCount);
			foreach (var blendShapeWeight in blendShapeWeights)
			{
				float weight = blendShapeWeight;
				buffer.Write(ref weight);
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
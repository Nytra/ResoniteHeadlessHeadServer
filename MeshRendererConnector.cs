﻿using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class MeshRendererConnectorBase<T> : Connector<T> where T : MeshRenderer
{
	public ulong MeshCompId;
	public string MeshLocalPath;
	public override void ApplyChanges()
	{
		Task.Run(async () => 
		{ 
			await Task.Delay(1000);

			var elem = Owner.Mesh?.Asset?.Owner as IWorldElement;
			var localPath = Owner.Mesh?.Asset?.AssetURL?.LocalPath ?? "NULL";
			if (elem is null && localPath == "NULL") return;
			//if (elem is null) return;
			MeshCompId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
			MeshLocalPath = localPath;

			Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector<T>(this));
		});
		
	}

	public override void Destroy(bool destroyingWorld)
	{
		Thundagun.QueuePacket(new DestroyMeshRendererConnector<T>(this, destroyingWorld));
	}

	public override void Initialize()
	{
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
	//string shaderFilePath;
	List<string> shaderFilePaths = new();
	//string shaderLocalPath;
	List<string> shaderLocalPaths = new();
	//ulong matCompId;
	List<ulong> matCompIds = new();
	bool isSkinned;
	List<ulong> boneRefIds = new();
	string meshPath;
	ulong meshCompId;
	List<float> blendShapeWeights = new();
	public bool enabled;
	public ulong rendId;

	public ApplyChangesMeshRendererConnector(MeshRendererConnectorBase<T> owner) : base(owner)
	{
		slotRefId = (owner.Owner.Slot.ReferenceID.Position << 8) | (owner.Owner.Slot.ReferenceID.User & 0xFFul);
		worldId = owner.Owner.World.LocalWorldHandle;
		isSkinned = owner.Owner is SkinnedMeshRenderer;
		var asset = owner.Owner.Material.Target?.Asset;
		meshCompId = owner.MeshCompId;
		meshPath = owner.MeshLocalPath;
		if (meshPath.Length > Thundagun.MAX_STRING_LENGTH)
			meshPath = meshPath.Substring(0, Math.Min(meshPath.Length, Thundagun.MAX_STRING_LENGTH));
		enabled = owner.Owner.Enabled;
		rendId = (owner.Owner.ReferenceID.Position << 8) | (owner.Owner.ReferenceID.User & 0xFFul);

		int i = 0;
		foreach (var mat in owner.Owner.Materials)
		{
			//shaderFilePaths.Add("NULL");
			//shaderLocalPaths.Add("NULL");

			var materialTarget = mat?.Asset?.Owner as IWorldElement;
			if (materialTarget != null)
			{
				matCompIds.Add((materialTarget.ReferenceID.Position << 8) | (materialTarget.ReferenceID.User & 0xFFul));
			}
			else
			{
				matCompIds.Add(default);
			}

			var matprovider = materialTarget as MaterialProvider;
			if (matprovider != null)
			{
				HashSet<StaticShader> hashset = matprovider.GetProviders<StaticShader>();
				var shad = hashset.FirstOrDefault(sh => sh != null);
				if (shad != null)
				{
					var shaderPath = shad.Asset?.AssetURL?.LocalPath ?? "NULL";
					if (shaderPath.Length > Thundagun.MAX_STRING_LENGTH)
						shaderPath = shaderPath.Substring(0, Math.Min(shaderPath.Length, Thundagun.MAX_STRING_LENGTH));
					shaderLocalPaths.Add(shaderPath);
					try
					{
						var shaderFilePath = ShaderConnector.LocalPathToFile[shaderPath];
						if (shaderFilePath.Length > Thundagun.MAX_STRING_LENGTH)
							shaderFilePath = shaderFilePath.Substring(0, Math.Min(shaderFilePath.Length, Thundagun.MAX_STRING_LENGTH));
						shaderFilePaths.Add(shaderFilePath);
					}
					catch (Exception e)
					{
						shaderFilePaths.Add("NULL");
					}
				}
				else
				{
					shaderFilePaths.Add("NULL");
					shaderLocalPaths.Add("NULL");
				}
			}
			else
			{
				shaderFilePaths.Add("NULL");
				shaderLocalPaths.Add("NULL");
			}
			i++;
		}

		

		//UniLog.Log($"ApplyChangesMeshRenderer: {isSkinned} {matCompId} {meshCompId} {shaderPath} {meshPath}");

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
		buffer.Read(out rendId);

		buffer.Read(out slotRefId);
		buffer.Read(out worldId);
		buffer.Read(out isSkinned);

		buffer.Read(out enabled);

		//var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		//buffer.Read(bytes2);
		//shaderFilePath = Encoding.UTF8.GetString(bytes2);

		//var bytes4 = new byte[Thundagun.MAX_STRING_LENGTH];
		//buffer.Read(bytes4);
		//shaderLocalPath = Encoding.UTF8.GetString(bytes4);

		//buffer.Read(out matCompId);

		int matCount;
		buffer.Read(out matCount);
		for (int i = 0; i < matCount; i++)
		{
			string shaderFilePath;
			buffer.ReadString(out shaderFilePath);

			string shaderLocalPath;
			buffer.ReadString(out shaderLocalPath);

			ulong matCompId;
			buffer.Read(out matCompId);
		}

		var bytes3 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes3);
		meshPath = Encoding.UTF8.GetString(bytes3);

		buffer.Read(out meshCompId);

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
		buffer.Write(ref rendId);

		buffer.Write(ref slotRefId);
		buffer.Write(ref worldId);
		buffer.Write(ref isSkinned);

		buffer.Write(ref enabled);

		//buffer.Write(Encoding.UTF8.GetBytes(shaderFilePath));

		//buffer.Write(Encoding.UTF8.GetBytes(shaderLocalPath));

		//buffer.Write(ref matCompId);

		int matCount = matCompIds.Count;
		buffer.Write(ref matCount);
		for (int i = 0; i < matCount; i++)
		{
			string shaderFilePath = shaderFilePaths[i];
			buffer.WriteString(shaderFilePath);

			string shaderLocalPath = shaderLocalPaths[i];
			buffer.WriteString(shaderLocalPath);

			ulong matCompId = matCompIds[i];
			buffer.Write(ref matCompId);
		}

		buffer.Write(Encoding.UTF8.GetBytes(meshPath));

		buffer.Write(ref meshCompId);

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
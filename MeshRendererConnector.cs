using FrooxEngine;
using SharedMemory;
using System;
using System.Reflection;
using System.Text;

namespace Thundagun;

public class MeshRendererConnectorBase<T> : Connector<T> where T : MeshRenderer
{
	public ulong MeshCompId;
	public string MeshLocalPath;
	public override void ApplyChanges()
	{
		//World.RunInSeconds(1, () =>
		//{
			
		//});

		var elem = Owner.Mesh?.Asset?.Owner as IWorldElement;
		var localPath = Owner.Mesh?.Asset?.AssetURL?.LocalPath ?? "NULL";
		if (elem is null && localPath == "NULL") return;
		//if (elem is null) return;
		MeshCompId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
		MeshLocalPath = localPath;

		Thundagun.QueuePacket(new ApplyChangesMeshRendererConnector<T>(this));
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
	public int sortingOrder;
	public int shadowCastingMode;
	public int motionVectorMode;
	List<ulong> blockOwnerIds = new();

	public ApplyChangesMeshRendererConnector(MeshRendererConnectorBase<T> owner) : base(owner)
	{
		slotRefId = (owner.Owner.Slot.ReferenceID.Position << 8) | (owner.Owner.Slot.ReferenceID.User & 0xFFul);
		worldId = owner.Owner.World.LocalWorldHandle;
		isSkinned = owner.Owner is SkinnedMeshRenderer;
		var asset = owner.Owner.Material.Target?.Asset;
		meshCompId = owner.MeshCompId;
		meshPath = owner.MeshLocalPath;
		//if (meshPath.Length > Thundagun.MAX_STRING_LENGTH)
			//meshPath = meshPath.Substring(0, Math.Min(meshPath.Length, Thundagun.MAX_STRING_LENGTH));
		enabled = owner.Owner.Enabled;
		rendId = (owner.Owner.ReferenceID.Position << 8) | (owner.Owner.ReferenceID.User & 0xFFul);
		sortingOrder = owner.Owner.SortingOrder.Value;
		shadowCastingMode = (int)owner.Owner.ShadowCastMode.Value;
		motionVectorMode = (int)owner.Owner.MotionVectorMode.Value;

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
				//HashSet<StaticShader> hashset = matprovider.GetProviders<StaticShader>();
				var shader = (Shader)matprovider.GetType().GetMethod("GetShader", BindingFlags.NonPublic |  BindingFlags.Instance).Invoke(matprovider, null);
				//var shad = hashset.FirstOrDefault(sh => sh != null);
				if (shader != null)
				{
					var shaderPath = shader.AssetURL?.LocalPath ?? "NULL";
					//if (shaderPath.Length > Thundagun.MAX_STRING_LENGTH)
						//shaderPath = shaderPath.Substring(0, Math.Min(shaderPath.Length, Thundagun.MAX_STRING_LENGTH));
					shaderLocalPaths.Add(shaderPath);
					try
					{
						var shaderFilePath = ShaderConnector.LocalPathToFile[shaderPath + shader.VariantIndex?.ToString() ?? ""];
						//var shaderFilePath = ShaderConnector.ShaderToFile[shader.Connector as ShaderConnector];
						//if (shaderFilePath.Length > Thundagun.MAX_STRING_LENGTH)
							//shaderFilePath = shaderFilePath.Substring(0, Math.Min(shaderFilePath.Length, Thundagun.MAX_STRING_LENGTH));
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

		foreach (var block in owner.Owner.MaterialPropertyBlocks)
		{
			var blockTarget = block?.Asset?.Owner as IWorldElement;
			if (blockTarget != null)
			{
				blockOwnerIds.Add((blockTarget.ReferenceID.Position << 8) | (blockTarget.ReferenceID.User & 0xFFul));
			}
			else
			{
				blockOwnerIds.Add(default);
			}
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

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.Write(rendId);

		buffer.Write(slotRefId);
		buffer.Write(worldId);
		buffer.Write(isSkinned);

		buffer.Write(enabled);

		buffer.Write(sortingOrder);
		buffer.Write(shadowCastingMode);
		buffer.Write(motionVectorMode);

		//buffer.Write(Encoding.UTF8.GetBytes(shaderFilePath));

		//buffer.Write(Encoding.UTF8.GetBytes(shaderLocalPath));

		//buffer.Write(ref matCompId);

		int matCount = matCompIds.Count;
		buffer.Write(matCount);
		for (int i = 0; i < matCount; i++)
		{
			string shaderFilePath = shaderFilePaths[i];
			buffer.WriteString2(shaderFilePath);

			string shaderLocalPath = shaderLocalPaths[i];
			buffer.WriteString2(shaderLocalPath);

			ulong matCompId = matCompIds[i];
			buffer.Write(matCompId);
		}

		int blockCount = blockOwnerIds.Count;
		buffer.Write(blockCount);
		for (int i = 0; i < blockCount; i++)
		{
			ulong blockId = blockOwnerIds[i];
			buffer.Write(blockId);
		}

		buffer.WriteString2(meshPath);

		buffer.Write(meshCompId);

		if (isSkinned)
		{
			int boneRefIdsCount = boneRefIds.Count;
			buffer.Write(boneRefIdsCount);
			foreach (var boneRefId in boneRefIds)
			{
				ulong refId = boneRefId;
				buffer.Write(refId);
			}

			int blendShapeWeightCount = blendShapeWeights.Count;
			buffer.Write(blendShapeWeightCount);
			foreach (var blendShapeWeight in blendShapeWeights)
			{
				float weight = blendShapeWeight;
				buffer.Write(weight);
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

	public override void Serialize(BinaryWriter buffer)
	{
	}
}
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Collections;
using System.Text;

namespace Thundagun;

public class MaterialConnector : MaterialConnectorBase, IMaterialConnector, ISharedMaterialConnector, IAssetConnector, ISharedMaterialPropertySetter, IMaterialPropertySetter
{
	public string ShaderLocalPath;
	public string ShaderFilePath;
	
	public Shader targetShader;
	public void ApplyChanges(Shader shader, AssetIntegrated onDone)
	{
		targetShader = shader;
		ShaderLocalPath = targetShader?.AssetURL?.LocalPath ?? "NULL";
		try
		{
			ShaderFilePath = ShaderConnector.LocalPathToFile[ShaderLocalPath];
		}
		catch (Exception e)
		{
			//UniLog.Warning($"Shader file path is null.");
			ShaderFilePath = "NULL";
		}

		var owner = Asset?.Owner as IWorldElement;

		//if (ShaderPath == "NULL") return;

		ownerId = ((owner?.ReferenceID.Position ?? default) << 8) | ((owner?.ReferenceID.User ?? default) & 0xFFul);

		//UniLog.Log($"ApplyChangesMaterial: {ownerId}, Actions Count: {actionQueue?.Count ?? -1}, {ShaderLocalPath} {ShaderFilePath}");

		var thing = new ApplyChangesMaterialConnectorBase(this);
		if (Asset.HighPriorityIntegration)
			Thundagun.QueueHighPriorityPacket(thing);
		else
			Thundagun.QueuePacket(thing);

		onDone(firstRender);
		firstRender = false;
	}
}

public class ApplyChangesMaterialConnectorBase : UpdatePacket<MaterialConnectorBase>
{
	// upload material info, then upload actions in queue
	string shaderFilePath;
	string shaderLocalPath;
	public Queue<MaterialConnectorBase.MaterialAction> actionQueue;
	public ulong ownerId;
	public bool isPropertyBlock;
	public ApplyChangesMaterialConnectorBase(MaterialConnectorBase owner) : base(owner)
	{
		if (owner is MaterialConnector matConn)
		{
			shaderFilePath = matConn.ShaderFilePath;
			//if (shaderFilePath.Length > Thundagun.MAX_STRING_LENGTH)
				//shaderFilePath = shaderFilePath.Substring(0, Math.Min(shaderFilePath.Length, Thundagun.MAX_STRING_LENGTH));

			shaderLocalPath = matConn.ShaderLocalPath;
			//if (shaderLocalPath.Length > Thundagun.MAX_STRING_LENGTH)
				//shaderLocalPath = shaderLocalPath.Substring(0, Math.Min(shaderLocalPath.Length, Thundagun.MAX_STRING_LENGTH));
		}
		else
		{
			shaderFilePath = "NULL";
			shaderLocalPath = "NULL";
		}
		
		isPropertyBlock = owner.isPropertyBlock;
		//actionQueue = new Queue<MaterialConnectorBase.MaterialAction>(owner.actionQueue);
		actionQueue = new Queue<MaterialConnectorBase.MaterialAction>(owner.actionQueue);
		owner.actionQueue.Clear();
		ownerId = owner.ownerId;
	}

	public override int Id => (int)PacketTypes.ApplyChangesMaterial;

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.WriteString2(shaderFilePath);

		buffer.WriteString2(shaderLocalPath);

		buffer.Write(ownerId);

		buffer.Write(isPropertyBlock);

		int actionCount = actionQueue.Count;
		buffer.Write(actionCount);
		while (actionQueue != null && actionQueue.Count > 0)
		{
			MaterialConnectorBase.MaterialAction action = actionQueue.Dequeue();
			// int, int, float4, object

			int type = (int)action.type;
			int propertyIndex = action.propertyIndex;
			float4 float4Value = action.float4Value;
			object obj = action.obj; // string, string, List<float>, List<float4>, itexture - TYPES: flag, tag, floatarray, float4array, texture

			buffer.Write(type);

			string propName;
			if (MaterialConnectorBase.IdToPropName.TryGetValue(propertyIndex, out propName))
			{
				buffer.WriteString2(propName);
			}
			else
			{
				buffer.WriteString2("oof");
			}

			// write float4

			float f0,f1,f2,f3;
			f0 = float4Value.x;
			f1 = float4Value.y;
			f2 = float4Value.z;
			f3 = float4Value.w;
			buffer.Write(f0);
			buffer.Write(f1);
			buffer.Write(f2);
			buffer.Write(f3);

			if (type == (int)MaterialConnectorBase.ActionType.Flag || type == (int)MaterialConnectorBase.ActionType.Tag)
			{
				if ((string)obj != null)
				{
					string newStr = (string)obj;
					//if (newStr.Length > Thundagun.MAX_STRING_LENGTH)
						//newStr = newStr.Substring(0, Math.Min(newStr.Length, Thundagun.MAX_STRING_LENGTH));
					buffer.WriteString2(newStr);
				}
				else
				{
					buffer.WriteString2("NULL");
				}
			}
			else if (type == (int)MaterialConnectorBase.ActionType.FloatArray)
			{
				var arr = (List<float>)obj;
				int arrCount = arr.Count;
				buffer.Write(arrCount);

				foreach (var flt in arr.ToArray())
				{
					float flt2 = flt;
					buffer.Write(flt2);
				}
			}
			else if (type == (int)MaterialConnectorBase.ActionType.Float4Array)
			{
				var arr = (List<float4>)obj;
				int arrCount = arr.Count;
				buffer.Write(arrCount);

				foreach (var flt in arr.ToArray())
				{
					float ff0, ff1, ff2, ff3;
					ff0 = flt.x;
					ff1 = flt.y;
					ff2 = flt.z;
					ff3 = flt.w;
					buffer.Write(ff0);
					buffer.Write(ff1);
					buffer.Write(ff2);
					buffer.Write(ff3);
				}
			}
			else if (type == (int)MaterialConnectorBase.ActionType.Matrix)
			{
				// never used in frooxengine, don't need to handle this
			}
			else if (type == (int)MaterialConnectorBase.ActionType.Texture)
			{
				// handle textures here later? needs TextureConnector
				var tex = obj as ITexture;
				var texConn = tex?.Connector as TextureConnector;

				//var localPath = texConn?.LocalPath ?? "NULL";
				var LocalPath = texConn?.LocalPath ?? "NULL";
				//if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
					//LocalPath = LocalPath.Substring(0, Math.Min(LocalPath.Length, Thundagun.MAX_STRING_LENGTH));

				//if (LocalPath == "NULL" && texConn?.Asset?.Owner is GlyphAtlasManager atlasManager)
				//{
				//	var texPath = atlasManager.Texture?.AssetURL?.LocalPath;
				//	var elem2 = atlasManager.Texture?.Owner as IWorldElement;
				//	var texId = elem2?.ReferenceID.ToString();
				//	LocalPath = atlasManager.Font.Data.Name + (texPath ?? texId ?? "");
				//}

				//if (LocalPath == "NULL")
				//{
					//LocalPath = texConn?.GetHashCode().ToString() ?? "NULL";
				//}
				buffer.WriteString2(LocalPath);

				//var elem = texConn?.Asset?.Owner as IWorldElement;
				//var ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
				var texOwnerId = texConn?.ownerId ?? default;

				//var ownerId = texConn?.ownerId ?? default;
				buffer.Write(texOwnerId);
			}
		}
	}
}
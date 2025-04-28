using Elements.Core;
using FrooxEngine;
using SharedMemory;

namespace Thundagun;

public class MaterialConnector : MaterialConnectorBase, IMaterialConnector, ISharedMaterialConnector, IAssetConnector, ISharedMaterialPropertySetter, IMaterialPropertySetter
{
	public bool firstRender = true;
	public string ShaderLocalPath;
	public string ShaderFilePath;
	public ulong ownerId;
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

		var thing = new ApplyChangesMaterialConnector(this);
		if (Asset.HighPriorityIntegration)
			Thundagun.QueueHighPriorityPacket(thing);
		else
			Thundagun.QueuePacket(thing);

		onDone(firstRender);
		firstRender = false;
	}

	public void SetInstancing(bool state)
	{
		Enqueue(new MaterialAction(ActionType.Instancing, -1, new float4(state ? 1 : 0)));
	}

	public void SetRenderQueue(int renderQueue)
	{
		Enqueue(new MaterialAction(ActionType.RenderQueue, -1, new float4(renderQueue)));
	}

	public void SetTag(MaterialTag tag, string value)
	{
		Enqueue(new MaterialAction(ActionType.Tag, (int)tag, float4.Zero, value));
	}
}

public class ApplyChangesMaterialConnector : UpdatePacket<MaterialConnector>
{
	// upload material info, then upload actions in queue
	string shaderFilePath;
	string shaderLocalPath;
	public Queue<MaterialConnectorBase.MaterialAction> actionQueue;
	public ulong ownerId;
	public ApplyChangesMaterialConnector(MaterialConnector owner) : base(owner)
	{
		shaderFilePath = owner.ShaderFilePath;
		if (shaderFilePath.Length > Thundagun.MAX_STRING_LENGTH)
			shaderFilePath = shaderFilePath.Substring(0, Math.Min(shaderFilePath.Length, Thundagun.MAX_STRING_LENGTH));

		shaderLocalPath = owner.ShaderLocalPath;
		if (shaderLocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			shaderLocalPath = shaderLocalPath.Substring(0, Math.Min(shaderLocalPath.Length, Thundagun.MAX_STRING_LENGTH));

		//actionQueue = new Queue<MaterialConnectorBase.MaterialAction>(owner.actionQueue);
		actionQueue = owner.actionQueue;
		ownerId = owner.ownerId;
	}

	public override int Id => (int)PacketTypes.ApplyChangesMaterial;

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.WriteString2(shaderFilePath);

		buffer.WriteString2(shaderLocalPath);

		buffer.Write(ownerId);

		int actionCount = Owner.actionQueue.Count;
		buffer.Write(actionCount);
		while (Owner.actionQueue != null && Owner.actionQueue.Count > 0)
		{
			MaterialConnectorBase.MaterialAction action = Owner.actionQueue.Dequeue();
			// int, int, float4, object

			int type = (int)action.type;
			int propertyIndex = action.propertyIndex;
			float4 float4Value = action.float4Value;
			object obj = action.obj; // string, string, List<float>, List<float4>, itexture - TYPES: flag, tag, floatarray, float4array, texture

			buffer.Write(type);
			buffer.Write(propertyIndex);
			
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
					if (newStr.Length > Thundagun.MAX_STRING_LENGTH)
						newStr = newStr.Substring(0, Math.Min(newStr.Length, Thundagun.MAX_STRING_LENGTH));
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

				var localPath = texConn?.LocalPath ?? "NULL";
				if (localPath == "NULL" && texConn?.Asset?.Owner is GlyphAtlasManager atlasManager)
				{
					localPath = atlasManager.Font.Data.Name;
				}
				buffer.WriteString2(localPath);

				var ownerId = texConn?.ownerId ?? default;
				buffer.Write(ownerId);
			}
		}
	}
}
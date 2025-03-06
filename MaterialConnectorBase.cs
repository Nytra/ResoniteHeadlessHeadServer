using Elements.Core;
using FrooxEngine;
using Microsoft.VisualBasic;
using SharedMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thundagun;

public class MaterialConnectorBase : ISharedMaterialConnector, ISharedMaterialPropertySetter
{
	public enum ActionType
	{
		Flag,
		Tag,
		Float4,
		Float,
		Float4Array,
		FloatArray,
		Matrix,
		Texture,
		RenderQueue,
		Instancing
	}
	public struct MaterialAction
	{
		public ActionType type;

		public int propertyIndex;

		public float4 float4Value;

		public object obj;

		public MaterialAction(ActionType type, int propertyIndex, in float4 float4Value, object obj = null)
		{
			this.type = type;
			this.propertyIndex = propertyIndex;
			this.float4Value = float4Value;
			this.obj = obj;
		}
	}
	public Asset Asset;
	public List<MaterialProperty> Properties = new();
	public Queue<MaterialAction> actionQueue = new();
	public RawValueList<float4x4> matrices = new();
	public static Queue<MaterialConnectorBase> initializingProperties = new();
	public static Queue<Action> onDoneActions = new();
	public static Queue<Action> markDoneActions = new();
	public bool Initialized;
	//public ulong ownerId;
	//public AssetIntegrated onDone;
	//public Action uploadMaterialAction;
	public void Initialize(Asset asset)
	{
		Asset = asset;
		//var owner = Asset?.Owner as IWorldElement;
		//ownerId = ((owner?.ReferenceID.Position ?? default) << 8) | ((owner?.ReferenceID.User ?? default) & 0xFFul);
		//var thing = new ApplyChangesMaterialConnector((MaterialConnector)this);
		//Thundagun.QueuePacket(thing);
	}

	public void InitializeProperties(List<MaterialProperty> properties, Action onDone)
	{



		var elem = Asset?.Owner as IWorldElement;
		//if (elem is null) 
		//{ 
		//onDone();
		//return;
		//}

		//UniLog.Log($"InitializeMaterialProperties: {elem?.ReferenceID.ToString() ?? "NULL"}, {Asset?.AssetURL?.LocalPath?.ToString() ?? "NULL"}");
		UniLog.Log($"InitializeMaterialProperties: {elem?.ReferenceID.ToString() ?? "NULL"} - {string.Join(',', properties.Select(p => p.Name))}");

		Properties = properties;

		initializingProperties.Enqueue(this);
		onDoneActions.Enqueue(() => 
		{
			onDone();
			Initialized = true;
			//var elem = Asset?.Owner as MaterialProvider;
			//UniLog.Log($"In on done action for mat {elem?.ReferenceID.ToString() ?? "NULL"}");
			//elem.World.RunInUpdates(30, () => 
			//{
			//	try
			//	{
			//		//UniLog.Log("1");
			//		foreach (var sm in elem.SyncMembers)
			//		{
			//			((SyncElement)sm).WasChanged = true;
			//		}
			//		//UniLog.Log("2");
			//		var method = elem.GetType().GetMethod("UpdateMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(elem, new object[] { Asset });
			//		//UniLog.Log("3");
			//		var mat = Asset as Material;
			//		//UniLog.Log("4");
			//		var shad = typeof(MaterialProvider).GetField("requestedVariantShader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(elem);
			//		//UniLog.Log("5");
			//		mat.Connector.ApplyChanges((Shader)shad, (bool b) => { });
			//		//UniLog.Log("6");
			//		if (this is MaterialConnector)
			//		{
			//			//var matConn = (MaterialConnector)this;
			//			//matConn.ShaderLocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";
			//			//try
			//			//{
			//			//	matConn.ShaderFilePath = ShaderConnector.LocalPathToFile[matConn.ShaderLocalPath];
			//			//}
			//			//catch (Exception e)
			//			//{
			//			//	matConn.ShaderFilePath = "NULL";
			//			//}
			//			//var owner = Asset?.Owner as IWorldElement;
			//			//matConn.ownerId = ((owner?.ReferenceID.Position ?? default) << 8) | ((owner?.ReferenceID.User ?? default) & 0xFFul);
			//			//UniLog.Log($"ApplyChangesMaterial: {matConn.ownerId}, Actions Count: {actionQueue?.Count ?? -1}, {matConn.ShaderLocalPath} {matConn.ShaderFilePath}");
			//			//Thundagun.QueuePacket(new ApplyChangesMaterialConnector((MaterialConnector)this));
			//		}
			//	}
			//	catch (Exception e)
			//	{
			//		UniLog.Error("owo error " + e.ToString());
			//	}
			//});
		});
		//markDoneActions.Enqueue(() => { Initialized = true; });

		Thundagun.QueueHighPriorityPacket(new InitializeMaterialPropertiesPacket(this));

		foreach (var prop in properties)
		{
			prop.Initialize(-1); // Needed?
		}

		onDone();
	}

	public void SetDebug(bool debug, string tag)
	{
	}

	public void SetFloat(int property, float value)
	{
		Enqueue(new MaterialAction(ActionType.Float, property, new float4(value)));
	}

	public void SetFloat4(int property, in float4 value)
	{
		Enqueue(new MaterialAction(ActionType.Float4, property, in value));
	}

	public void SetFloat4Array(int property, List<float4> values)
	{
		Enqueue(new MaterialAction(ActionType.Float4Array, property, float4.Zero, values));
	}

	public void SetFloatArray(int property, List<float> values)
	{
		Enqueue(new MaterialAction(ActionType.FloatArray, property, float4.Zero, values));
	}

	public void SetMatrix(int property, in float4x4 matrix)
	{
		Enqueue(new MaterialAction(ActionType.Matrix, property, new float4(StoreMatrix(in matrix)))); // never used in frooxengine, don't need to handle this
	}

	public void SetTexture(int property, ITexture texture)
	{
		Enqueue(new MaterialAction(ActionType.Texture, property, float4.Zero, texture));
	}

	public void Unload()
	{
	}

	void ISharedMaterialPropertySetter.SetFloat4(int property, in float4 value)
	{
		SetFloat4(property, in value);
	}

	void ISharedMaterialPropertySetter.SetMatrix(int property, in float4x4 matrix)
	{
		SetMatrix(property, in matrix);
	}

	private int StoreMatrix(in float4x4 matrix)
	{
		if (matrices == null)
		{
			matrices = Pool.BorrowRawValueList<float4x4>();
		}
		matrices.Add(in matrix);
		return matrices.Count - 1;
	}

	protected void Enqueue(in MaterialAction action)
	{
		if (actionQueue == null)
		{
			actionQueue = Pool.BorrowQueue<MaterialAction>();
		}
		actionQueue.Enqueue(action);

		//if (this is MaterialConnector)
			//Thundagun.QueuePacket(new ApplyChangesMaterialConnector((MaterialConnector)this));
	}
}

public class InitializeMaterialPropertiesPacket : UpdatePacket<MaterialConnectorBase>
{
	public List<string> PropertyNames;
	public List<int> PropertyIds;
	public InitializeMaterialPropertiesPacket(MaterialConnectorBase owner) : base(owner)
	{
		//PropertyNames = owner.Properties.Select(p => p.Name);
		PropertyNames = new();
		foreach (var prop in owner.Properties)
		{
			PropertyNames.Add(prop.Name);
		}
	}

	public override int Id => (int)PacketTypes.InitializeMaterialProperties;

	public override void Deserialize(CircularBuffer buffer)
	{
		int idCount;
		buffer.Read(out idCount);
		PropertyIds = new();
		for (int i = 0; i < idCount; i++)
		{
			int id;
			buffer.Read(out id);
			PropertyIds.Add(id);
		}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		int propCount = PropertyNames.Count;
		buffer.Write(ref propCount);
		foreach (var str in PropertyNames)
		{
			string newStr = str;
			if (newStr.Length > Thundagun.MAX_STRING_LENGTH)
				newStr = str.Substring(0, Math.Min(str.Length, Thundagun.MAX_STRING_LENGTH));
			buffer.WriteString(newStr);
		}
	}
}

//public class MaterialActionPacket : UpdatePacket<MaterialConnectorBase>
//{
//	MaterialConnectorBase.ActionType Type;
//	int PropertyIndex;
//	float4 Float4Value;
//	object Obj;
//	public MaterialActionPacket(MaterialConnectorBase owner, MaterialConnectorBase.ActionType type, int propertyIndex, float4 float4Value, object obj=null) : base(owner)
//	{
//		Type = type;
//		PropertyIndex = propertyIndex;
//		Float4Value = float4Value;
//		Obj = obj;
//	}

//	public override int Id => (int)PacketTypes.MaterialAction;

//	public override void Deserialize(CircularBuffer buffer)
//	{
//	}

//	public override void Serialize(CircularBuffer buffer)
//	{
//	}
//}
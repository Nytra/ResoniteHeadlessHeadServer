using Elements.Core;
using FrooxEngine;
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
	//public AssetIntegrated onDone;
	//public Action uploadMaterialAction;
	public void Initialize(Asset asset)
	{
		Asset = asset;
	}

	public void InitializeProperties(List<MaterialProperty> properties, Action onDone)
	{



		//var elem = Asset?.Owner as IWorldElement;
		//if (elem is null) 
		//{ 
			//onDone();
			//return;
		//}
		//UniLog.Log($"InitializeMaterialProperties: {elem.ReferenceID.ToString() ?? "NULL"}");

		Properties = properties;

		initializingProperties.Enqueue(this);
		onDoneActions.Enqueue(onDone);

		Thundagun.QueuePacket(new InitializeMaterialPropertiesPacket(this));



		//foreach (var prop in properties)
		//{
			//prop.Initialize(0);
		//}

		//onDone();
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
		Enqueue(new MaterialAction(ActionType.Matrix, property, new float4(StoreMatrix(in matrix)))); // need to handle StoreMatrix differently???
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
			buffer.WriteString(str);
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
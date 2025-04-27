using Elements.Core;
using FrooxEngine;
using SharedMemory;

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
	public void Initialize(Asset asset)
	{
		Asset = asset;
	}

	public void InitializeProperties(List<MaterialProperty> properties, Action onDone)
	{
		var elem = Asset?.Owner as IWorldElement;

		UniLog.Log($"InitializeMaterialProperties: {elem?.ReferenceID.ToString() ?? "NULL"} - {string.Join(',', properties.Select(p => p.Name))}");

		Properties = properties;

		initializingProperties.Enqueue(this);

		Thundagun.QueueHighPriorityPacket(new InitializeMaterialPropertiesPacket(this));

		foreach (var prop in properties)
		{
			prop.Initialize(-1); // Needed? Just so I can check on the other side if they are wrong...
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
	}
}

public class InitializeMaterialPropertiesPacket : UpdatePacket<MaterialConnectorBase>
{
	public List<string> PropertyNames;
	public List<int> PropertyIds;
	public InitializeMaterialPropertiesPacket(MaterialConnectorBase owner) : base(owner)
	{
		PropertyNames = new();
		foreach (var prop in owner.Properties)
		{
			PropertyNames.Add(prop.Name);
		}
	}

	public override int Id => (int)PacketTypes.InitializeMaterialProperties;

	public override void Serialize(BinaryWriter buffer)
	{
		int propCount = PropertyNames.Count;
		buffer.Write(propCount);
		foreach (var str in PropertyNames)
		{
			string newStr = str;
			if (newStr.Length > Thundagun.MAX_STRING_LENGTH)
				newStr = str.Substring(0, Math.Min(str.Length, Thundagun.MAX_STRING_LENGTH));
			buffer.WriteString2(newStr);
		}
	}

	public override void Deserialize(BinaryReader buffer)
	{
		int propCount;
		propCount = buffer.ReadInt32();
		PropertyIds = new();
		for (int i = 0; i < propCount; i++)
		{
			var id = buffer.ReadInt32();
			PropertyIds.Add(id);

		}
	}
}
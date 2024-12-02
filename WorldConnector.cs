using Elements.Core;
using FrooxEngine;
using System.Runtime.Serialization;
using UnityEngine;
//using UnityEngine;

namespace Thundagun.NewConnectors;

public class WorldConnector : IWorldConnector
{
	//public GameObject WorldRoot { get; set; }
	public World Owner { get; set; }

	public void Initialize(World owner)
	{
		Owner = owner;
		Thundagun.QueuePacket(new InitializeWorldConnector(this));
	}

	public void ChangeFocus(World.WorldFocus focus)
	{
		//Thundagun.QueuePacket(new ChangeFocusWorldConnector(this, focus));
	}

	public void Destroy()
	{
		//Thundagun.QueuePacket(new DestroyWorldConnector(this));
	}

	//public static void SetLayerRecursively(Transform transform, int layer)
	//{
	//	transform.gameObject.layer = layer;
	//	for (var index = 0; index < transform.childCount; ++index)
	//		SetLayerRecursively(transform.GetChild(index), layer);
	//}
}

public class InitializeWorldConnector : UpdatePacket<WorldConnector>
{
	public WorldManagerConnector connector;

	public InitializeWorldConnector(WorldConnector owner) : base(owner)
	{
		connector = owner.Owner.WorldManager.Connector as WorldManagerConnector;
	}

	//public override void Update()
	//{
	//	Owner.WorldRoot = new GameObject("World");
	//	Owner.WorldRoot.SetActive(false);
	//	Owner.WorldRoot.transform.SetParent(connector.Root.transform);
	//	Owner.WorldRoot.transform.position = Vector3.zero;
	//	Owner.WorldRoot.transform.rotation = Quaternion.identity;
	//	Owner.WorldRoot.transform.localScale = Vector3.one;
	//}

	public override void Serialize(BinaryWriter bw)
	{
	}
}
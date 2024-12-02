using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
//using MsgPack.Serialization;
//using MsgPack;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Thundagun.NewConnectors;

public class SlotConnector : Connector<Slot>, ISlotConnector
{
	public bool Active;
	public byte ForceLayer;
	public ushort GameObjectRequests;
	public SlotConnector ParentConnector;
	//public Vector3 Position;
	//public Quaternion Rotation;
	public bool ShouldDestroy;
	//public UnityEngine.Transform Transform;

	public WorldConnector WorldConnector => (WorldConnector)World.Connector;

	//public GameObject GeneratedGameObject { get; private set; }

	//public int Layer => GeneratedGameObject == null ? 0 : GeneratedGameObject.layer;

	public override void Initialize()
	{
		UniLog.Log("Slot connector init");
		//ParentConnector = Owner.Parent?.Connector as SlotConnector;
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this, !Owner.IsRootSlot));
	}

	public override void ApplyChanges()
	{
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		//Thundagun.QueuePacket(new DestroySlotConnector(this, destroyingWorld));
	}

	public static IConnector<Slot> Constructor()
	{
		return new SlotConnector();
	}

	//public GameObject ForceGetGameObject()
	//{
	//	if (GeneratedGameObject == null)
	//		GenerateGameObject();
	//	return GeneratedGameObject;
	//}

	//public GameObject RequestGameObject()
	//{
	//	GameObjectRequests++;
	//	return ForceGetGameObject();
	//}

	public void FreeGameObject()
	{
		GameObjectRequests--;
		//TryDestroy();
	}

	//public void TryDestroy(bool destroyingWorld = false)
	//{
	//	if (!ShouldDestroy || GameObjectRequests != 0)
	//		return;
	//	if (!destroyingWorld)
	//	{
	//		if (GeneratedGameObject) UnityEngine.Object.Destroy(GeneratedGameObject);
	//		ParentConnector?.FreeGameObject();
	//	}

	//	GeneratedGameObject = null;
	//	Transform = null;
	//	ParentConnector = null;
	//}

	//private void GenerateGameObject()
	//{
	//	GeneratedGameObject = new GameObject("");
	//	Transform = GeneratedGameObject.transform;
	//	UpdateParent();
	//	UpdateLayer();
	//	SetData();
	//}

	//private void UpdateParent()
	//{
	//	var gameObject = ParentConnector != null ? ParentConnector.RequestGameObject() : WorldConnector.WorldRoot;
	//	Transform.SetParent(gameObject.transform, false);
	//}

	//public void UpdateLayer()
	//{
	//	var layer = ForceLayer <= 0 ? Transform.parent.gameObject.layer : ForceLayer;
	//	if (layer == GeneratedGameObject.layer)
	//		return;
	//	SetHiearchyLayer(GeneratedGameObject, layer);
	//}

	//public static void SetHiearchyLayer(GameObject root, int layer)
	//{
	//	root.layer = layer;
	//	for (var index = 0; index < root.transform.childCount; ++index)
	//		SetHiearchyLayer(root.transform.GetChild(index).gameObject, layer);
	//}

	//public void SetData()
	//{
	//	GeneratedGameObject.SetActive(Active);
	//	var transform = Transform;
	//	transform.localPosition = Position;
	//	transform.localRotation = Rotation;
	//	transform.localScale = Scale;
	//}
}

public class ApplyChangesSlotConnector : UpdatePacket<SlotConnector>
{
	public bool Active;
	public Vector3 Position;
	public Vector3 Rotation;
	public Vector3 Scale;

	public ApplyChangesSlotConnector(SlotConnector owner, bool forceReparent) : base(owner)
	{
		var o = owner.Owner;
		Active = o.ActiveSelf;
		Rotation = o.Rotation_Field.Value.EulerAngles.ToUnity();
		Scale = o.Scale_Field.Value.ToUnity();
		Position = o.Position_Field.Value.ToUnity();
	}

	public ApplyChangesSlotConnector(SlotConnector owner) : base(owner)
	{
		var o = owner.Owner;
		Active = o.ActiveSelf;
		Position = o.Position_Field.Value.ToUnity();
		Rotation = o.Rotation_Field.Value.EulerAngles.ToUnity();
		Scale = o.Scale_Field.Value.ToUnity();
	}
	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(Active);

		bw.Write(Position.x);
		bw.Write(Position.y);
		bw.Write(Position.z);

		bw.Write(Rotation.x);
		bw.Write(Rotation.y);
		bw.Write(Rotation.z);

		bw.Write(Scale.x);
		bw.Write(Scale.y);
		bw.Write(Scale.z);
	}
}

//public class DestroySlotConnector : UpdatePacket<SlotConnector>
//{
//	public bool DestroyingWorld;

//	public DestroySlotConnector(SlotConnector owner, bool destroyingWorld) : base(owner)
//	{
//		DestroyingWorld = destroyingWorld;
//	}

//	public override void Update()
//	{
//		Owner.ShouldDestroy = true;
//		UniLog.Log("Destroy slot connector");
//		//Owner.TryDestroy(DestroyingWorld);
//	}
//}

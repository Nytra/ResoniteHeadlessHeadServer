using Elements.Core;
using FrooxEngine;
using UnityEngine;

namespace Thundagun.NewConnectors;

public class SlotConnector : Connector<Slot>, ISlotConnector
{
	public SlotConnector ParentConnector;
	public ulong RefID;

	public WorldConnector WorldConnector => (WorldConnector)World.Connector;

	public override void Initialize()
	{
		UniLog.Log("Slot connector initialize");
		RefID = Owner.ReferenceID.Position;
		ParentConnector = Owner.Parent?.Connector as SlotConnector;
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this, !Owner.IsRootSlot));
	}

	public override void ApplyChanges()
	{
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		UniLog.Log("Slot connector destroy");
		Thundagun.QueuePacket(new DestroySlotConnector(this, destroyingWorld));
	}

	public static IConnector<Slot> Constructor()
	{
		return new SlotConnector();
	}
}

public class ApplyChangesSlotConnector : UpdatePacket<SlotConnector>
{
	public bool Active;
	public Vector3 Position;
	public Vector3 Rotation;
	public Vector3 Scale;
	public ulong refId;
	public ulong newParentId;
	public bool Reparent;
	public bool HasRenderer;
	public ApplyChangesSlotConnector(SlotConnector owner, bool forceReparent) : base(owner)
	{
		var o = owner.Owner;
		var parent = o.Parent;
		Active = o.ActiveSelf;
		Rotation = o.Rotation_Field.Value.EulerAngles.ToUnity();
		Scale = o.Scale_Field.Value.ToUnity();
		Position = o.Position_Field.Value.ToUnity();
		refId = owner.RefID;
		if ((parent != null && parent.Connector != owner.ParentConnector) || forceReparent)
		{
			Reparent = true;
			newParentId = o.Parent.ReferenceID.Position;
		}
		var skinnedMesh = owner.Owner.GetComponentInParents<FrooxEngine.SkinnedMeshRenderer>();
		if (skinnedMesh != null && skinnedMesh.Bones.Contains(owner.Owner))
		{
			HasRenderer = true;
		}
		else
		{
			HasRenderer = owner.Owner.GetComponent<FrooxEngine.MeshRenderer>() != null;
		}
	}

	public ApplyChangesSlotConnector(SlotConnector owner) : base(owner)
	{
		var o = owner.Owner;
		var parent = o.Parent;
		Active = o.ActiveSelf;
		Position = o.Position_Field.Value.ToUnity();
		Rotation = o.Rotation_Field.Value.EulerAngles.ToUnity();
		Scale = o.Scale_Field.Value.ToUnity();
		refId = owner.RefID;
		if (parent != null && parent.Connector != owner.ParentConnector)
		{
			Reparent = true;
			newParentId = o.Parent.ReferenceID.Position;
		}
		var skinnedMesh = owner.Owner.GetComponentInParents<FrooxEngine.SkinnedMeshRenderer>();
		if (skinnedMesh != null && skinnedMesh.Bones.Contains(owner.Owner))
		{
			HasRenderer = true;
		}
		else
		{
			HasRenderer = owner.Owner.GetComponent<FrooxEngine.MeshRenderer>() != null;
		}
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

		bw.Write(refId);

		bw.Write(Reparent);
		bw.Write(newParentId);

		bw.Write(HasRenderer);
	}
}

public class DestroySlotConnector : UpdatePacket<SlotConnector>
{
	public ulong RefID;

	public DestroySlotConnector(SlotConnector owner, bool destroyingWorld) : base(owner)
	{
		RefID = owner.RefID;
	}

	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(RefID);
	}
}
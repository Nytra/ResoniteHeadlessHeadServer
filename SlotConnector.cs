using Elements.Core;
using FrooxEngine;
using UnityEngine;

namespace Thundagun.NewConnectors;

public class SlotConnector : Connector<Slot>, ISlotConnector
{
	public ulong RefID;
	public SlotConnector ParentConnector;
	public WorldConnector WorldConnector => (WorldConnector)World.Connector;
	public long WorldId;

	public override void Initialize()
	{
		//UniLog.Log("Slot connector initialize");
		RefID = Owner.ReferenceID.Position;
		ParentConnector = Owner.Parent?.Connector as SlotConnector;
		WorldId = Owner.World.LocalWorldHandle;
		//Thundagun.QueuePacket(new ApplyChangesSlotConnector(this, !Owner.IsRootSlot));
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
	}

	public override void ApplyChanges()
	{
		//RefID = Owner.ReferenceID.Position;
		//WorldId = Owner.World.LocalWorldHandle;
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
		//UniLog.Log("Slot connector destroy");
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
	public bool ActiveChanged;
	public Vector3 Position;
	public bool PositionChanged;
	public Vector3 Rotation;
	public bool RotationChanged;
	public Vector3 Scale;
	public bool ScaleChanged;
	public ulong RefId;
	public ulong ParentRefId;
	public bool HasParent;
	public bool IsRootSlot;
	public bool Reparent;
	public string SlotName;
	public long WorldId;
	public World World;

	public ApplyChangesSlotConnector(SlotConnector owner, bool forceReparent) : base(owner)
	{
		var o = owner.Owner;
		var parent = o.Parent;
		Active = o.ActiveSelf;
		ActiveChanged = o.ActiveSelf_Field.GetWasChangedAndClear();
		Position = o.Position_Field.Value.ToUnity();
		PositionChanged = o.Position_Field.GetWasChangedAndClear();
		Rotation = o.Rotation_Field.Value.EulerAngles.ToUnity();
		RotationChanged = o.Rotation_Field.GetWasChangedAndClear();
		Scale = o.Scale_Field.Value.ToUnity();
		ScaleChanged = o.Scale_Field.GetWasChangedAndClear();
		RefId = owner.RefID;
		ParentRefId = o.Parent?.ReferenceID.Position ?? default;
		HasParent = parent != null;
		IsRootSlot = o.IsRootSlot;
		if ((parent?.Connector != owner.ParentConnector && parent != null) || forceReparent)
		{
			owner.ParentConnector = parent?.Connector as SlotConnector;
			Reparent = true;
		}
		SlotName = o.Name;
		WorldId = owner.WorldId;
		World = owner.World;
	}

	public ApplyChangesSlotConnector(SlotConnector owner) : base(owner)
	{
		var o = owner.Owner;
		var parent = o.Parent;
		Active = o.ActiveSelf;
		ActiveChanged = o.ActiveSelf_Field.GetWasChangedAndClear();
		Position = o.Position_Field.Value.ToUnity();
		PositionChanged = o.Position_Field.GetWasChangedAndClear();
		Rotation = o.Rotation_Field.Value.EulerAngles.ToUnity();
		RotationChanged = o.Rotation_Field.GetWasChangedAndClear();
		Scale = o.Scale_Field.Value.ToUnity();
		ScaleChanged = o.Scale_Field.GetWasChangedAndClear();
		RefId = owner.RefID;
		ParentRefId = o.Parent?.ReferenceID.Position ?? default;
		HasParent = parent != null;
		IsRootSlot = o.IsRootSlot;
		if (parent?.Connector != owner.ParentConnector && parent != null)
		{
			owner.ParentConnector = parent?.Connector as SlotConnector;
			Reparent = true;
		}
		SlotName = o.Name;
		WorldId = owner.WorldId;
		World = owner.World;
	}

	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(Active);
		bw.Write(ActiveChanged);

		bw.Write(Position.x);
		bw.Write(Position.y);
		bw.Write(Position.z);
		bw.Write(PositionChanged);

		bw.Write(Rotation.x);
		bw.Write(Rotation.y);
		bw.Write(Rotation.z);
		bw.Write(RotationChanged);

		bw.Write(Scale.x);
		bw.Write(Scale.y);
		bw.Write(Scale.z);
		bw.Write(ScaleChanged);

		bw.Write(RefId);

		bw.Write(ParentRefId);

		bw.Write(HasParent);

		bw.Write(IsRootSlot);

		bw.Write(Reparent);

		bw.Write(SlotName ?? "NULL");

		bw.Write(WorldId);
	}
	public override void Deserialize(BinaryReader br)
	{
		Active = br.ReadBoolean();
		ActiveChanged = br.ReadBoolean();

		float px = br.ReadSingle();
		float py = br.ReadSingle();
		float pz = br.ReadSingle();
		Position = new Vector3(px, py, pz);
		PositionChanged = br.ReadBoolean();

		float rx = br.ReadSingle();
		float ry = br.ReadSingle();
		float yz = br.ReadSingle();
		Rotation = new Vector3(rx, ry, yz);
		RotationChanged = br.ReadBoolean();

		float sx = br.ReadSingle();
		float sy = br.ReadSingle();
		float sz = br.ReadSingle();
		Scale = new Vector3(sx, sy, sz);
		ScaleChanged = br.ReadBoolean();

		RefId = br.ReadUInt64();

		ParentRefId = br.ReadUInt64();

		HasParent = br.ReadBoolean();

		IsRootSlot = br.ReadBoolean();

		Reparent = br.ReadBoolean();

		SlotName = br.ReadString();

		WorldId = br.ReadInt64();
	}
	public override string ToString()
	{
		return $"ApplyChangesSlotConnector: {Active} {Position} {PositionChanged} {Rotation} {RotationChanged} {Scale} {ScaleChanged} {RefId} {ParentRefId} {HasParent} {IsRootSlot} {Reparent} {SlotName} {WorldId}";
	}
}

public class DestroySlotConnector : UpdatePacket<SlotConnector>
{
	public ulong RefID;
	public bool DestroyingWorld;
	public long WorldId;
	public World World;

	public DestroySlotConnector(SlotConnector owner, bool destroyingWorld) : base(owner)
	{
		RefID = owner.RefID;
		DestroyingWorld = destroyingWorld;
		WorldId = owner.WorldId;
		World = owner.World;
	}

	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(RefID);
		bw.Write(DestroyingWorld);
		bw.Write(WorldId);
	}
	public override void Deserialize(BinaryReader br)
	{
		RefID = br.ReadUInt64();
		DestroyingWorld = br.ReadBoolean();
		WorldId = br.ReadInt64();
	}
	public override string ToString()
	{
		return $"DestroySlotConnector: {RefID} {DestroyingWorld} {WorldId}";
	}
}
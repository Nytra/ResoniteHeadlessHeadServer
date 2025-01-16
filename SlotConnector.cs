using Elements.Core;
using FrooxEngine;
using SharedMemory;
using UnityEngine;

namespace Thundagun.NewConnectors;

public class SlotConnector : Connector<Slot>, ISlotConnector
{
	public ulong RefID;
	public SlotConnector ParentConnector;
	public WorldConnector WorldConnector => (WorldConnector)World.Connector;
	public long WorldId;
	//public byte ForceLayer; // not needed yet

	public override void Initialize()
	{
		//UniLog.Log("Slot connector initialize");
		RefID = Owner.ReferenceID.Position;
		ParentConnector = Owner.Parent?.Connector as SlotConnector;
		WorldId = Owner.World.LocalWorldHandle;
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this, !Owner.IsRootSlot));
		//Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
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
	public override int Id => (int)PacketTypes.ApplyChangesSlot;

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
		SlotName = o.Name ?? "NULL";
		WorldId = owner.WorldId;
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref Active);
		buffer.Write(ref ActiveChanged);

		buffer.Write(ref Position.x);
		buffer.Write(ref Position.y);
		buffer.Write(ref Position.z);
		buffer.Write(ref PositionChanged);

		buffer.Write(ref Rotation.x);
		buffer.Write(ref Rotation.y);
		buffer.Write(ref Rotation.z);
		buffer.Write(ref RotationChanged);

		buffer.Write(ref Scale.x);
		buffer.Write(ref Scale.y);
		buffer.Write(ref Scale.z);
		buffer.Write(ref ScaleChanged);

		buffer.Write(ref RefId);

		buffer.Write(ref ParentRefId);

		buffer.Write(ref HasParent);

		buffer.Write(ref IsRootSlot);

		buffer.Write(ref Reparent);

		//buffer.Write(ref SlotName);

		buffer.Write(ref WorldId);
	}
	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out Active);
		buffer.Read(out ActiveChanged);

		float px, py, pz;
		buffer.Read(out px);
		buffer.Read(out py);
		buffer.Read(out pz);
		Position = new Vector3(px, py, pz);
		buffer.Read(out PositionChanged);

		float rx, ry, rz;
		buffer.Read(out rx);
		buffer.Read(out ry);
		buffer.Read(out rz);
		Rotation = new Vector3(rx, ry, rz);
		buffer.Read(out RotationChanged);

		float sx, sy, sz;
		buffer.Read(out sx);
		buffer.Read(out sy);
		buffer.Read(out sz);
		Scale = new Vector3(sx, sy, sz);
		buffer.Read(out ScaleChanged);

		buffer.Read(out RefId);

		buffer.Read(out ParentRefId);

		buffer.Read(out HasParent);

		buffer.Read(out IsRootSlot);

		buffer.Read(out Reparent);

		//SlotName = br.ReadString();

		buffer.Read(out WorldId);
	}
	public override string ToString()
	{
		return $"ApplyChangesSlotConnector: {Active} {Position} {PositionChanged} {Rotation} {RotationChanged} {Scale} {ScaleChanged} {RefId} {ParentRefId} {HasParent} {IsRootSlot} {Reparent} {SlotName} {WorldId}";
	}
}

public class DestroySlotConnector : UpdatePacket<SlotConnector>
{
	public override int Id => (int)PacketTypes.DestroySlot;

	public ulong RefID;
	public bool DestroyingWorld;
	public long WorldId;

	public DestroySlotConnector(SlotConnector owner, bool destroyingWorld) : base(owner)
	{
		RefID = owner.RefID;
		DestroyingWorld = destroyingWorld;
		WorldId = owner.WorldId;
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref RefID);
		buffer.Write(ref DestroyingWorld);
		buffer.Write(ref WorldId);
	}
	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out RefID);
		buffer.Read(out DestroyingWorld);
		buffer.Read(out WorldId);
	}
	public override string ToString()
	{
		return $"DestroySlotConnector: {RefID} {DestroyingWorld} {WorldId}";
	}
}
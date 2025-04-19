using Elements.Core;
using FrooxEngine;
using FrooxEngine.PhotonDust;
using FrooxEngine.UIX;
using SharedMemory;
using System.Text;
using Elements.Assets;

namespace Thundagun;

public class SlotConnector : Connector<Slot>, ISlotConnector
{
	public ulong RefID;
	public WorldConnector WorldConnector => (WorldConnector)World.Connector;
	public long WorldId;
	public bool ForceShowDebugVisuals;

	public override void Initialize()
	{
		RefID = (Owner.ReferenceID.Position << 8) | (Owner.ReferenceID.User & 0xFFul);
		WorldId = Owner.World.LocalWorldHandle;
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
	}

	public override void ApplyChanges()
	{
		//RefID = (Owner.ReferenceID.Position << 8) | (Owner.ReferenceID.User & 0xFFul);
		//WorldId = Owner.World.LocalWorldHandle;
		Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
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
	public float3 Position;
	public bool PositionChanged;
	public floatQ Rotation;
	public bool RotationChanged;
	public float3 Scale;
	public bool ScaleChanged;
	public ulong RefId;
	public ulong ParentRefId;
	public bool HasParent;
	public bool IsRootSlot;
	public bool Reparent;
	public string SlotName;
	public long WorldId;
	public bool IsUserRootSlot;
	public bool HasActiveUser;
	public bool ShowDebugVisuals;
	public bool IsLocalElement;

	public ApplyChangesSlotConnector(SlotConnector owner) : base(owner)
	{
		var o = owner.Owner;
		var parent = o.Parent;
		Active = o.ActiveSelf;
		ActiveChanged = o.ActiveSelf_Field.GetWasChangedAndClear();
		Position = o.Position_Field.Value;
		PositionChanged = o.Position_Field.GetWasChangedAndClear();
		Rotation = o.Rotation_Field.Value;
		RotationChanged = o.Rotation_Field.GetWasChangedAndClear();
		Scale = o.Scale_Field.Value;
		ScaleChanged = o.Scale_Field.GetWasChangedAndClear();
		RefId = owner.RefID;
		ParentRefId = ((o.Parent?.ReferenceID.Position ?? default) << 8) | ((o.Parent?.ReferenceID.User ?? default) & 0xFFul);
		HasParent = parent != null;
		IsRootSlot = o.IsRootSlot;
		IsLocalElement = o.IsLocalElement;

		if (!string.IsNullOrEmpty(o.Name))
		{
			SlotName = new StringRenderTree(o.Name).GetRawString();
			if (SlotName.Length > Thundagun.MAX_STRING_LENGTH)
				SlotName = SlotName.Substring(0, Math.Min(SlotName.Length, Thundagun.MAX_STRING_LENGTH));
		}
		else
		{
			if (o.Name == null)
				SlotName = "NULL";
			else
				SlotName = o.Name;
		}

		WorldId = owner.WorldId;
		IsUserRootSlot = o.ActiveUserRoot?.Slot == o;
		HasActiveUser = o.ActiveUser != null;
		ShowDebugVisuals = o.GetComponent<Canvas>() != null ||
			o.GetComponent<ParticleSystem>() != null ||
			o.GetComponent<Light>() != null ||
			o.GetComponent<ReflectionProbe>() != null ||
			owner.ForceShowDebugVisuals;
	}

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.Write(Active);
		buffer.Write(ActiveChanged);

		float px = Position.x;
		float py = Position.y;
		float pz = Position.z;
		buffer.Write(px);
		buffer.Write(py);
		buffer.Write(pz);
		buffer.Write(PositionChanged);

		float rx = Rotation.x;
		float ry = Rotation.y;
		float rz = Rotation.z;
		float rw = Rotation.w;
		buffer.Write(rx);
		buffer.Write(ry);
		buffer.Write(rz);
		buffer.Write(rw);
		buffer.Write(RotationChanged);

		float sx = Scale.x;
		float sy = Scale.y;
		float sz = Scale.z;
		buffer.Write(sx);
		buffer.Write(sy);
		buffer.Write(sz);
		buffer.Write(ScaleChanged);

		buffer.Write(RefId);

		buffer.Write(ParentRefId);

		buffer.Write(HasParent);

		buffer.Write(IsRootSlot);

		buffer.Write(Reparent);

		buffer.WriteString2(SlotName);

		buffer.Write(WorldId);

		buffer.Write(IsUserRootSlot);

		buffer.Write(HasActiveUser);

		buffer.Write(ShowDebugVisuals);

		buffer.Write(IsLocalElement);
	}

	public override string ToString()
	{
		return $"ApplyChangesSlotConnector: {Active} {ActiveChanged} {Position} {PositionChanged} {Rotation} {RotationChanged} {Scale} {ScaleChanged} {RefId} {ParentRefId} {HasParent} {IsRootSlot} {Reparent} {SlotName} {WorldId} {IsUserRootSlot} {HasActiveUser} {ShowDebugVisuals}";
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

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.Write(RefID);
		buffer.Write(DestroyingWorld);
		buffer.Write(WorldId);
	}

	public override string ToString()
	{
		return $"DestroySlotConnector: {RefID} {DestroyingWorld} {WorldId}";
	}
}
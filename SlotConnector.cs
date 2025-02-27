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
	public bool ForceRender;

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
	public bool ShouldRender;
	public bool ForceRender;
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
			SlotName = SlotName.Substring(0, Math.Min(SlotName.Length, Thundagun.MAX_STRING_LENGTH));
		}
		else
		{
			SlotName = "NULL";
		}

		WorldId = owner.WorldId;
		IsUserRootSlot = o.ActiveUserRoot?.Slot == o;
		HasActiveUser = o.ActiveUser != null;
		ShouldRender = o.GetComponent<MeshRenderer>() != null ||
			o.GetComponent<Canvas>() != null ||
			o.GetComponent<ParticleSystem>() != null ||
			o.GetComponent<Light>() != null ||
			o.GetComponent<ReflectionProbe>() != null; // also gets SkinnedMeshRenderer since it is inherited
		ForceRender = owner.ForceRender;

		//if (o.GetComponent<SkinnedMeshRenderer>() is SkinnedMeshRenderer skinned)
		//{
		//	foreach (var bone in skinned.Bones)
		//	{
		//		var slotConn = bone.Connector as SlotConnector;
		//		if (!slotConn.ForceRender)
		//		{
		//			slotConn.ForceRender = true;
		//			Thundagun.QueuePacket(new ApplyChangesSlotConnector(slotConn));
		//		}
		//	}
		//}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref Active);
		buffer.Write(ref ActiveChanged);

		float px = Position.x;
		float py = Position.y;
		float pz = Position.z;
		buffer.Write(ref px);
		buffer.Write(ref py);
		buffer.Write(ref pz);
		buffer.Write(ref PositionChanged);

		float rx = Rotation.x;
		float ry = Rotation.y;
		float rz = Rotation.z;
		float rw = Rotation.w;
		buffer.Write(ref rx);
		buffer.Write(ref ry);
		buffer.Write(ref rz);
		buffer.Write(ref rw);
		buffer.Write(ref RotationChanged);

		float sx = Scale.x;
		float sy = Scale.y;
		float sz = Scale.z;
		buffer.Write(ref sx);
		buffer.Write(ref sy);
		buffer.Write(ref sz);
		buffer.Write(ref ScaleChanged);

		buffer.Write(ref RefId);

		buffer.Write(ref ParentRefId);

		buffer.Write(ref HasParent);

		buffer.Write(ref IsRootSlot);

		buffer.Write(ref Reparent);

		buffer.Write(Encoding.UTF8.GetBytes(SlotName));

		buffer.Write(ref WorldId);

		buffer.Write(ref IsUserRootSlot);

		buffer.Write(ref HasActiveUser);

		buffer.Write(ref ShouldRender);

		buffer.Write(ref ForceRender);

		buffer.Write(ref IsLocalElement);
	}
	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out Active);
		buffer.Read(out ActiveChanged);

		float px, py, pz;
		buffer.Read(out px);
		buffer.Read(out py);
		buffer.Read(out pz);
		Position = new float3(px, py, pz);
		buffer.Read(out PositionChanged);

		float rx, ry, rz, rw;
		buffer.Read(out rx);
		buffer.Read(out ry);
		buffer.Read(out rz);
		buffer.Read(out rw);
		Rotation = new floatQ(rx, ry, rz, rw);
		buffer.Read(out RotationChanged);

		float sx, sy, sz;
		buffer.Read(out sx);
		buffer.Read(out sy);
		buffer.Read(out sz);
		Scale = new float3(sx, sy, sz);
		buffer.Read(out ScaleChanged);

		buffer.Read(out RefId);

		buffer.Read(out ParentRefId);

		buffer.Read(out HasParent);

		buffer.Read(out IsRootSlot);

		buffer.Read(out Reparent);

		var bytes = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes);
		SlotName = Encoding.UTF8.GetString(bytes);

		buffer.Read(out WorldId);

		buffer.Read(out IsUserRootSlot);

		buffer.Read(out HasActiveUser);

		buffer.Read(out ShouldRender);

		buffer.Read(out ForceRender);

		buffer.Read(out IsLocalElement);
	}
	public override string ToString()
	{
		return $"ApplyChangesSlotConnector: {Active} {ActiveChanged} {Position} {PositionChanged} {Rotation} {RotationChanged} {Scale} {ScaleChanged} {RefId} {ParentRefId} {HasParent} {IsRootSlot} {Reparent} {SlotName} {WorldId} {IsUserRootSlot} {HasActiveUser} {ShouldRender} {ForceRender}";
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
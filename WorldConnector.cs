using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Runtime.Serialization;

namespace Thundagun.NewConnectors;

public class WorldConnector : IWorldConnector
{
	public World Owner { get; set; }
	public long WorldId;

	public void Initialize(World owner)
	{
		Owner = owner;
		WorldId = owner.LocalWorldHandle;
		Thundagun.QueuePacket(new InitializeWorldConnector(this));
	}

	public void ChangeFocus(World.WorldFocus focus)
	{
		//WorldId = Owner.LocalWorldHandle;
		Thundagun.QueuePacket(new ChangeFocusWorldConnector(this, focus));
	}

	public void Destroy()
	{
		Thundagun.QueuePacket(new DestroyWorldConnector(this));
	}
}

public class InitializeWorldConnector : UpdatePacket<WorldConnector>
{
	public override int Id => (int)PacketTypes.InitializeWorld;

	//public WorldManagerConnector connector;
	public long WorldId;

	public InitializeWorldConnector(WorldConnector owner) : base(owner)
	{
		//connector = owner.Owner.WorldManager.Connector as WorldManagerConnector;
		WorldId = owner.WorldId;
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref WorldId);
	}
	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out WorldId);
	}
	public override string ToString()
	{
		return $"InitializeWorldConnector: {WorldId}";
	}
}

public class ChangeFocusWorldConnector : UpdatePacket<WorldConnector>
{
	public override int Id => (int)PacketTypes.ChangeFocusWorld;

	public int Focus;
	public long WorldId;

	public ChangeFocusWorldConnector(WorldConnector owner, World.WorldFocus focus) : base(owner)
	{
		Focus = (int)focus;
		WorldId = owner.WorldId;
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref Focus);
		buffer.Write(ref WorldId);
	}
	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out Focus);
		buffer.Read(out WorldId);
	}
	public override string ToString()
	{
		return $"ChangeFocusWorldConnector: {Focus} {WorldId}";
	}
}

public class DestroyWorldConnector : UpdatePacket<WorldConnector>
{
	public override int Id => (int)PacketTypes.DestroyWorld;

	public long WorldId;
	public DestroyWorldConnector(WorldConnector owner) : base(owner)
	{
		WorldId = owner.WorldId;
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(ref WorldId);
	}
	public override void Deserialize(CircularBuffer buffer)
	{
		buffer.Read(out WorldId);
	}
	public override string ToString()
	{
		return $"DestroyWorldConnector: {WorldId}";
	}
}
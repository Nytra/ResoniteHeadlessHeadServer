using Elements.Core;
using FrooxEngine;
using System.Runtime.Serialization;
using UnityEngine;

namespace Thundagun.NewConnectors;

public class WorldConnector : IWorldConnector
{
	public World Owner { get; set; }
	public long WorldId;

	public void Initialize(World owner)
	{
		Owner = owner;
		WorldId = owner.LocalWorldHandle;
		UniLog.Log("World connector initialize");
		Thundagun.QueuePacket(new InitializeWorldConnector(this));
	}

	public void ChangeFocus(World.WorldFocus focus)
	{
		WorldId = Owner.LocalWorldHandle;
		UniLog.Log("World connector change focus");
		Thundagun.QueuePacket(new ChangeFocusWorldConnector(this, focus));
	}

	public void Destroy()
	{
		UniLog.Log("World connector destroy");
		Thundagun.QueuePacket(new DestroyWorldConnector(this));
	}
}

public class InitializeWorldConnector : UpdatePacket<WorldConnector>
{
	//public WorldManagerConnector connector;
	public long WorldId;

	public InitializeWorldConnector(WorldConnector owner) : base(owner)
	{
		//connector = owner.Owner.WorldManager.Connector as WorldManagerConnector;

		// ID: owner.Owner.LocalWorldHandle; (ulong)
		WorldId = owner.WorldId;
	}

	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(WorldId);
	}
	public override void Deserialize(BinaryReader br)
	{
		WorldId = br.ReadInt64();
	}
}

public class ChangeFocusWorldConnector : UpdatePacket<WorldConnector>
{
	public int Focus;
	public long WorldId;

	public ChangeFocusWorldConnector(WorldConnector owner, World.WorldFocus focus) : base(owner)
	{
		Focus = (int)focus;
		WorldId = owner.WorldId;
	}

	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(Focus);
		bw.Write(WorldId);
	}
	public override void Deserialize(BinaryReader br)
	{
		Focus = br.ReadInt32();
		WorldId = br.ReadInt64();
	}
}

public class DestroyWorldConnector : UpdatePacket<WorldConnector>
{
	public long WorldId;
	public DestroyWorldConnector(WorldConnector owner) : base(owner)
	{
		WorldId = owner.WorldId;
	}

	public override void Serialize(BinaryWriter bw)
	{
		bw.Write(WorldId);
	}
	public override void Deserialize(BinaryReader br)
	{
		WorldId = br.ReadInt64();
	}
}
using Elements.Core;
using FrooxEngine;
using System.Runtime.Serialization;
using UnityEngine;

namespace Thundagun.NewConnectors;

public class WorldConnector : IWorldConnector
{
	public World Owner { get; set; }

	public void Initialize(World owner)
	{
		Owner = owner;
		UniLog.Log("World connector initialize");
		Thundagun.QueuePacket(new InitializeWorldConnector(this));
	}

	public void ChangeFocus(World.WorldFocus focus)
	{
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
	public WorldManagerConnector connector;

	public InitializeWorldConnector(WorldConnector owner) : base(owner)
	{
		connector = owner.Owner.WorldManager.Connector as WorldManagerConnector;
	}

	public override void Serialize(BinaryWriter bw)
	{
	}
	public override void Deserialize(BinaryReader bw)
	{
	}
}

public class ChangeFocusWorldConnector : UpdatePacket<WorldConnector>
{
	public World.WorldFocus Focus;

	public ChangeFocusWorldConnector(WorldConnector owner, World.WorldFocus focus) : base(owner)
	{
		Focus = focus;
	}

	public override void Serialize(BinaryWriter bw)
	{
	}
	public override void Deserialize(BinaryReader bw)
	{
	}
}

public class DestroyWorldConnector : UpdatePacket<WorldConnector>
{
	public DestroyWorldConnector(WorldConnector owner) : base(owner)
	{
	}

	public override void Serialize(BinaryWriter bw)
	{
	}
	public override void Deserialize(BinaryReader bw)
	{
	}
}
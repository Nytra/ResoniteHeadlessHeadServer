using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thundagun;

public class SkyboxConnector : Connector<Skybox>
{
	public override void ApplyChanges()
	{
		if (Owner.ActiveSkybox && World.Focus == World.WorldFocus.Focused)
		{
			Thundagun.QueuePacket(new ApplyChangesSkyboxConnector(this));
		}
	}

	public override void Destroy(bool destroyingWorld)
	{
	}

	public override void Initialize()
	{
	}
}

public class ApplyChangesSkyboxConnector : UpdatePacket<SkyboxConnector>
{
	public ulong matId;
	public ApplyChangesSkyboxConnector(SkyboxConnector owner) : base(owner)
	{
		var elem = owner.Owner.Material;
		matId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
	}

	public override int Id => (int)PacketTypes.ApplyChangesSkybox;

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.Write(matId);
	}
}
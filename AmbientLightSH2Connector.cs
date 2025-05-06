using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thundagun;

public class AmbientLightSH2Connector : Connector<AmbientLightSH2>
{
	public override void ApplyChanges()
	{
		if (base.Owner.ShouldBeActive)
		{
			Thundagun.QueuePacket(new ApplyChangesAmbientLightSH2Connector(this));
		}
	}

	public override void Destroy(bool destroyingWorld)
	{
	}

	public override void Initialize()
	{
	}
}

public class ApplyChangesAmbientLightSH2Connector : UpdatePacket<AmbientLightSH2Connector>
{
	List<color> sh = new();
	public ApplyChangesAmbientLightSH2Connector(AmbientLightSH2Connector owner) : base(owner)
	{
		for (int i = 0; i < 9; i++)
		{
			color a = owner.Owner.AmbientLight.Value[i].ToProfile(ColorProfile.Linear);
			sh.Add(a);
		}
	}

	public override int Id => (int)PacketTypes.ApplyChangesAmbientLightSH2;

	public override void Serialize(BinaryWriter buffer)
	{
		foreach (var color in sh)
		{
			buffer.Write(color.r);
			buffer.Write(color.g);
			buffer.Write(color.b);
			buffer.Write(color.a);
		}
	}
}
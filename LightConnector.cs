using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thundagun;

public class LightConnector : Connector<Light>
{
	public override void ApplyChanges()
	{
		//UniLog.Log("ApplyChangesLight");
		Thundagun.QueuePacket(new ApplyChangesLightConnector(this));
	}

	public override void Destroy(bool destroyingWorld)
	{
	}

	public override void Initialize()
	{
	}
}

public class ApplyChangesLightConnector : UpdatePacket<LightConnector>
{
	public long worldId;
	public ulong slotId;
	public ulong ownerId;
	public bool shouldBeEnabled;
	public float scale;
	public int lightType;
	public int shadowType;
	public color color;
	public float intensity;
	public float range;
	public float spotAngle;
	public float shadowStrength;
	public float shadowNearPlane;
	public int shadowCustomResolution;
	public float shadowBias;
	public float shadowNormalBias;

	public ulong cookieId;
	public string cookieLocalPath;
	public ApplyChangesLightConnector(LightConnector owner) : base(owner)
	{
		worldId = owner.Owner.World.LocalWorldHandle;
		slotId = ((owner.Owner.Slot.ReferenceID.Position) << 8) | ((owner.Owner.Slot.ReferenceID.User) & 0xFFul);
		ownerId = ((owner.Owner.ReferenceID.Position) << 8) | ((owner.Owner.ReferenceID.User) & 0xFFul);

		shouldBeEnabled = owner.Owner.ShouldBeEnabled;
		scale = MathX.AvgComponent(owner.Owner.Slot.GlobalScale);
		lightType = (int)owner.Owner.LightType.Value;
		shadowType = (int)owner.Owner.ShadowType.Value;
		color = MathX.Clamp(MathX.FilterInvalid(owner.Owner.Color.Value), -64f, 64f).ToProfile(ColorProfile.sRGB);
		intensity = MathX.Clamp(MathX.FilterInvalid(owner.Owner.Intensity.Value), -1024f, 1024f);
		range = MathX.FilterInvalid(owner.Owner.Range.Value * scale);
		spotAngle = MathX.Clamp(MathX.FilterInvalid(owner.Owner.SpotAngle.Value), 0f, 180f);
		shadowStrength = MathX.Clamp01(MathX.FilterInvalid(owner.Owner.ShadowStrength.Value));
		shadowNearPlane = MathX.Max(0.001f, MathX.FilterInvalid(owner.Owner.ShadowNearPlane.Value));
		shadowCustomResolution = owner.Owner.ShadowMapResolution.Value;
		shadowBias = owner.Owner.ShadowBias.Value;
		shadowNormalBias = owner.Owner.ShadowNormalBias.Value;

		var asset = owner.Owner.Cookie.Asset?.Connector as TextureConnector;
		cookieLocalPath = asset?.LocalPath ?? "NULL";
		cookieId = asset?.ownerId ?? default;
	}

	public override int Id => (int)PacketTypes.ApplyChangesLight;

	public override void Serialize(BinaryWriter buffer)
	{
		buffer.Write(worldId);
		buffer.Write(slotId);
		buffer.Write(ownerId);
		buffer.Write(shouldBeEnabled);

		if (shouldBeEnabled)
		{
			buffer.Write(scale);
			buffer.Write(lightType);
			buffer.Write(shadowType);
			buffer.Write(color.r);
			buffer.Write(color.g);
			buffer.Write(color.b);
			buffer.Write(color.a);
			buffer.Write(intensity);
			buffer.Write(range);
			buffer.Write(spotAngle);
			buffer.Write(shadowStrength);
			buffer.Write(shadowNearPlane);
			buffer.Write(shadowCustomResolution);
			buffer.Write(shadowBias);
			buffer.Write(shadowNormalBias);
			buffer.Write(cookieId);
			buffer.WriteString2(cookieLocalPath);
		}
	}
}
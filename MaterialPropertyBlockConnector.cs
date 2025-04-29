using Elements.Core;
using FrooxEngine;
using SharedMemory;

namespace Thundagun;

public class MaterialPropertyBlockConnector : MaterialConnectorBase, IMaterialPropertyBlockConnector, ISharedMaterialConnector, IAssetConnector, ISharedMaterialPropertySetter
{
	public void ApplyChanges(AssetIntegrated onDone)
	{
		isPropertyBlock = true;
		var owner = Asset?.Owner as IWorldElement;

		ownerId = ((owner?.ReferenceID.Position ?? default) << 8) | ((owner?.ReferenceID.User ?? default) & 0xFFul);

		//UniLog.Log($"ApplyChangesMaterialPropertyBlock: {ownerId}, Actions Count: {actionQueue?.Count ?? -1}");

		var thing = new ApplyChangesMaterialConnectorBase(this);
		if (Asset?.HighPriorityIntegration ?? false)
			Thundagun.QueueHighPriorityPacket(thing);
		else
			Thundagun.QueuePacket(thing);

		onDone(firstRender);
		firstRender = false;
	}
}
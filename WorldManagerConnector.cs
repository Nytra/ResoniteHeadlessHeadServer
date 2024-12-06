using Elements.Core;
using FrooxEngine;

public class WorldManagerConnector : IWorldManagerConnector
{
	public async Task Initialize(WorldManager owner)
	{
		UniLog.Log("World manager: initialize");
	}

	public void WorldAdded(World world)
	{
		UniLog.Log("World manager: world added");
	}

	public void WorldRemoved(World world)
	{
		UniLog.Log("World manager: world removed");
	}
}

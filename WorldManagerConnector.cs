using Elements.Core;
using FrooxEngine;

public class WorldManagerConnector : IWorldManagerConnector
{
	public async Task Initialize(WorldManager owner)
	{
		UniLog.Log("World manager init");
	}

	public void WorldAdded(World world)
	{
		UniLog.Log("World added");
	}

	public void WorldRemoved(World world)
	{
		UniLog.Log("World removed");
	}
}

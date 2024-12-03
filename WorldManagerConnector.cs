using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
//using UnityEngine;

public class WorldManagerConnector : IWorldManagerConnector
{
	//public GameObject Root { get; private set; }

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

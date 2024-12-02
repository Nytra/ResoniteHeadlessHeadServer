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
		//await InitializationTasks.Enqueue(delegate
		//{
		//	Root = new GameObject(null);
		//	Root.transform.position = Vector3.zero;
		//	Root.transform.rotation = Quaternion.identity;
		//	Root.transform.localScale = Vector3.one;
		//});
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

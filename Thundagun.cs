using Elements.Core;
using System.IO.Pipes;
using System.Diagnostics;
using FrooxEngine;

namespace Thundagun;

public class Thundagun
{
	private static BinaryWriter bw;
	private static NamedPipeServerStream pipeServer;
	private static Process childProcess;
	private const bool START_CHILD_PROCESS = false;
	//private static Queue<IUpdatePacket> packets = new();
	//private static Thingy thingy = new();
	//class Thingy
	//{
		//public bool locked = false;
	//}
	//private static bool lockTaken = false;
	public static void QueuePacket(IUpdatePacket packet)
	{
		//UniLog.Log(packet.ToString());
		if (pipeServer.IsConnected)
		{
			bw.Write(packet.Name);
			packet.Serialize(bw);
			//Thread.Sleep(1);
			//packets.Enqueue(packet);
			//if (packets.Count == 1)
			//{
			//	World w = packet.World ?? Engine.Current.WorldManager.FocusedWorld;
			//	w.RunSynchronously(() =>
			//	{
			//		while (packets.Count > 0)
			//		{
			//			var packet2 = packets.Dequeue();
			//			bw.Write(packet2.Name);
			//			packet2.Serialize(bw);
			//		}
			//	});
			//}
		}
	}
	public static void Setup(string[] args)
	{
		UniLog.Log("Start of Setup!");

		string pipeName = "ResoniteHeadlessHead";

		// Create a NamedPipeServerStream
		pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out);

		if (START_CHILD_PROCESS)
		{
			Console.WriteLine("Parent: Starting child process...");

			// Configure the child process to start in a new window
			childProcess = new Process();
			childProcess.StartInfo.FileName = @"HeadlessLibraries\Client\ResoniteThundagunHeadless.exe"; // Adjust to the child executable path
			childProcess.StartInfo.Arguments = $"{pipeName}";
			childProcess.StartInfo.UseShellExecute = true; // Run in a new window
			childProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

			// Start the child process
			childProcess.Start();
		}

		Console.WriteLine("Parent: Waiting for the client to connect...");

		pipeServer.WaitForConnection();

		bw = new BinaryWriter(pipeServer);

		// Send a 'sync message' and wait for client to receive it.
		bw.Write("SYNC");
		pipeServer.WaitForPipeDrain();

		UniLog.OnLog += (string str) =>
		{
			//sw.WriteLine(str);
		};

		if (START_CHILD_PROCESS)
		{
			Task.Run(async () =>
			{
				while (true)
				{
					if (childProcess.HasExited)
					{
						Process.GetCurrentProcess().Kill();
					}
					await Task.Delay(1000);
				}
			});
		}

		//Task.Run(async () =>
		//{
		//	while (true)
		//	{
		//		if (packets.Count > 0)
		//		{
		//			lock(thingy)
		//				thingy.locked = true;

		//			await Task.Delay(100);

		//			Queue<IUpdatePacket> copy = null;

		//			lock (packets)
		//			{
		//				copy = new Queue<IUpdatePacket>(packets);
		//				packets.Clear();
		//			}
					
		//			if (copy != null)
		//			{
		//				while (copy.Count > 0)
		//				{
		//					var packet = copy.Dequeue();
		//					bw.Write(packet.Name);
		//					packet.Serialize(bw);
		//				}
		//			}
		//		}

		//		Thread.Sleep(1000);

		//		lock (thingy)
		//			thingy.locked = false;

		//		await Task.Delay(1);
		//	}
		//});
	}
}

public abstract class UpdatePacket<T> : IUpdatePacket
{
	public string Name => GetType().Name;
	public World World { get; set; }
	public T Owner;
	public UpdatePacket(T owner)
	{
		Owner = owner;
	}
	public abstract void Serialize(BinaryWriter bw);
	public abstract void Deserialize(BinaryReader bw);
}

public interface IUpdatePacket
{
	public string Name { get; }
	public World World { get; set; }
	public void Serialize(BinaryWriter bw);
	public void Deserialize(BinaryReader bw);
}
using Elements.Core;
using System.IO.Pipes;
using System.Diagnostics;
using FrooxEngine;
using SharedMemory;
using Thundagun.NewConnectors;
using System.Text;
using System.Runtime.InteropServices;

namespace Thundagun;

public class Thundagun
{
	//private static BinaryWriter bw;
	//private static NamedPipeServerStream pipeServer;
	//private static BufferReadWrite buffer;
	private static CircularBuffer buffer;
	private static Process childProcess;
	private const bool START_CHILD_PROCESS = false;
	private static Queue<IUpdatePacket> packets = new();
	//private static Thingy thingy = new();
	//class Thingy
	//{
		//public bool locked = false;
	//}
	//private static bool lockTaken = false;
	public static void QueuePacket(IUpdatePacket packet)
	{
		//UniLog.Log(packet.ToString());
		if (buffer != null)
		{
			lock (packets)
			{
				packets.Enqueue(packet);
			}
			//bw.Write(packet.Name);
			//packet.Serialize(bw);


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
		//pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out);

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

		//pipeServer.WaitForConnection();

		buffer = new CircularBuffer("MyBuffer3", 1024, 1025);
		var syncBuffer = new BufferReadWrite("SyncBuffer3", 1024);

		Engine.Current.OnShutdown += () => 
		{ 
			buffer.Close();
			syncBuffer.Close();
		};

		//bw = new BinaryWriter();

		// Send a 'sync message'.
		int num = 999;
		syncBuffer.Write(ref num);

		do
		{
			buffer.Read(out num);
		}
		while (num != 999);

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

		Task.Run(async () => 
		{ 
			while (true)
			{
				if (packets.Count > 0)
				{
					Queue<IUpdatePacket> copy;
					lock (packets)
					{
						copy = new Queue<IUpdatePacket>(packets);
						packets.Clear();
					}
					while (copy.Count > 0)
					{
						var packet = copy.Dequeue();
						var num = packet.Id;
						buffer.Write(ref num);
						packet.Serialize(buffer);
					}
				}
			}
		});

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
	public abstract int Id { get; }
	public T Owner;
	public UpdatePacket(T owner)
	{
		Owner = owner;
	}
	public abstract void Serialize(CircularBuffer buffer);
	public abstract void Deserialize(CircularBuffer buffer);
}

public interface IUpdatePacket
{
	public int Id { get; }
	public void Serialize(CircularBuffer buffer);
	public void Deserialize(CircularBuffer buffer);
}

public enum PacketTypes
{
	Sync,
	ApplyChangesSlot,
	DestroySlot,
	InitializeWorld,
	ChangeFocusWorld,
	DestroyWorld
}
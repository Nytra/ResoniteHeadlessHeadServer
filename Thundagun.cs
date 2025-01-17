using Elements.Core;
using System.Diagnostics;
using FrooxEngine;
using SharedMemory;

namespace Thundagun;

public class Thundagun
{
	private static CircularBuffer buffer;
	private static Process childProcess;
	private const bool START_CHILD_PROCESS = false;
	private static Queue<IUpdatePacket> packets = new();
	public static void QueuePacket(IUpdatePacket packet)
	{
		//UniLog.Log(packet.ToString());
		lock (packets)
		{
			packets.Enqueue(packet);
		}
	}
	public static void Setup(string[] args)
	{
		Console.WriteLine("Server: Start of setup.");

		if (START_CHILD_PROCESS)
		{
			Console.WriteLine("Server: Starting child process...");

			// Configure the child process to start in a new window
			childProcess = new Process();
			childProcess.StartInfo.FileName = @"HeadlessLibraries\Client\ResoniteThundagunHeadless.exe"; // Adjust to the child executable path
			childProcess.StartInfo.Arguments = $"";
			childProcess.StartInfo.UseShellExecute = true; // Run in a new window
			childProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

			// Start the child process
			childProcess.Start();
		}

		Console.WriteLine("Server: Creating buffer...");

		buffer = new CircularBuffer("MyBuffer4", 1024, 1025);
		var syncBuffer = new BufferReadWrite("SyncBuffer4", 1024);

		Console.WriteLine("Server: Buffer created.");

		Engine.Current.OnShutdown += () => 
		{ 
			buffer.Close();
			syncBuffer.Close();
		};

		Console.WriteLine("Server: Waiting for the client to connect...");

		// Send a 'sync message'
		int num = 999;
		syncBuffer.Write(ref num);

		do
		{
			buffer.Read(out num);
		}
		while (num != 999);

		Console.WriteLine("Server: Client connected.");

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

		Console.WriteLine("Server: Starting packet loop.");

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
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
	private static Task packetTask;
	private static int mainBufferId;
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

		Console.WriteLine("Server: Creating buffers...");

		var rand = new Random();
		mainBufferId = rand.Next();
		if (mainBufferId == 0) mainBufferId = 1;

		Console.WriteLine($"Server: Opening main buffer with id {mainBufferId}.");

		buffer = new CircularBuffer($"MyBuffer{mainBufferId}", 4096, 128);
		var syncBuffer = new BufferReadWrite($"SyncBuffer", 4);
		var returnBuffer = new CircularBuffer("ReturnBuffer", 4, 4);

		Console.WriteLine("Server: Buffers created.");

		Engine.Current.OnShutdown += () => 
		{ 
			buffer.Close();
			syncBuffer.Close();
			returnBuffer.Close();
		};

		// Send a 'sync message'

		Console.WriteLine("Server: Writing main buffer id to sync buffer...");

		syncBuffer.Write(ref mainBufferId);

		Console.WriteLine("Server: Waiting for the client to connect...");

		int num;
		do
		{
		returnBuffer.Read(out num);
		}
		while (num != mainBufferId);

		Console.WriteLine("Server: Client connected.");

		syncBuffer.Close();
		returnBuffer.Close();

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

		packetTask = Task.Run(ProcessPackets);
	}
	private static void ProcessPackets()
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
	DestroyWorld,
	ApplyChangesMeshRenderer,
	DestroyMeshRenderer
}
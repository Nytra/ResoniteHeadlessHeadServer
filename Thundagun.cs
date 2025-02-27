using Elements.Core;
using System.Diagnostics;
using FrooxEngine;
using SharedMemory;

namespace Thundagun;

public class Thundagun
{
	private static CircularBuffer? buffer;
	private static CircularBuffer? returnBuffer;
	private static BufferReadWrite? syncBuffer;
	private static Process? childProcess;
	private const bool START_CHILD_PROCESS = false;
	private static Queue<PacketStruct> packets = new();
	private static int mainBufferId;
	public const int MAX_STRING_LENGTH = 256; // UTF8
	public struct PacketStruct
	{
		public IUpdatePacket packet;
		public Action callback;
	}
	public static void QueuePacket(IUpdatePacket packet, Action callback = null)
	{
		//UniLog.Log(packet.ToString());
		var packetStruct = new PacketStruct();
		packetStruct.packet = packet;
		packetStruct.callback = callback;
		lock (packets)
		{
			packets.Enqueue(packetStruct);
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

		buffer = new CircularBuffer($"MyBuffer{mainBufferId}", 4096, 256); // MathX.Max(Thundagun.MAX_STRING_LENGTH, sizeof(ulong))
		syncBuffer = new BufferReadWrite($"SyncBuffer{DateTime.Now.Minute}", sizeof(int));
		returnBuffer = new CircularBuffer($"ReturnBuffer{mainBufferId}", 4096, 256);

		Console.WriteLine("Server: Buffers created.");

		Engine.Current.OnShutdown += () => 
		{ 
			buffer.Close();
			buffer = null;
			syncBuffer?.Close();
			returnBuffer?.Close();
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
		syncBuffer = null;
		returnBuffer.Close();
		returnBuffer = null;

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

		Task.Run(ProcessPackets);
	}
	private static void ProcessPackets()
	{
		while (true)
		{
			try
			{
				if (packets.Count > 0)
				{
					Queue<PacketStruct> copy;
					lock (packets)
					{
						copy = new Queue<PacketStruct>(packets);
						packets.Clear();
					}
					while (copy.Count > 0)
					{
						var packetStruct = copy.Dequeue();
						var num = packetStruct.packet.Id;
						buffer.Write(ref num);
						try
						{
							packetStruct.packet.Serialize(buffer);
						}
						catch (Exception e)
						{
							UniLog.Error($"Exception during serialization: {e}");
						}
						try
						{
							packetStruct.callback?.Invoke();
						}
						catch (Exception e)
						{
							UniLog.Error($"Exception running packet queue callback: {e}");
						}
					}
				}
			}
			catch (Exception e)
			{
				UniLog.Error($"Exception running packet task: {e}");
			}
			//int n;
			//returnBuffer.Read(out n); // halt until the client sends data in this buffer
		}
	}
	//private static void ReturnThread()
	//{
	//	while (true)
	//	{
	//		int num;
	//		returnBuffer.Read(out num);
	//		if (num == 5)
	//		{

	//		}
	//	}
	//}
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
	None,
	ApplyChangesSlot,
	DestroySlot,
	InitializeWorld,
	ChangeFocusWorld,
	DestroyWorld,
	ApplyChangesMeshRenderer,
	DestroyMeshRenderer,
	LoadFromFileShader,
	ApplyChangesMesh
}
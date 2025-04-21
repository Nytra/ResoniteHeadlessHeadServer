using Elements.Core;
using System.Diagnostics;
using FrooxEngine;
using SharedMemory;

namespace Thundagun;

public class Thundagun
{
	private static CircularBuffer? buffer;
	private static CircularBuffer? returnBuffer;
	//private static CircularBuffer? frameSyncBuffer;
	private static BufferReadWrite? syncBuffer;
	private static Process? childProcess;
	private const bool START_CHILD_PROCESS = false;
	private static Queue<PacketStruct> packets = new();
	private static Queue<PacketStruct> highPriorityPackets = new();
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
	public static void QueueHighPriorityPacket(IUpdatePacket packet, Action callback = null)
	{
		//UniLog.Log(packet.ToString());
		var packetStruct = new PacketStruct();
		packetStruct.packet = packet;
		packetStruct.callback = callback;
		lock (highPriorityPackets)
		{
			highPriorityPackets.Enqueue(packetStruct);
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

		buffer = new CircularBuffer($"MyBuffer{mainBufferId}", 50, 1048576); // MathX.Max(Thundagun.MAX_STRING_LENGTH, sizeof(ulong))
		syncBuffer = new BufferReadWrite($"SyncBuffer{DateTime.Now.Minute}", sizeof(int));
		returnBuffer = new CircularBuffer($"ReturnBuffer{mainBufferId}", 50, 1048576);
		//frameSyncBuffer = new CircularBuffer($"FrameSyncBuffer{mainBufferId}", 2, 4);

		Console.WriteLine("Server: Buffers created.");

		Engine.Current.OnShutdown += () => 
		{ 
			buffer.Close();
			buffer = null;
			syncBuffer?.Close();
			syncBuffer = null;
			returnBuffer?.Close();
			returnBuffer = null;
			//frameSyncBuffer?.Close();
			//frameSyncBuffer = null;
		};

		// Send a sync message

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
		Task.Run(ReturnTask);
		//Engine.Current.GlobalCoroutineManager.RunInUpdates(1, () =>
		//{
			//FrameSyncLoop();
		//});
	}
	//private static void FrameSyncLoop()
	//{
	//	Thread.Sleep((int)((1 / 30f) * 1000f));
	//	Engine.Current.GlobalCoroutineManager.RunInUpdates(1, () => 
	//	{ 
	//		FrameSyncLoop();
	//	});
	//}
	private static async void ProcessPackets()
	{
		while (true)
		{
			try
			{
				if (highPriorityPackets.Count > 0)
				{
					Queue<PacketStruct> copy;
					lock (highPriorityPackets)
					{
						copy = new Queue<PacketStruct>(highPriorityPackets);
						highPriorityPackets.Clear();
					}
					while (copy.Count > 0)
					{
						var packetStruct = copy.Dequeue();
						var num = packetStruct.packet.Id;
						MemoryStream ms = new();
						BinaryWriter bw = new(ms);
						bw.Write(num);
						try
						{
							//ms.Seek(0, SeekOrigin.Begin);
							packetStruct.packet.Serialize(bw);
							byte[] arr = ms.ToArray();
							int len = arr.Length;
							buffer.Write(ref len);
							buffer.Write(arr);
						}
						catch (Exception e)
						{
							UniLog.Error($"Exception during high priority serialization: {e}");
							throw;
						}
						try
						{
							packetStruct.callback?.Invoke();
						}
						catch (Exception e)
						{
							UniLog.Error($"Exception running high priority packet queue callback: {e}");
							throw;
						}
						await Task.Delay(10);
					}
				}
				//aaa
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
						if (highPriorityPackets.Count > 0)
						{
							Queue<PacketStruct> highPrioCopy;
							lock (highPriorityPackets)
							{
								highPrioCopy = new Queue<PacketStruct>(highPriorityPackets);
								highPriorityPackets.Clear();
							}
							while (highPrioCopy.Count > 0)
							{
								var highPrio = highPrioCopy.Dequeue();
								var num2 = highPrio.packet.Id;
								MemoryStream ms2 = new();
								BinaryWriter bw2 = new(ms2);
								bw2.Write(num2);
								try
								{
									//ms2.Seek(0, SeekOrigin.Begin);
									highPrio.packet.Serialize(bw2);
									byte[] arr = ms2.ToArray();
									int len2 = arr.Length;
									buffer.Write(ref len2);
									buffer.Write(arr);
								}
								catch (Exception e)
								{
									UniLog.Error($"Exception during high priority serialization: {e}");
									throw;
								}
								try
								{
									highPrio.callback?.Invoke();
								}
								catch (Exception e)
								{
									UniLog.Error($"Exception running high priority packet queue callback: {e}");
									throw;
								}
								await Task.Delay(10);
							}
						}
						var packetStruct = copy.Dequeue();
						var num = packetStruct.packet.Id;
						MemoryStream ms = new();
						BinaryWriter bw = new(ms);
						bw.Write(num);
						try
						{
							packetStruct.packet.Serialize(bw);
							//ms.Seek(0, SeekOrigin.Begin);
							byte[] arr = ms.ToArray();
							int len = arr.Length;
							buffer.Write(ref len);
							buffer.Write(arr);
						}
						catch (Exception e)
						{
							UniLog.Error($"Exception during serialization: {e}");
							throw;
						}
						try
						{
							packetStruct.callback?.Invoke();
						}
						catch (Exception e)
						{
							UniLog.Error($"Exception running packet queue callback: {e}");
							throw;
						}
						await Task.Delay(10);
					}
				}
			}
			catch (Exception e)
			{
				UniLog.Error($"Exception running packet task: {e}");
				throw;
			}
			//int n;
			//returnBuffer.Read(out n); // halt until the client sends data in this buffer
		}
	}
	private static void ReturnTask()
	{
		int num;
		while (true)
		{
			try 
			{
				//returnBuffer.Read(out num);

				int len;
				returnBuffer.Read(out len);

				byte[] arr = new byte[len];
				returnBuffer.Read(arr);

				MemoryStream ms = new(arr, 0, len);
				BinaryReader br = new(ms);

				num = br.ReadInt32();

				if (num != 0)
				{
					if (num == (int)PacketTypes.InitializeMaterialProperties)
					{
						var matConn = MaterialConnector.initializingProperties.Dequeue();

						InitializeMaterialPropertiesPacket deserializedObject = new(matConn);
						deserializedObject.Deserialize(br);

						UniLog.Log($"InitializeMaterialProperties ReturnPacket Data: {string.Join(',', deserializedObject.PropertyIds)}");

						int i = 0;
						foreach (var prop in matConn.Properties)
						{
							try
							{
								prop.GetType().GetProperty("Index").SetValue(prop, deserializedObject.PropertyIds[i]);
								//prop.Initialize(deserializedObject.PropertyIds[i]);
							}
							catch (Exception e)
							{
								UniLog.Warning($"Error when initializing material property: {e}");
							}
							i += 1;
						}
					}
				}
			}
			catch (Exception e) 
			{
				UniLog.Error($"ReturnBuffer Error: {e}");
				throw;
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
	public abstract void Serialize(BinaryWriter buffer);
	public virtual void Deserialize(BinaryReader buffer)
	{
		// owo
	}
}

public interface IUpdatePacket
{
	public int Id { get; }
	public void Serialize(BinaryWriter buffer);
	public void Deserialize(BinaryReader buffer);
}

public enum PacketTypes
{
	None, // 0 means no packet
	ApplyChangesSlot,
	DestroySlot,
	InitializeWorld,
	ChangeFocusWorld,
	DestroyWorld,
	ApplyChangesMeshRenderer,
	DestroyMeshRenderer,
	LoadFromFileShader,
	ApplyChangesMesh,
	ApplyChangesMaterial,
	InitializeMaterialProperties,
	SetFormatTexture,
	SetPropertiesTexture,
	SetDataTexture
}
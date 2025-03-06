using Elements.Core;
using System.Diagnostics;
using FrooxEngine;
using SharedMemory;
using System;
using System.Linq.Expressions;

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

		buffer = new CircularBuffer($"MyBuffer{mainBufferId}", 16384, 512); // MathX.Max(Thundagun.MAX_STRING_LENGTH, sizeof(ulong))
		syncBuffer = new BufferReadWrite($"SyncBuffer{DateTime.Now.Minute}", sizeof(int));
		returnBuffer = new CircularBuffer($"ReturnBuffer{mainBufferId}", 4096, 512);
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
		//returnBuffer.Close();
		//returnBuffer = null;

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
	private static void ProcessPackets()
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
						buffer.Write(ref num);
						try
						{
							packetStruct.packet.Serialize(buffer);
							//UniLog.Log($"Serialized {num}");
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
								buffer.Write(ref num2);
								try
								{
									highPrio.packet.Serialize(buffer);
									//UniLog.Log($"Serialized {num}");
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
							}
						}
						var packetStruct = copy.Dequeue();
						var num = packetStruct.packet.Id;
						buffer.Write(ref num);
						try
						{
							packetStruct.packet.Serialize(buffer);
							//UniLog.Log($"Serialized {num}");
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
				returnBuffer.Read(out num);

				if (num != 0)
				{
					if (num == (int)PacketTypes.InitializeMaterialProperties)
					{

						var matConn = MaterialConnector.initializingProperties.Dequeue();

						InitializeMaterialPropertiesPacket deserializedObject = new(matConn);
						deserializedObject.Deserialize(returnBuffer);

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

						if (MaterialConnectorBase.onDoneActions.Count > 0)
						{
							var onDone = MaterialConnector.onDoneActions.Dequeue();
							onDone.Invoke();
						}
						if (MaterialConnectorBase.markDoneActions.Count > 0)
						{
							var markDone = MaterialConnector.markDoneActions.Dequeue();
							markDone.Invoke();
						}
					}
					//else if (num == (int)PacketTypes.ShaderLoadedCallback)
					//{
					//	ShaderLoadedCallback callback = new();
					//	callback.Deserialize(returnBuffer);

					//	UniLog.Log($"ShaderLoadedCallback: {callback.shaderPath}");

					//	string path = callback.shaderPath;

					//	var pathCleaned = "";
					//	foreach (var letter in path) 
					//	{
					//		if (Char.IsAsciiLetterOrDigit(letter) || letter == '/' || letter == '.' || letter == '\\' || letter == ':')
					//		{
					//			pathCleaned += letter;
					//		}
					//	}
					//	//ShaderConnector.loadedShaders.Add(path);
					//	//var matChange = MaterialConnector.queuedMaterialChanges.Dequeue();
					//	UniLog.Log($"Callback actions num: {ShaderConnector.onLoadedActions[pathCleaned].Count}");
					//	foreach (var act in ShaderConnector.onLoadedActions[pathCleaned])
					//	{
					//		act.Invoke();
					//	}
					//	//Thundagun.QueuePacket(matChange);

					//	//ShaderConnector.allLoaded = true;

					//	//Engine.Current.GlobalCoroutineManager.RunInSeconds(5, () => 
					//	//{
					//	//	// if all loaded, flush material changes
					//	//	if (ShaderConnector.allLoaded)
					//	//	{
					//	//		ShaderConnector.allLoadedFinal = true;
					//	//		UniLog.Log($"Flushing material queue!");
					//	//		while (MaterialConnector.queuedMaterialChanges.Count > 0)
					//	//		{
					//	//			var act = MaterialConnector.queuedMaterialChanges.Dequeue();
					//	//			Thundagun.QueuePacket(act);
					//	//		}
					//	//		ShaderConnector.shader++;
					//	//	}
					//	//});
					//}
				}
			}
			catch (Exception e) 
			{
				UniLog.Error($"ReturnBuffer Error: {e}");
				throw;
			}
			//updates++;
			//if (updates > 25)
			//{
			//	updates = 0;
			//	await Task.Delay(TimeSpan.FromMilliseconds(1));
			//}
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
	InitializeMaterialProperties
	//ShaderLoadedCallback
}
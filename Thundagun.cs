using Elements.Core;
using System.IO.Pipes;
using System.Diagnostics;

namespace Thundagun;

public class Thundagun
{
	private static StreamWriter sw;
	private static BinaryWriter bw;
	private static NamedPipeServerStream pipeServer;
	private static Queue<IUpdatePacket> packets = new();
	public static void QueuePacket(IUpdatePacket packet)
	{
		lock (packets)
		{
			packets.Enqueue(packet);
		}
	}
	public static void Setup(string[] args)
	{
		UniLog.Log("Start of Setup!");

		string pipeName = "ResoniteHeadlessHead";

		// Create a NamedPipeServerStream
		pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out);
		Console.WriteLine("Parent: Starting child process...");

		// Configure the child process to start in a new window
		var childProcess = new Process();
		childProcess.StartInfo.FileName = @"HeadlessLibraries\Client\ResoniteThundagunHeadless.exe"; // Adjust to the child executable path
		childProcess.StartInfo.Arguments = $"{pipeName}";
		childProcess.StartInfo.UseShellExecute = true; // Run in a new window
		childProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

		// Start the child process
		//childProcess.Start();

		Console.WriteLine("Parent: Waiting for the child to connect...");

		pipeServer.WaitForConnection();

		// Read user input and send that to the client process.
		sw = new StreamWriter(pipeServer);
		sw.AutoFlush = true;

		bw = new BinaryWriter(pipeServer);

		// Send a 'sync message' and wait for client to receive it.
		sw.WriteLine("SYNC");
		pipeServer.WaitForPipeDrain();

		UniLog.OnLog += (string str) =>
		{
			//sw.WriteLine(str);
		};

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

		Task.Run(async () => 
		{
			while (true)
			{
				if (pipeServer.IsConnected)
				{
					while (packets.Count > 0)
					{
						IUpdatePacket packet;
						lock (packets)
						{
							packet = packets.Dequeue();
						}
						sw.WriteLine(packet.Name);
						packet.Serialize(bw);
					}
				}
				await Task.Delay(1);
			}
		});
	}
}

public abstract class UpdatePacket<T> : IUpdatePacket
{
	public string Name => GetType().Name;
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
	public void Serialize(BinaryWriter bw);
	public void Deserialize(BinaryReader bw);
}
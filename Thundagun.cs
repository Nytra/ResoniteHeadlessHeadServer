using Elements.Core;
using System.IO.Pipes;
using System.Diagnostics;

namespace Thundagun;

public class Thundagun
{
	private static BinaryWriter bw;
	private static NamedPipeServerStream pipeServer;
	private static Process childProcess;
	private const bool START_CHILD_PROCESS = false;
	public static void QueuePacket(IUpdatePacket packet)
	{
		UniLog.Log(packet.ToString());
		if (pipeServer.IsConnected)
		{
			bw.Write(packet.Name);
			packet.Serialize(bw);
			//bw.Flush();
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
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class ShaderConnector : IShaderConnector
{
	public string File;
	public string LocalPath;
	public static Dictionary<string, List<Action>> onLoadedActions = new();
	public static HashSet<string> loadedShaders = new();
	public static int shader = 0;
	public static bool allLoaded = false;
	public static bool allLoadedFinal = false;
	public static Dictionary<string, string> LocalPathToFile = new();
	public Asset Asset;
	public void Initialize(Asset asset)
	{
		Asset = asset;
		LocalPath = asset?.AssetURL?.LocalPath ?? "NULL";
		//UniLog.Log($"Initialize shader: {LocalPath}");
	}

	public void LoadFromFile(string file, AssetIntegrated onLoaded)
	{
		File = file ?? "NULL";

		LocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";

		if (File != "NULL" && LocalPath != "NULL" && !LocalPathToFile.ContainsKey(LocalPath))
		{
			lock (LocalPathToFile)
			{
				LocalPathToFile[LocalPath] = File;
			}
		}

		UniLog.Log($"Loading shader: {LocalPath}, {File}");

		onLoaded(true);
	}

	public void Unload()
	{
	}
}

public class LoadFromFileShaderConnector : UpdatePacket<ShaderConnector>
{
	string File;
	string LocalPath;
	public LoadFromFileShaderConnector(ShaderConnector owner) : base(owner)
	{
		File = owner.File;
		if (File.Length > Thundagun.MAX_STRING_LENGTH)
			File = owner.File.Substring(0, Math.Min(owner.File.Length, Thundagun.MAX_STRING_LENGTH));
		LocalPath = owner.LocalPath;
		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			LocalPath = owner.LocalPath.Substring(0, Math.Min(owner.LocalPath.Length, Thundagun.MAX_STRING_LENGTH));
	}

	public override int Id => (int)PacketTypes.LoadFromFileShader;

	public override void Deserialize(CircularBuffer buffer)
	{
		var bytes = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes);
		File = Encoding.UTF8.GetString(bytes);

		var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes2);
		LocalPath = Encoding.UTF8.GetString(bytes2);
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(Encoding.UTF8.GetBytes(File));

		buffer.Write(Encoding.UTF8.GetBytes(LocalPath));
	}
}
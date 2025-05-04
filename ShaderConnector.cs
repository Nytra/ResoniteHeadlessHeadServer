using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class ShaderConnector : IShaderConnector
{
	public string File;
	public string LocalPath;
	public static Dictionary<string, string> LocalPathToFile = new();
	public static Dictionary<ShaderConnector, string> ShaderToFile = new();
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

		if (LocalPath == null || LocalPath == "NULL") 
			LocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";

		//lock (ShaderToFile)
		//{
			//ShaderToFile[this] = File;
		//}

		if (File != "NULL" && LocalPath != "NULL")// && !LocalPathToFile.ContainsKey(LocalPath))
		{
			var shader = Asset as Shader;
			lock (LocalPathToFile)
			{
				LocalPathToFile[LocalPath + shader.VariantIndex?.ToString() ?? ""] = File;
			}
		}

		UniLog.Log($"Loading shader: {LocalPath}, {File}");

		onLoaded(true);
	}

	public void Unload()
	{
	}
}

//public class LoadFromFileShaderConnector : UpdatePacket<ShaderConnector>
//{
//	string File;
//	string LocalPath;
//	public LoadFromFileShaderConnector(ShaderConnector owner) : base(owner)
//	{
//		File = owner.File;
//		if (File.Length > Thundagun.MAX_STRING_LENGTH)
//			File = owner.File.Substring(0, Math.Min(owner.File.Length, Thundagun.MAX_STRING_LENGTH));
//		LocalPath = owner.LocalPath;
//		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
//			LocalPath = owner.LocalPath.Substring(0, Math.Min(owner.LocalPath.Length, Thundagun.MAX_STRING_LENGTH));
//	}

//	public override int Id => (int)PacketTypes.LoadFromFileShader;

//	public override void Serialize(BinaryWriter buffer)
//	{
//		buffer.WriteString2(File);

//		buffer.WriteString2(LocalPath);
//	}
//}
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

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
	public void Initialize(Asset asset)
	{
		LocalPath = asset?.AssetURL?.LocalPath ?? "NULL";
		//UniLog.Log($"Initialize shader: {LocalPath}");
		//UniLog.Log($"ShaderInit: {LocalPath}");
	}

	public void LoadFromFile(string file, AssetIntegrated onLoaded)
	{
		File = file ?? "NULL";
		//if (File == "NULL") 
		//{
			//onLoaded(true);
			//return;
		//}

		lock (LocalPathToFile)
		{
			LocalPathToFile[LocalPath] = File;
		}
		

		//if (File == "NULL") 
		//{
			//onLoaded(true);
			//return;
		//}

		UniLog.Log($"Loading shader: {LocalPath}, {File}");
		
		//Thundagun.QueuePacket(new LoadFromFileShaderConnector(this));
		//shaders++;
		//if (!ShaderConnector.onLoadedActions.ContainsKey(File))
		//{
			//var list = new List<Action>();
			//list.Add(() => onLoaded(true));
			//lock (onLoadedActions)
				//onLoadedActions.Add(File, list);
		//}
		//else
		//{
		//	onLoadedActions[File].Add(() => onLoaded(true));
		//}
		
		//allLoaded = false;

		//while (!loadedShaders.Contains(File))
		//{
			//Thread.Sleep(1);
		//}

		//onLoaded(true);

		//Engine.Current.GlobalCoroutineManager.RunInSeconds(15, () => 
		//{
		//	
		//});

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

//public class ShaderLoadedCallback : IUpdatePacket
//{
//	public string shaderPath;
//	public int Id => (int)PacketTypes.ShaderLoadedCallback;

//	public void Deserialize(CircularBuffer buffer)
//	{
//		buffer.ReadString(out shaderPath);
//	}

//	public void Serialize(CircularBuffer buffer)
//	{
//		buffer.WriteString(shaderPath);
//	}
//}
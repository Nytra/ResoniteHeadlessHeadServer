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
	public void Initialize(Asset asset)
	{
		LocalPath = asset?.AssetURL?.LocalPath ?? "NULL";
		//UniLog.Log($"ShaderInit: {LocalPath}");
	}

	public void LoadFromFile(string file, AssetIntegrated onLoaded)
	{
		UniLog.Log($"Loading shader: {file}");
		File = file ?? "NULL";
		Thundagun.QueuePacket(new LoadFromFileShaderConnector(this));
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
		File = owner.File.Substring(0, Math.Min(owner.File.Length, 128));
		LocalPath = owner.LocalPath.Substring(0, Math.Min(owner.LocalPath.Length, 128)); ;
	}

	public override int Id => (int)PacketTypes.LoadFromFileShader;

	public override void Deserialize(CircularBuffer buffer)
	{
		var bytes = new byte[512];
		buffer.Read(bytes);
		File = Encoding.UTF8.GetString(bytes);

		var bytes2 = new byte[512];
		buffer.Read(bytes2);
		LocalPath = Encoding.UTF8.GetString(bytes2);
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(Encoding.UTF8.GetBytes(File));

		buffer.Write(Encoding.UTF8.GetBytes(LocalPath));
	}
}
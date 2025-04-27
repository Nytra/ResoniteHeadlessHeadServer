using SharedMemory;
using System;
using System.Text;

namespace Thundagun;

public static class HelperExtensions
{
	public static void ReadString(this CircularBuffer buffer, out string str)
	{
		var bytes = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes);
		str = Encoding.UTF8.GetString(bytes);
	}

	public static void WriteString(this CircularBuffer buffer, string str)
	{
		buffer.Write(Encoding.UTF8.GetBytes(str));
	}

	public static void WriteString2(this BinaryWriter writer, string str)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(str);
		writer.Write(bytes.Length);
		writer.Write(bytes);
	}

	public static string ReadString2(this BinaryReader reader)
	{
		//byte[] bytes = Encoding.UTF8.GetBytes(str);
		//writer.Write(bytes.Length);
		//writer.Write(bytes);

		int len = reader.ReadInt32();
		var bytes = new byte[len];
		reader.Read(bytes);
		return Encoding.UTF8.GetString(bytes);
	}
}
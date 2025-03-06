using SharedMemory;
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
}
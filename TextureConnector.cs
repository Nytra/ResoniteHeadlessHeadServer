using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using SharedMemory;
using System.Text;

namespace Thundagun;

public class TextureConnector : ITexture2DConnector
{
	public Asset Asset;

	bool firstRender = true;

	public bool _texturePropertiesDirty;
	public TextureFilterMode _filterMode;
	public int _anisoLevel;
	public TextureWrapMode _wrapU;
	public TextureWrapMode _wrapV;
	public float _mipmapBias;
	public TextureFormatData _textureFormatData;
	public TextureUploadData _textureUploadData;
	public string LocalPath = null;
	public ulong ownerId;

	int _lastWidth;
	int _lastHeight;
	TextureFormatData _lastFormat;
	int _lastMips;

	public enum TextureType
	{
		Texture2D,
		Cubemap,
		Texture3D
	}

	public class TextureFormatData : IEquatable<TextureFormatData>
	{
		public TextureType type;

		public int width;

		public int height;

		public int depth;

		public int mips;

		public Elements.Assets.TextureFormat format;

		public AssetIntegrated onDone;

		public ColorProfile profile;

		public int ArraySize => type switch
		{
			TextureType.Texture2D => 1,
			TextureType.Cubemap => 6,
			TextureType.Texture3D => depth,
			_ => throw new Exception("Invalid texture type: " + type),
		};

		public bool Equals(TextureFormatData? other)
		{
			if (other is null) return this is null;
			return other.type == type &&
			other.width == width &&
			other.height == height &&
			other.depth == depth &&
			other.mips == mips &&
			other.format == format &&
			other.profile == profile &&
			other.ArraySize == ArraySize;
		}
	}

	public class TextureUploadData
	{
		public Bitmap2D bitmap2D;

		public BitmapCube bitmapCube;

		public Bitmap3D bitmap3D;

		public int startMip;

		public TextureUploadHint hint2D;

		public Texture3DUploadHint hint3D;

		public AssetIntegrated onDone;

		public Bitmap Bitmap => (Bitmap)(bitmap2D ?? ((object)bitmapCube) ?? ((object)bitmap3D));

		public Elements.Assets.TextureFormat Format => Bitmap.Format;

		public int2 FaceSize => bitmap2D?.Size ?? bitmapCube?.Size ?? int2.Zero;

		public int ElementCount
		{
			get
			{
				if (bitmap2D != null)
				{
					return 1;
				}
				if (bitmapCube != null)
				{
					return 6;
				}
				if (bitmap3D != null)
				{
					return bitmap3D.Size.z;
				}
				throw new Exception("Invalid state, must have either Bitmap2D, BitmapCUBE or Bitmap3D");
			}
		}

		public int2 MipMapSize(int mip)
		{
			return bitmap2D?.MipMapSize(mip) ?? bitmapCube?.MipMapSize(mip) ?? int2.Zero;
		}

		public int PixelStart(int x, int y, int mip, int face)
		{
			return bitmap2D?.PixelStart(x, y, mip) ?? bitmapCube.PixelStart(x, y, (BitmapCube.Face)face, mip);
		}

		public void ConvertTo(Elements.Assets.TextureFormat format)
		{
			if (bitmap2D != null)
			{
				bitmap2D = bitmap2D.ConvertTo(format);
			}
			if (bitmapCube != null)
			{
				bitmapCube = bitmapCube.ConvertTo(format);
			}
		}
	}

	public void Initialize(Asset asset)
	{
		Asset = asset;
		LocalPath = asset?.AssetURL?.LocalPath ?? "NULL";
		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			LocalPath = LocalPath.Substring(0, Math.Min(LocalPath.Length, Thundagun.MAX_STRING_LENGTH));
		var elem = Asset?.Owner as IWorldElement;
		ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);
	}

	// first
	public void SetTexture2DProperties(TextureFilterMode filterMode, int anisoLevel, TextureWrapMode wrapU, TextureWrapMode wrapV, float mipmapBias, AssetIntegrated onSet)
	{
		_texturePropertiesDirty = true;
		_filterMode = filterMode;
		_anisoLevel = anisoLevel;
		_wrapU = wrapU;
		_wrapV = wrapV;
		_mipmapBias = mipmapBias;

		LocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";
		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			LocalPath = LocalPath.Substring(0, Math.Min(LocalPath.Length, Thundagun.MAX_STRING_LENGTH));
		var elem = Asset?.Owner as IWorldElement;
		if (elem is null && LocalPath == "NULL")
		{
			if (onSet != null)
				onSet(false);
			return;
		}

		ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);

		if (Asset.HighPriorityIntegration)
			Thundagun.QueueHighPriorityPacket(new SetPropertiesTextureConnector(this));
		else
			Thundagun.QueuePacket(new SetPropertiesTextureConnector(this));

		if (onSet != null)
			onSet(false);
	}

	// second
	public void SetTexture2DFormat(int width, int height, int mipmaps, TextureFormat format, ColorProfile profile, AssetIntegrated onDone)
	{
		TextureFormatData textureFormatData = new TextureFormatData();
		textureFormatData.type = TextureType.Texture2D;
		textureFormatData.width = width;
		textureFormatData.height = height;
		textureFormatData.depth = 1;
		textureFormatData.mips = mipmaps;
		textureFormatData.format = format;
		textureFormatData.onDone = onDone;
		textureFormatData.profile = profile;
		_textureFormatData = textureFormatData;

		LocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";
		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			LocalPath = LocalPath.Substring(0, Math.Min(LocalPath.Length, Thundagun.MAX_STRING_LENGTH));

		var elem = Asset?.Owner as IWorldElement;
		if (elem is null && LocalPath == "NULL")
		{
			callOnDone();
			return;
		}

		ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);

		Thundagun.QueueHighPriorityPacket(new SetFormatTextureConnector(this));

		callOnDone();

		_lastWidth = textureFormatData.width;
		_lastHeight = textureFormatData.height;
		_lastFormat = textureFormatData;
		_lastMips = textureFormatData.mips;

		void callOnDone()
		{
			textureFormatData.onDone(firstRender || _lastWidth != textureFormatData.width || _lastHeight != textureFormatData.height || !_lastFormat.Equals(textureFormatData) || _lastMips > 1 != textureFormatData.mips > 1);
			firstRender = false;
		}
	}

	// third
	public void SetTexture2DData(Bitmap2D data, int startMipLevel, TextureUploadHint hint, AssetIntegrated onSet)
	{
		TextureUploadData textureUploadData = new TextureUploadData();
		textureUploadData.bitmap2D = data;
		textureUploadData.startMip = startMipLevel;
		textureUploadData.hint2D = hint;
		textureUploadData.onDone = onSet;
		_textureUploadData = textureUploadData;

		LocalPath = Asset?.AssetURL?.LocalPath ?? "NULL";
		if (LocalPath.Length > Thundagun.MAX_STRING_LENGTH)
			LocalPath = LocalPath.Substring(0, Math.Min(LocalPath.Length, Thundagun.MAX_STRING_LENGTH));
		var elem = Asset?.Owner as IWorldElement;
		if (elem is null && LocalPath == "NULL")
		{
			onSet(false);
			return;
		}

		ownerId = ((elem?.ReferenceID.Position ?? default) << 8) | ((elem?.ReferenceID.User ?? default) & 0xFFul);

		if (Asset.HighPriorityIntegration)
			Thundagun.QueueHighPriorityPacket(new SetDataTextureConnector(this));
		else
			Thundagun.QueuePacket(new SetDataTextureConnector(this));

		onSet(false);
	}

	public void Unload()
	{
	}
}

public class SetFormatTextureConnector : UpdatePacket<TextureConnector>
{
	TextureConnector.TextureFormatData formatData;
	bool propertiesDirty;
	TextureFilterMode filterMode;
	int anisoLevel;
	TextureWrapMode wrapModeU;
	TextureWrapMode wrapModeV;
	float mipMapBias;
	string localPath;
	ulong ownerId;

	public SetFormatTextureConnector(TextureConnector owner) : base(owner)
	{
		formatData = owner._textureFormatData;
		propertiesDirty = owner._texturePropertiesDirty;
		owner._texturePropertiesDirty = false;
		filterMode = owner._filterMode;
		anisoLevel = owner._anisoLevel;
		wrapModeU = owner._wrapU;
		wrapModeV = owner._wrapV;
		mipMapBias = owner._mipmapBias;
		localPath = owner.LocalPath;
		ownerId = owner.ownerId;
	}

	public override int Id => (int)PacketTypes.SetFormatTexture;

	public override void Deserialize(CircularBuffer buffer)
	{
		string localPath2;
		var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes2);
		localPath2 = Encoding.UTF8.GetString(bytes2);

		ulong ownerId2;
		buffer.Read(out ownerId2);

		int type;
		buffer.Read(out type);

		int format;
		buffer.Read(out format);

		int width;
		buffer.Read(out width);

		int height;
		buffer.Read(out height);

		int mips;
		buffer.Read(out mips);

		bool propsDirty;
		buffer.Read(out propsDirty);

		if (propsDirty)
		{
			int filter;
			buffer.Read(out filter);

			int aniso;
			buffer.Read(out aniso);

			int wrapU;
			buffer.Read(out wrapU);

			int wrapV;
			buffer.Read(out wrapV);

			float mipBias;
			buffer.Read(out mipBias);
		}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(Encoding.UTF8.GetBytes(localPath));

		buffer.Write(ref ownerId);

		int type = (int)formatData.type;
		buffer.Write(ref type);

		int format = (int)formatData.format;
		buffer.Write(ref format);

		buffer.Write(ref formatData.width);

		buffer.Write(ref formatData.height);

		buffer.Write(ref formatData.mips);

		buffer.Write(ref propertiesDirty);

		if (propertiesDirty)
		{
			int filter = (int)filterMode;
			buffer.Write(ref filter);

			buffer.Write(ref anisoLevel);

			int wrapU = (int)wrapModeU;
			buffer.Write(ref wrapU);

			int wrapV = (int)wrapModeV;
			buffer.Write(ref wrapV);

			buffer.Write(ref mipMapBias);
		}
	}
}

public class SetPropertiesTextureConnector : UpdatePacket<TextureConnector>
{
	bool propertiesDirty;
	TextureFilterMode filterMode;
	int anisoLevel;
	TextureWrapMode wrapModeU;
	TextureWrapMode wrapModeV;
	float mipMapBias;
	string localPath;
	ulong ownerId;
	public SetPropertiesTextureConnector(TextureConnector owner) : base(owner)
	{
		propertiesDirty = owner._texturePropertiesDirty;
		owner._texturePropertiesDirty = false;
		filterMode = owner._filterMode;
		anisoLevel = owner._anisoLevel;
		wrapModeU = owner._wrapU;
		wrapModeV = owner._wrapV;
		mipMapBias = owner._mipmapBias;
		localPath = owner.LocalPath;
		ownerId = owner.ownerId;
	}

	public override int Id => (int)PacketTypes.SetPropertiesTexture;

	public override void Deserialize(CircularBuffer buffer)
	{
		string localPath2;
		var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes2);
		localPath2 = Encoding.UTF8.GetString(bytes2);

		ulong ownerId2;
		buffer.Read(out ownerId2);

		bool propsDirty;
		buffer.Read(out propsDirty);

		if (propsDirty)
		{
			int filter;
			buffer.Read(out filter);

			int aniso;
			buffer.Read(out aniso);

			int wrapU;
			buffer.Read(out wrapU);

			int wrapV;
			buffer.Read(out wrapV);

			float mipBias;
			buffer.Read(out mipBias);
		}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(Encoding.UTF8.GetBytes(localPath));

		buffer.Write(ref ownerId);

		buffer.Write(ref propertiesDirty);

		if (propertiesDirty)
		{
			int filter = (int)filterMode;
			buffer.Write(ref filter);

			buffer.Write(ref anisoLevel);

			int wrapU = (int)wrapModeU;
			buffer.Write(ref wrapU);

			int wrapV = (int)wrapModeV;
			buffer.Write(ref wrapV);

			buffer.Write(ref mipMapBias);
		}
	}
}

public class SetDataTextureConnector : UpdatePacket<TextureConnector>
{
	TextureConnector.TextureUploadData uploadData;
	string localPath;
	ulong ownerId;
	public SetDataTextureConnector(TextureConnector owner) : base(owner)
	{
		uploadData = owner._textureUploadData;
		localPath = owner.LocalPath;
		ownerId = owner.ownerId;
	}

	public override int Id => (int)PacketTypes.SetDataTexture;

	public override void Deserialize(CircularBuffer buffer)
	{
		string localPath2;
		var bytes2 = new byte[Thundagun.MAX_STRING_LENGTH];
		buffer.Read(bytes2);
		localPath2 = Encoding.UTF8.GetString(bytes2);

		ulong ownerId2;
		buffer.Read(out ownerId2);

		int startMip;
		buffer.Read(out startMip);

		int format;
		buffer.Read(out format);

		double bitsPerPixel;
		buffer.Read(out bitsPerPixel);

		bool readable;
		buffer.Read(out readable);

		// read bitmap2d

		int arrLen;
		buffer.Read(out arrLen);

		byte[] buf = new byte[arrLen];
		buffer.Read(buf, timeout: 5000);

		//for (int i = 0; i < arrLen; i++)
		//{
			//byte byt;
			//buffer.Read(out byt);
		//}
	}

	public override void Serialize(CircularBuffer buffer)
	{
		buffer.Write(Encoding.UTF8.GetBytes(localPath));

		buffer.Write(ref ownerId);

		buffer.Write(ref uploadData.startMip);

		int format = (int)uploadData.Format;
		buffer.Write(ref format);

		double bitsPerPixel = uploadData.Format.GetBitsPerPixel();
		buffer.Write(ref bitsPerPixel);

		buffer.Write(ref uploadData.hint2D.readable);

		// write bitmap2d

		int arrLen = uploadData.bitmap2D.RawData.Length;
		buffer.Write(ref arrLen);

		buffer.Write(uploadData.bitmap2D.RawData, timeout: 5000);

		//int sliceSize = Math.Min(arrLen, 65536);
		//int i = 0;
		////UniLog.Log($"textureDebug. sliceSize: {sliceSize}, arrLen: {arrLen}");
		//while (i < arrLen - sliceSize)
		//{
		//	//UniLog.Log($"in loop.");
		//	byte[] buf = new byte[sliceSize];
		//	Array.Copy(uploadData.bitmap2D.RawData, i, buf, 0, sliceSize);
		//	buffer.Write(buf);
		//	i += sliceSize;
		//	sliceSize = Math.Min(arrLen - i, 65536);
		//	//UniLog.Log($"{i}, {sliceSize}");
		//}

		//foreach (var byt in uploadData.bitmap2D.RawData)
		//{
			//byte byt2 = byt;
			//buffer.Write(ref byt2);
		//}
	}
}
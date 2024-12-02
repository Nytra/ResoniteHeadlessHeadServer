using System;
using System.Runtime.CompilerServices;
using Elements.Core;
using FrooxEngine;
using UnityEngine;

public static class Conversions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static Vector2 ToUnity(this float2 v)
	{
		return *(Vector2*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static Vector3 ToUnity(this float3 v)
	{
		return *(Vector3*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static Vector4 ToUnity(this float4 v)
	{
		return *(Vector4*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static Quaternion ToUnity(this floatQ v)
	{
		return *(Quaternion*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color ToUnity(this in color c)
	{
		return new Color(c.r, c.g, c.b, c.a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color32 ToUnity(this in color32 c)
	{
		return new Color32(c.r, c.g, c.b, c.a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color ToUnity(this in colorX c, ColorProfile targetProfile)
	{
		color c2 = c.ToProfile(targetProfile);
		return ToUnity(in c2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color ToUnityAuto(this in colorX c, Engine engine)
	{
		color c2 = c.ToProfile((!engine.SystemInfo.UsingLinearSpace) ? ColorProfile.sRGB : ColorProfile.Linear);
		return ToUnity(in c2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UnityEngine.Rect ToUnity(this Elements.Core.Rect rect)
	{
		return new UnityEngine.Rect(ToUnity(rect.position), ToUnity(rect.size));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Matrix4x4 ToUnity(this in float4x4 m)
	{
		Matrix4x4 result = default(Matrix4x4);
		result.m00 = m.m00;
		result.m01 = m.m01;
		result.m02 = m.m02;
		result.m03 = m.m03;
		result.m10 = m.m10;
		result.m11 = m.m11;
		result.m12 = m.m12;
		result.m13 = m.m13;
		result.m20 = m.m20;
		result.m21 = m.m21;
		result.m22 = m.m22;
		result.m23 = m.m23;
		result.m30 = m.m30;
		result.m31 = m.m31;
		result.m32 = m.m32;
		result.m33 = m.m33;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float2 ToEngine(this Vector2 v)
	{
		return *(float2*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float3 ToEngine(this Vector3 v)
	{
		return *(float3*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static float4 ToEngine(this Vector4 v)
	{
		return *(float4*)(&v);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static floatQ ToEngine(this Quaternion v)
	{
		return *(floatQ*)(&v);
	}

	public static ColorProfile ToEngine(this ColorSpace space)
	{
		return space switch
		{
			ColorSpace.Linear => ColorProfile.Linear,
			ColorSpace.Gamma => ColorProfile.sRGB,
			ColorSpace.Uninitialized => ColorProfile.sRGB,
			_ => throw new NotSupportedException("Unsupported Unity ColorSpace: " + space),
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static color ToEngine(this in Color c)
	{
		return new color(c.r, c.g, c.b, c.a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static colorX ToEngineWithProfile(this in Color c, ColorProfile profile)
	{
		return new colorX(c.r, c.g, c.b, c.a, profile);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static color32 ToEngine(this in Color32 c)
	{
		return new color32(c.r, c.g, c.b, c.a);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Elements.Core.Rect ToEngine(UnityEngine.Rect rect)
	{
		float2 position = ToEngine(rect.position);
		float2 size = ToEngine(rect.size);
		return new Elements.Core.Rect(in position, in size);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float4x4 ToEngine(this in Matrix4x4 um)
	{
		return new float4x4(um.m00, um.m01, um.m02, um.m03, um.m10, um.m11, um.m12, um.m13, um.m20, um.m21, um.m22, um.m23, um.m30, um.m31, um.m32, um.m33);
	}
}

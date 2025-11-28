using System.Globalization;
using System.Numerics;

namespace AugGISConfigToolGUI;

public static class AugGisConfigUtils
{
	public static uint Vec3ToHex(Vector3 a_color)
	{
		uint r = (uint)(Math.Clamp(a_color.X, 0f, 1f) * 255f);
		uint g = (uint)(Math.Clamp(a_color.Y, 0f, 1f) * 255f);
		uint b = (uint)(Math.Clamp(a_color.Z, 0f, 1f) * 255f);
		return (r << 16) | (g << 8) | b;
	}
	
	public static Vector3 HexToVec3(uint a_hex)
	{
		float r = ((a_hex >> 16) & 0xFF) / 255f;
		float g = ((a_hex >> 8) & 0xFF) / 255f;
		float b = (a_hex & 0xFF) / 255f;
		return new Vector3(r, g, b);
	}
	
	public static bool TryParseHexColor(string a_hex, out uint a_colorValue)
	{
		a_colorValue = 0;

		if (string.IsNullOrWhiteSpace(a_hex))
			return false;

		// Remove common prefixes (#, 0x, &H)
		a_hex = a_hex.Trim();
		if (a_hex.StartsWith("#"))
			a_hex = a_hex.Substring(1);
		else if (a_hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			a_hex = a_hex.Substring(2);
		else if (a_hex.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
			a_hex = a_hex.Substring(2);

		if (a_hex.Length != 6 && a_hex.Length != 8)
			return false;

		return uint.TryParse(a_hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a_colorValue);
	}
}
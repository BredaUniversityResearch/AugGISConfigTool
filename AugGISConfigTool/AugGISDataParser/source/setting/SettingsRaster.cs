using System.Text.Json.Serialization;

namespace AugGISDataParser;

public class SettingsRaster
{
	[JsonIgnore] public Vector2 extentsMin;
	[JsonIgnore] public Vector2 extentsMax;
}
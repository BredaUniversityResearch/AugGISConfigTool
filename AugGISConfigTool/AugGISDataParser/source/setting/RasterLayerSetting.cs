using System.Text.Json.Serialization;

namespace AugGISDataParser;

public class RasterLayerSetting
{
	[JsonIgnore] public Vector2 extentsMin;
	[JsonIgnore] public Vector2 extentsMax;
}
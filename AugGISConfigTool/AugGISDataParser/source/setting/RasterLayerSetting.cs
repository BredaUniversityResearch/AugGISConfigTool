using DotSpatial.Data;
using Newtonsoft.Json;

namespace AugGISDataParser;

public class RasterLayerSetting
{
	public string name = String.Empty;
	public string rasterFilePath = string.Empty;

	public List<string> tags = new List<string>();

	[JsonProperty("mapping")] public List<RasterMapping> rasterMappings = new List<RasterMapping>();

	[JsonProperty("types")] public List<LayerType> rasterLayerTypes = new List<LayerType>();

	[JsonProperty("scale")] public RasterScale rasterScale = new RasterScale()
		{ minValue = 0, maxValue = 0, interpolation = RasterScale.EInterpolation.Lin };

	[JsonIgnore] public Vector2 extentsMin;
	[JsonIgnore] public Vector2 extentsMax;

	[JsonIgnore] public IRaster? rasterFile = null;
}
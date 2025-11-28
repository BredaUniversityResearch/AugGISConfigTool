using DotSpatial.Data;
using Newtonsoft.Json;

namespace AugGISDataParser;

public class RasterLayerSetting
{
	public string name = String.Empty;
	public string rasterFilePath = string.Empty;

	public List<string> tags = new List<string>();

	[JsonProperty("mapping")] public List<RasterMapping> rasterMappings = new List<RasterMapping>();

	[JsonProperty("types")] public List<LayerTypeData> rasterLayerTypes = new List<LayerTypeData>();

	[JsonProperty("scale")] public RasterScale rasterScale = new RasterScale()
		{ minValue = 0, maxValue = 0, interpolation = RasterScale.EInterpolation.Lin };

	public Vector2 extentsMin;
	public Vector2 extentsMax;
}
using Newtonsoft.Json;

namespace AugGISDataParser;

public class ConfigRasterLayer
{
	public string name = String.Empty;
	public string @short = String.Empty;
	
	[JsonProperty("data")]
	public string rasterFilePath = string.Empty;

	[JsonIgnore] public string originalRasterFilePath = string.Empty;
	
	public List<string> tags = new List<string>();

	[JsonProperty("mapping")] public List<RasterMapping> rasterMappings = new List<RasterMapping>();

	[JsonProperty("types")] public List<LayerType> rasterLayerTypes = new List<LayerType>();

	[JsonProperty("scale")] public RasterScale rasterScale = new RasterScale()
		{ minValue = 0, maxValue = 0, interpolation = RasterScale.EInterpolation.Lin };
}
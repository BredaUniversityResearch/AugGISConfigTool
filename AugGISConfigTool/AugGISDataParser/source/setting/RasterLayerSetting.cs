using DotSpatial.Data;
using Newtonsoft.Json;

namespace AugGISDataParser;

public class RasterLayerSetting
{
	public string name = String.Empty;
	public string rasterFilePath = string.Empty;

	public List<string> tags = new List<string>();
	
	[JsonProperty("mapping")]
	public List<RasterMapping> rasterMappings = new List<RasterMapping>();
	
	[JsonProperty("types")]
	public List<LayerType>  rasterLayerTypes = new List<LayerType>();
	
	[JsonProperty("scale")]
	public List<RasterScale> rasterScales = new List<RasterScale>();
	
	[JsonIgnore] public Vector2 extentsMin;
	[JsonIgnore] public Vector2 extentsMax;
	
	[JsonIgnore]
	public IRaster? rasterFile = null;
}
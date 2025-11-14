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
	public List<LayerType>  layerTypes = new List<LayerType>();
	
	[JsonIgnore]
	public List<string> layerTypeKeys = new List<string>();
	
	[JsonIgnore] public Vector2 extentsMin;
	[JsonIgnore] public Vector2 extentsMax;
	
	[JsonIgnore]
	public IRaster? rasterFile = null;
}
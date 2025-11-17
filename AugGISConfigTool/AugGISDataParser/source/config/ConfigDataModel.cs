using Newtonsoft.Json;

namespace AugGISDataParser
{
	public class ConfigDataModel
	{
		public string region = "";
		public string projection = "+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs";
		public double[] coordinate0 = new double[2];
		public double[] coordinate1 =  new double[2];

		[JsonProperty("raster_layers")]
		public List<ConfigRasterLayer> rasterLayers = new List<ConfigRasterLayer>();
		
		[JsonProperty("vector_layers")]
		public List<ConfigVectorLayer> vectorLayers = new List<ConfigVectorLayer>();
	}
	
	public class ConfigMetaData
	{
		//TODO add metadata info
	}
	
	public class JsonConfigObject
	{
		[JsonProperty("metadata")]
		public ConfigMetaData metaData = new ConfigMetaData();

		[JsonProperty("datamodel")]
		public ConfigDataModel dataModel = new ConfigDataModel();
	}
}
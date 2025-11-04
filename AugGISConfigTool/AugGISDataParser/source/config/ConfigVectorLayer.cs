using DotSpatial.Symbology;
using Newtonsoft.Json;

namespace AugGISDataParser
{
	public class ConfigVectorLayer
	{
		public struct LayerData
		{
			public double[,] points;
			public double[,] gaps;

			[JsonProperty("types")] 
			public List<int> typeIndices;

			public LayerData()
			{
				points = null;
				gaps = null;
				typeIndices = new List<int>();
			}
		}

		public struct LayerType
		{
			public string name;
		}
		
		public string name;
		public string @short;
		
		[JsonProperty("data")]
		public List<LayerData> layerData = new List<LayerData>();
		
		[JsonProperty("types")]
		public List<LayerType> layerTypes = new List<LayerType>();
		
		public List<string> tags = new List<string>();
	}
}
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

            [JsonProperty("meta")]
            public Dictionary<string,string> metaIndices;

            public LayerData()
			{
				points = null;
				gaps = null;
				typeIndices = new List<int>();
				metaIndices = new Dictionary<string, string>();
            }
		}
		
		public string name;
		public string @short;
		
		[JsonProperty("data")]
		public List<LayerData> layerData = new List<LayerData>();
		
		[JsonProperty("types")]
		public List<LayerTypeData> layerTypeData = new List<LayerTypeData>();
		
		public List<string> tags = new List<string>();
	}
}
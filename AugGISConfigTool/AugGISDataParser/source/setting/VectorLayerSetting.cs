using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Data;

namespace AugGISDataParser
{
	public class VectorLayerSetting
	{
		public string name = string.Empty;
		public string type = string.Empty;
		
		public List<string> tags = new List<string>();

		public string shapeFilePath = string.Empty;
		[JsonIgnore] public FeatureSet? featureSet = null;
		
		[JsonIgnore]
		public Dictionary<string, List<string>> attributeKeyToValues = new Dictionary<string, List<string>>();
		[JsonIgnore] 
		public Vector2 extentsMin;
		[JsonIgnore]
		public Vector2 extentsMax;
	}
}
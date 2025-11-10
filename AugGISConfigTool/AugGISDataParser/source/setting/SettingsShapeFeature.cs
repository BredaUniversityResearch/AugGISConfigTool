using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AugGISDataParser
{
	public class SettingsShapeFeature
	{
		public struct Attribute
		{
			public string key;
			public string? value;
		}

		public struct Data
		{
			public List<Vector2> points;
			public List<Attribute> attributes;

			public Data()
			{
				points = new List<Vector2>();
				attributes = new List<Attribute>();
			}
		}

		public string name = string.Empty;
		public string type = string.Empty;

		[JsonProperty("shape_feature_data")] public List<Data> data = new List<Data>();

		public List<string> tags = new List<string>();

		[JsonIgnore]
		public Dictionary<string, List<string>> attributeKeyToValues = new Dictionary<string, List<string>>();

		[JsonIgnore] public Vector2 extents_min;
		[JsonIgnore] public Vector2 extents_max;
	}
}
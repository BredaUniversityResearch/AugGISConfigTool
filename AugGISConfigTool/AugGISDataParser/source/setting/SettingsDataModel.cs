using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AugGISDataParser
{
	public class SettingsDataModel
	{
		public string region = "";

		public string projection =
			"+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs";

		public Vector2 coordinate0;
		public Vector2 coordinate1;

		public List<SettingsShapeFeature> shapeFeatures = new List<SettingsShapeFeature>();

		public void OnAfterLoad()
		{
			foreach (SettingsShapeFeature shapeFeature in shapeFeatures)
			{
				foreach (SettingsShapeFeature.Data data in shapeFeature.data)
				{
					foreach (SettingsShapeFeature.Attribute attribute in data.attributes)
					{
						if (shapeFeature.attributeKeyToValues.ContainsKey(attribute.key))
						{
							List<string> attributeValues = shapeFeature.attributeKeyToValues[attribute.key];

							if (!attributeValues.Contains(attribute.value))
							{
								attributeValues.Add(attribute.value);
							}
						}
						else
						{
							List<string> attributeValues = new List<string>();
							attributeValues.Add(attribute.value);
							shapeFeature.attributeKeyToValues[attribute.key] = attributeValues;
						}
					}
				}
			}
		}
	}
}
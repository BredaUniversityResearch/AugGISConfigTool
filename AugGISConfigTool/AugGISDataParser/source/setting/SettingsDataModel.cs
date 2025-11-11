using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotSpatial.Data;
using Newtonsoft.Json;

namespace AugGISDataParser
{
	public class SettingsDataModel
	{
		public string region = "";

		public string projection =
			"+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs";

		public Vector2 coordinate0;
		public Vector2 coordinate1;

		public List<VectorLayerSetting> vectorLayerSettings = new List<VectorLayerSetting>();

		public string gisDataDirectoryPath = string.Empty;

		[JsonIgnore] public int loadedShapeFileCount = 0;
		
		public void OnAfterLoad()
		{
			for (int i = 0; i < vectorLayerSettings.Count; i++ )
			{
				VectorLayerSetting vectorLayerSetting = vectorLayerSettings[i];

				if (vectorLayerSetting.shapefile == null)
				{
					vectorLayerSetting.shapefile = Shapefile.OpenFile(vectorLayerSetting.shapeFilePath);
				}
				
				foreach (IFeature feature in vectorLayerSetting.shapefile.Features)
				{
					for (int attribIndex = 0; attribIndex < feature.DataRow.Table.Columns.Count; attribIndex++)
					{
						string attribKey = feature.DataRow.Table.Columns[attribIndex].ToString();
						string? attribValue = feature.DataRow[attribIndex].ToString();

						if (vectorLayerSetting.attributeKeyToValues.ContainsKey(attribKey))
						{
							List<string> attributeValues = vectorLayerSetting.attributeKeyToValues[attribKey];

							if (!attributeValues.Contains(attribValue))
							{
								attributeValues.Add(attribValue);
							}
						}
						else
						{
							List<string> attributeValues = new List<string>();
							attributeValues.Add(attribValue);
							vectorLayerSetting.attributeKeyToValues[attribKey] = attributeValues;
						}
					}
				}
			}
		}
	}
}
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
		public List<RasterLayerSetting> rasterLayerSettings = new List<RasterLayerSetting>();
		
		public void OnAfterLoad()
		{
			foreach (VectorLayerSetting vectorLayerSetting in vectorLayerSettings)
			{
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

			foreach (RasterLayerSetting rasterLayerSetting in rasterLayerSettings)
			{
				if (rasterLayerSetting.rasterFile == null)
				{
					rasterLayerSetting.rasterFile = Raster.Open(rasterLayerSetting.rasterFilePath);
				}

				foreach (LayerType type in rasterLayerSetting.layerTypes)
				{
					rasterLayerSetting.layerTypeKeys.Add(type.name);
				}
			}
		}
	}
}
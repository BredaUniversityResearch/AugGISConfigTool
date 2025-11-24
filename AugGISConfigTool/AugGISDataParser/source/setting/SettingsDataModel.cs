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
			// ReSharper disable once UnusedVariable
			DotSpatial.Data.Rasters.GdalExtension.GdalRasterProvider grp = new DotSpatial.Data.Rasters.GdalExtension.GdalRasterProvider();
			
			foreach (VectorLayerSetting vectorLayerSetting in vectorLayerSettings)
			{
				if (vectorLayerSetting.featureSet == null)
				{
					if (vectorLayerSetting.shapeFilePath.EndsWith(".shp"))
					{
						vectorLayerSetting.featureSet = Shapefile.OpenFile(vectorLayerSetting.shapeFilePath);
					}
					else if (vectorLayerSetting.shapeFilePath.EndsWith(".json"))
					{
						vectorLayerSetting.featureSet = SettingsDataCreator.GetFeatureSetFromGeoJson(vectorLayerSetting.shapeFilePath);
					}
				}
				
				foreach (IFeature feature in vectorLayerSetting.featureSet.Features)
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
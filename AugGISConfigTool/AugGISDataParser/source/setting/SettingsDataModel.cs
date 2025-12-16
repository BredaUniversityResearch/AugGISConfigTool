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
					else if (vectorLayerSetting.shapeFilePath.EndsWith(".json") ||
					         vectorLayerSetting.shapeFilePath.EndsWith(".geojson"))
					{
						GeoJsonFeatureSets geoJsonFeatureSets = SettingsDataCreator.GetFeatureSetFromGeoJson(vectorLayerSetting.shapeFilePath);

						if (vectorLayerSetting.tags.Contains("Point"))
						{
							vectorLayerSetting.featureSet = geoJsonFeatureSets.pointFeatureSet;
						}
						else if (vectorLayerSetting.tags.Contains("Line"))
						{
							vectorLayerSetting.featureSet = geoJsonFeatureSets.lineFeatureSet;
						}
						else if (vectorLayerSetting.tags.Contains("Polygon"))
						{
							vectorLayerSetting.featureSet = geoJsonFeatureSets.polygonFeatureSet;
						}
						else
						{
							throw new Exception("Layer does NOT contain any supported tag");
						}
					}
				}
				
				if (vectorLayerSetting.selectedTypeKey == string.Empty)
				{
					vectorLayerSetting.selectedTypeKey = vectorLayerSetting.attributeKeyToValues.Keys.ElementAt(0);
				}
			}
			
			
		}
	}
}
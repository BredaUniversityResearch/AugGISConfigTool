using System.Data;
using DotSpatial.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;
using DotSpatial.Projections;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Feature = DotSpatial.Data.Feature;
using IFeature = DotSpatial.Data.IFeature;

namespace AugGISDataParser
{
	public static class SettingsDataCreator
	{
		private static int WGS84EsriCode = 4326;
		private static int ETRS89EsriCode = 3035;
		
		private static GeoJsonReader jsonReader = new GeoJsonReader();
		public static SettingsDataModel CreateSettingsDataModelFromGisData(string a_gisDataDirectoryPath)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			SettingsDataModel settingsDataModel = new SettingsDataModel();

			string[] files = Directory.GetFiles(a_gisDataDirectoryPath);
			List<string> shpFiles = new List<string>();
			List<string> rasterTifFiles = new List<string>();
			List<string> geoJsonFiles = new List<string>();
			
			foreach (string file in files)
			{
				if (file.EndsWith(".shp"))
				{
					shpFiles.Add(file);
				}
				else if (file.EndsWith(".json") || file.EndsWith(".geojson"))
				{
					geoJsonFiles.Add(file);
				}
				else if (file.EndsWith(".tiff") || file.EndsWith(".tif"))
				{
					rasterTifFiles.Add(file);
				}
				else
				{
					Console.WriteLine("[Warning] Unsupported GIS file detected: {0}", file);
				}
			}

			Vector2 coordinateMin = new Vector2(double.MaxValue, double.MaxValue);
			Vector2 coordinateMax = new Vector2(double.MinValue, double.MinValue);

			foreach (string shpFilePath in shpFiles)
			{
				VectorLayerSetting vectorLayerSetting = ParseShapeFile(shpFilePath);

				settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
				vectorLayerSetting.shapeFilePath = shpFilePath;
			}
			
			foreach (string geoJsonFile in geoJsonFiles)
			{
				GeoJsonFeatureSets geoJsonFeatureSets = GetFeatureSetFromGeoJson(geoJsonFile);
				string jsonFileName = Path.GetFileName(geoJsonFile);
				
				if (geoJsonFeatureSets.pointFeatureSet?.Features.Count > 0)
				{
					VectorLayerSetting vectorLayerSetting = ParseFeatureSet(geoJsonFeatureSets.pointFeatureSet);
					
					vectorLayerSetting.name = jsonFileName + "_point";
					vectorLayerSetting.shapeFilePath = geoJsonFile;
					settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
				}
				
				if (geoJsonFeatureSets.lineFeatureSet?.Features.Count > 0)
				{
					VectorLayerSetting vectorLayerSetting = ParseFeatureSet(geoJsonFeatureSets.lineFeatureSet);
					
					vectorLayerSetting.shapeFilePath = geoJsonFile;
					vectorLayerSetting.name = jsonFileName + "_line";
					settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
				}
				
				if (geoJsonFeatureSets.polygonFeatureSet?.Features.Count > 0)
				{
					VectorLayerSetting vectorLayerSetting = ParseFeatureSet(geoJsonFeatureSets.polygonFeatureSet);
					
					vectorLayerSetting.shapeFilePath = geoJsonFile;
					vectorLayerSetting.name = jsonFileName + "_polygon";
					settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
				}
			}
			
			// ReSharper disable once UnusedVariable
			DotSpatial.Data.Rasters.GdalExtension.GdalRasterProvider grp =
				new DotSpatial.Data.Rasters.GdalExtension.GdalRasterProvider();
			
			foreach (string rasterFile in rasterTifFiles)
			{
				RasterLayerSetting rasterLayerSetting = ParseGeoTifFile(rasterFile);
				
				if (rasterLayerSetting.extentsMin.x < coordinateMin.x) coordinateMin.x = rasterLayerSetting.extentsMin.x;
				if (rasterLayerSetting.extentsMin.y < coordinateMin.y) coordinateMin.y = rasterLayerSetting.extentsMin.y;

				if (rasterLayerSetting.extentsMax.x > coordinateMax.x) coordinateMax.x = rasterLayerSetting.extentsMax.x;
				if (rasterLayerSetting.extentsMax.y > coordinateMax.y) coordinateMax.y = rasterLayerSetting.extentsMax.y;
				
				settingsDataModel.rasterLayerSettings.Add(rasterLayerSetting);
			}

			foreach (VectorLayerSetting loadedVectorLayerSetting in settingsDataModel.vectorLayerSettings)
			{
				if (loadedVectorLayerSetting.extentsMin.x < coordinateMin.x) coordinateMin.x = loadedVectorLayerSetting.extentsMin.x;
				if (loadedVectorLayerSetting.extentsMin.y < coordinateMin.y) coordinateMin.y = loadedVectorLayerSetting.extentsMin.y;

				if (loadedVectorLayerSetting.extentsMax.x > coordinateMax.x) coordinateMax.x = loadedVectorLayerSetting.extentsMax.x;
				if (loadedVectorLayerSetting.extentsMax.y > coordinateMax.y) coordinateMax.y = loadedVectorLayerSetting.extentsMax.y;
			}

			settingsDataModel.coordinate0 = coordinateMin;
			settingsDataModel.coordinate1 = coordinateMax;

			return settingsDataModel;
		}

		public static VectorLayerSetting ParseShapeFile(string a_shpFilePath)
		{
			Shapefile shapefile = Shapefile.OpenFile(a_shpFilePath);
			VectorLayerSetting vectorLayerSetting = ParseFeatureSet(shapefile);
			shapefile.Close();
			return vectorLayerSetting;
		}

		public static GeoJsonFeatureSets GetFeatureSetFromGeoJson(string a_geoJsonPath)
		{
			string jsonString = File.ReadAllText(a_geoJsonPath);
			FeatureCollection featureCollection = jsonReader.Read<FeatureCollection>(jsonString);

			FeatureSet pointFeatureSet = new FeatureSet();
			FeatureSet lineFeatureSet = new FeatureSet();
			FeatureSet polygonFeatureSet = new FeatureSet();
			
			foreach (NetTopologySuite.Features.IFeature netTopologyFeature in featureCollection)
			{
				switch (netTopologyFeature.Geometry.GeometryType)
				{
					case Geometry.TypeNamePoint:
						pointFeatureSet.AddFeature(netTopologyFeature.Geometry);
						ConvertAttributesFromNetTopologyToDotSpatial(netTopologyFeature, pointFeatureSet);
						break;
					case Geometry.TypeNameLineString:
						lineFeatureSet.AddFeature(netTopologyFeature.Geometry);
						ConvertAttributesFromNetTopologyToDotSpatial(netTopologyFeature, lineFeatureSet);
						break;
					case Geometry.TypeNamePolygon:
						polygonFeatureSet.AddFeature(netTopologyFeature.Geometry);
						ConvertAttributesFromNetTopologyToDotSpatial(netTopologyFeature, polygonFeatureSet);
						break;
					default:
						throw new Exception("Unknown geometry type");
						break;
				}
			}

			ProjectionInfo ogGeoJsonProjection = ProjectionInfo.FromEpsgCode(WGS84EsriCode);
			pointFeatureSet.Projection = ogGeoJsonProjection;
			lineFeatureSet.Projection = ogGeoJsonProjection;
			polygonFeatureSet.Projection = ogGeoJsonProjection;

			if (pointFeatureSet.Features.Count > 0)
			{
				pointFeatureSet.Reproject(ProjectionInfo.FromEpsgCode(ETRS89EsriCode));
				pointFeatureSet.UpdateExtent();
			}

			if (lineFeatureSet.Features.Count > 0)
			{
				lineFeatureSet.Reproject(ProjectionInfo.FromEpsgCode(ETRS89EsriCode));
				pointFeatureSet.UpdateExtent();
			}

			if (polygonFeatureSet.Features.Count > 0)
			{
				polygonFeatureSet.Reproject(ProjectionInfo.FromEpsgCode(ETRS89EsriCode));
				pointFeatureSet.UpdateExtent();
			}

			GeoJsonFeatureSets geoJsonFeatureSets =
				new GeoJsonFeatureSets(pointFeatureSet, lineFeatureSet, polygonFeatureSet);
			return geoJsonFeatureSets;
		}

		private static void ConvertAttributesFromNetTopologyToDotSpatial(NetTopologySuite.Features.IFeature a_netTopologyFeature, FeatureSet a_featureSet)
		{
			if (a_netTopologyFeature.Attributes == null || a_netTopologyFeature.Attributes.Count == 0)
			{
				return;
			}

			foreach (string key in a_netTopologyFeature.Attributes.GetNames())
			{
				if (a_featureSet.DataTable.Columns.Contains(key))
				{
					continue;
				}

				a_featureSet.DataTable.Columns.Add(new DataColumn(key));
			}
			
			object[] attributeValues = a_netTopologyFeature.Attributes.GetValues();
			DataRow row = a_featureSet.DataTable.NewRow();
			row.ItemArray = attributeValues;
			foreach (IFeature feature in a_featureSet.Features)
			{
				feature.DataRow.ItemArray = attributeValues;
			}
		}
		

		public static VectorLayerSetting ParseFeatureSet(FeatureSet a_featureSet)
		{
			VectorLayerSetting vectorLayerSetting = new VectorLayerSetting();
			
			vectorLayerSetting.name = a_featureSet.Name ?? string.Empty;
			vectorLayerSetting.tags.Add(a_featureSet.FeatureType.ToString());

			vectorLayerSetting.extentsMin = new Vector2(a_featureSet.Extent.MinX, a_featureSet.Extent.MinY);
			vectorLayerSetting.extentsMax = new Vector2(a_featureSet.Extent.MaxX, a_featureSet.Extent.MaxY);
			vectorLayerSetting.featureSet = a_featureSet;

			foreach (IFeature feature in vectorLayerSetting.featureSet.Features)
			{
				if (feature.DataRow == null)
				{
					continue;
				}
				for (int attribIndex = 0; attribIndex < feature.DataRow.Table.Columns.Count; attribIndex++)
				{
					string attribKey = feature.DataRow.Table.Columns[attribIndex].ToString();
					object? attribValue = feature.DataRow[attribIndex];
					
					//skip null values
					if (attribValue == DBNull.Value)
					{
						continue;
					}
					
					if (vectorLayerSetting.attributeKeyToValues.ContainsKey(attribKey))
					{
						List<object?> attributeValues = vectorLayerSetting.attributeKeyToValues[attribKey];

						if (!attributeValues.Contains(attribValue))
						{
							attributeValues.Add(attribValue);
						}
					}
					else
					{
						List<object?> attributeValues = new List<object?> {attribValue};
						vectorLayerSetting.attributeKeyToValues[attribKey] = attributeValues;
					}
				}
			}
			
			return vectorLayerSetting;
		}
		
		private static RasterLayerSetting ParseGeoTifFile(string a_rasterFilePath)
		{
			RasterLayerSetting rasterLayerSetting = new RasterLayerSetting();
			rasterLayerSetting.rasterFilePath = a_rasterFilePath;
			
			IRaster rasterFile = Raster.Open(a_rasterFilePath);
			
			rasterLayerSetting.name = rasterFile.Name;
			rasterLayerSetting.extentsMin = new Vector2(rasterFile.Extent.MinX, rasterFile.Extent.MinY);
			rasterLayerSetting.extentsMax = new Vector2(rasterFile.Extent.MaxX, rasterFile.Extent.MaxY);
			rasterLayerSetting.tags.Add("Raster");
			
			rasterFile.Close();
			return rasterLayerSetting;
		}

		public static void SaveSettingsDataModelToFile(SettingsDataModel a_settingsDataModel,
			string a_fileName = @"settings.json")
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.Formatting = Formatting.Indented;

			using (StreamWriter sw = new StreamWriter(a_fileName))
			{
				using (JsonWriter writer = new JsonTextWriter(sw))
				{
					serializer.Serialize(writer, a_settingsDataModel);
				}
			}
		}
		
		public static SettingsDataModel? LoadSettingsDataModelFromFile(string a_settingsFilePath = @"settings.json")
		{
			SettingsDataModel? settingsDataModel;
			
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.Formatting = Formatting.Indented;

			using (StreamReader sr = new StreamReader(a_settingsFilePath))
			using (JsonTextReader reader = new JsonTextReader(sr) )
			{
				settingsDataModel = serializer.Deserialize<SettingsDataModel>(reader);
			}

			settingsDataModel?.OnAfterLoad();
			
			return settingsDataModel;
		}
	}
}
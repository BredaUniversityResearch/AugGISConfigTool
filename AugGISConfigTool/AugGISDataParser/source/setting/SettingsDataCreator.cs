using DotSpatial.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Feature = DotSpatial.Data.Feature;
using IFeature = DotSpatial.Data.IFeature;

namespace AugGISDataParser
{
	public static class SettingsDataCreator
	{
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

				if (vectorLayerSetting.extentsMin.x < coordinateMin.x) coordinateMin.x = vectorLayerSetting.extentsMin.x;
				if (vectorLayerSetting.extentsMin.y < coordinateMin.y) coordinateMin.y = vectorLayerSetting.extentsMin.y;

				if (vectorLayerSetting.extentsMax.x > coordinateMax.x) coordinateMax.x = vectorLayerSetting.extentsMax.x;
				if (vectorLayerSetting.extentsMax.y > coordinateMax.y) coordinateMax.y = vectorLayerSetting.extentsMax.y;

				settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
				vectorLayerSetting.shapeFilePath = shpFilePath;
			}
			
			foreach (string geoJsonFile in geoJsonFiles)
			{
				FeatureSet featureSet = GetFeatureSetFromGeoJson(geoJsonFile);
				VectorLayerSetting vectorLayerSetting = ParseFeatureSet(featureSet);
				vectorLayerSetting.shapeFilePath = geoJsonFile;
				settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
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

			settingsDataModel.coordinate0 = coordinateMin;
			settingsDataModel.coordinate1 = coordinateMax;

			return settingsDataModel;
		}

		public static VectorLayerSetting ParseShapeFile(string a_shpFilePath)
		{
			Shapefile shapefile = Shapefile.OpenFile(a_shpFilePath);
			VectorLayerSetting vectorLayerSetting = ParseFeatureSet(shapefile);
			vectorLayerSetting.featureSet = shapefile;
			shapefile.Close();
			return vectorLayerSetting;
		}

		public static FeatureSet GetFeatureSetFromGeoJson(string a_geoJsonPath)
		{
			string jsonString = File.ReadAllText(a_geoJsonPath);
			FeatureCollection featureCollection = jsonReader.Read<FeatureCollection>(jsonString);
			
			FeatureSet featureSet = new FeatureSet();
			foreach (NetTopologySuite.Features.IFeature netTopologyFeature in featureCollection)
			{
				featureSet.AddFeature(netTopologyFeature.Geometry);
			}

			return featureSet;
		}

		public static VectorLayerSetting ParseFeatureSet(FeatureSet a_featureSet)
		{
			VectorLayerSetting vectorLayerSetting = new VectorLayerSetting();
			
			vectorLayerSetting.name = a_featureSet.Name ?? string.Empty;
			vectorLayerSetting.tags.Add(a_featureSet.FeatureType.ToString());

			vectorLayerSetting.extentsMin = new Vector2(a_featureSet.Extent.MinX, a_featureSet.Extent.MinY);
			vectorLayerSetting.extentsMax = new Vector2(a_featureSet.Extent.MaxX, a_featureSet.Extent.MaxY);
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
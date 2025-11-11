using DotSpatial.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;

namespace AugGISDataParser
{
	public static class SettingsDataCreator
	{
		public static SettingsDataModel CreateSettingsDataModelFromGisData(string a_gisDataDirectoryPath)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			SettingsDataModel settingsDataModel = new SettingsDataModel();
			settingsDataModel.gisDataDirectoryPath = a_gisDataDirectoryPath;

			string[] files = Directory.GetFiles(a_gisDataDirectoryPath);
			List<string> shpFiles = new List<string>();
			List<string> rasterTifFiles = new List<string>();
			
			foreach (string file in files)
			{
				if (file.EndsWith(".shp"))
				{
					shpFiles.Add(file);
				}
				else if (file.EndsWith(".tiff"))
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
				Shapefile shapefile = Shapefile.OpenFile(shpFilePath);
				VectorLayerSetting vectorLayerSetting = ParseShapeFile(shapefile);
				settingsDataModel.loadedShapeFileCount++;

				if (vectorLayerSetting.extentsMin.x < coordinateMin.x) coordinateMin.x = vectorLayerSetting.extentsMin.x;
				if (vectorLayerSetting.extentsMin.y < coordinateMin.y) coordinateMin.y = vectorLayerSetting.extentsMin.y;

				if (vectorLayerSetting.extentsMax.x > coordinateMax.x) coordinateMax.x = vectorLayerSetting.extentsMax.x;
				if (vectorLayerSetting.extentsMax.y > coordinateMax.y) coordinateMax.y = vectorLayerSetting.extentsMax.y;

				settingsDataModel.vectorLayerSettings.Add(vectorLayerSetting);
				vectorLayerSetting.shapeFilePath = shpFilePath;
			}
			
			// ReSharper disable once UnusedVariable
			DotSpatial.Data.Rasters.GdalExtension.GdalRasterProvider grp = new DotSpatial.Data.Rasters.GdalExtension.GdalRasterProvider();
			
			foreach (string rasterFile in rasterTifFiles)
			{
				IRaster raster = Raster.Open(rasterFile);
				if (raster == null)
				{
					Console.WriteLine("Invalid Raster File: {0}", rasterFile);
				}
				else
				{
					Console.WriteLine(raster.NumColumns.ToString());
					Console.WriteLine(raster.NumRows.ToString());
					foreach (var categoryName in raster.CategoryNames())
					{
						Console.WriteLine(categoryName);
					}
				}
			}

			settingsDataModel.coordinate0 = coordinateMin;
			settingsDataModel.coordinate1 = coordinateMax;

			return settingsDataModel;
		}

		private static VectorLayerSetting ParseShapeFile(Shapefile a_shapefile)
		{
			VectorLayerSetting vectorLayerSetting = new VectorLayerSetting();
			vectorLayerSetting.name = a_shapefile.Name;
			vectorLayerSetting.tags.Add(a_shapefile.FeatureType.ToString());

			vectorLayerSetting.extentsMin = new Vector2(a_shapefile.Extent.MinX, a_shapefile.Extent.MinY);
			vectorLayerSetting.extentsMax = new Vector2(a_shapefile.Extent.MaxX, a_shapefile.Extent.MaxY);

			vectorLayerSetting.shapefile = a_shapefile;
			return vectorLayerSetting;
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
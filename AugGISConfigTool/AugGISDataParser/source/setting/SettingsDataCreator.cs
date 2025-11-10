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
				SettingsShapeFeature feature = ParseShapeFile(shpFilePath);

				if (feature.extents_min.x < coordinateMin.x) coordinateMin.x = feature.extents_min.x;
				if (feature.extents_min.y < coordinateMin.y) coordinateMin.y = feature.extents_min.y;

				if (feature.extents_max.x > coordinateMax.x) coordinateMax.x = feature.extents_max.x;
				if (feature.extents_max.y > coordinateMax.y) coordinateMax.y = feature.extents_max.y;

				settingsDataModel.shapeFeatures.Add(feature);
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
					Console.Write(raster.Extent.ToString());
				}
			}

			settingsDataModel.coordinate0 = coordinateMin;
			settingsDataModel.coordinate1 = coordinateMax;

			return settingsDataModel;
		}

		private static SettingsShapeFeature ParseShapeFile(string a_shapeFilePath)
		{
			Shapefile shapefile = Shapefile.OpenFile(a_shapeFilePath);
			SettingsShapeFeature shapeFeature = new SettingsShapeFeature();

			shapeFeature.name = shapefile.Name;
			shapeFeature.tags.Add(shapefile.FeatureType.ToString());

			shapeFeature.extents_min = new Vector2(shapefile.Extent.MinX, shapefile.Extent.MinY);
			shapeFeature.extents_max = new Vector2(shapefile.Extent.MaxX, shapefile.Extent.MaxY);

			for (int shapeFeatureIndex = 0; shapeFeatureIndex < shapefile.Features.Count; shapeFeatureIndex++)
			{
				SettingsShapeFeature.Data settingShapeData = new SettingsShapeFeature.Data();

				IFeature feature = shapefile.Features[shapeFeatureIndex];

				for (int attribIndex = 0; attribIndex < feature.DataRow.Table.Columns.Count; attribIndex++)
				{
					string attribKey = feature.DataRow.Table.Columns[attribIndex].ToString();
					string? attribValue = feature.DataRow[attribIndex].ToString();

					SettingsShapeFeature.Attribute attribute = new SettingsShapeFeature.Attribute()
						{ key = attribKey, value = attribValue };
					settingShapeData.attributes.Add(attribute);
				}

				for (int coordinateIndex = 0; coordinateIndex < feature.Geometry.Coordinates.Length; coordinateIndex++)
				{
					NetTopologySuite.Geometries.Coordinate coordinate = feature.Geometry.Coordinates[coordinateIndex];
					Vector2 coordinateVector = new Vector2(coordinate.X, coordinate.Y);
					settingShapeData.points.Add(coordinateVector);
				}

				shapeFeature.data.Add(settingShapeData);
			}

			return shapeFeature;
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
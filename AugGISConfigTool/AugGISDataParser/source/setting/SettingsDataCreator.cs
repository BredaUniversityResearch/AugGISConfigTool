using DotSpatial.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AugGISDataParser
{
	public static class SettingsDataCreator
	{
		public static SettingsDataModel CreateSettingsDataModelFromGISData(string gisDataDirectoryPath)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			SettingsDataModel settingsDataModel = new SettingsDataModel();

			string[] files = Directory.GetFiles(gisDataDirectoryPath);
			List<string> shpFiles = new List<string>();

			foreach (string file in files)
			{
				if (file.EndsWith(".shp"))
				{
					shpFiles.Add(file);
				}
				else
				{
					Console.WriteLine("[Warning] Unsupported GIS file detected in directory {0}", gisDataDirectoryPath);
				}
			}

			Vector2 coordinate_min = new Vector2(double.MaxValue, double.MaxValue);
			Vector2 coordinate_max = new Vector2(double.MinValue, double.MinValue);

			for (int shpFileIndex = 0; shpFileIndex < shpFiles.Count; shpFileIndex++)
			{
				string shpFilePath = shpFiles[shpFileIndex];
				SettingsShapeFeature feature = ParseShapeFile(shpFilePath, settingsDataModel);

				if (feature.extents_min.x < coordinate_min.x) coordinate_min.x = feature.extents_min.x;
				if (feature.extents_min.y < coordinate_min.y) coordinate_min.y = feature.extents_min.y;

				if (feature.extents_max.x > coordinate_max.x) coordinate_max.x = feature.extents_max.x;
				if (feature.extents_max.y > coordinate_max.y) coordinate_max.y = feature.extents_max.y;

				settingsDataModel.shapeFeatures.Add(feature);
			}

			settingsDataModel.coordinate0 = coordinate_min;
			settingsDataModel.coordinate1 = coordinate_max;

			return settingsDataModel;
		}

		private static SettingsShapeFeature ParseShapeFile(string shapeFilePath, SettingsDataModel dataModel)
		{
			Shapefile shapefile = Shapefile.OpenFile(shapeFilePath);
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

					if (shapeFeature.attributeKeyToValues.ContainsKey(attribKey))
					{
						List<string> attributeValues = shapeFeature.attributeKeyToValues[attribKey];

						if (!attributeValues.Contains(attribValue))
						{
							attributeValues.Add(attribValue);
						}
					}
					else
					{
						List<string> attributeValues = new List<string>();
						attributeValues.Add(attribValue);
						shapeFeature.attributeKeyToValues[attribKey] = attributeValues;
					}
				}

				for (int coordinateIndex = 0; coordinateIndex < feature.Geometry.Coordinates.Length; coordinateIndex++)
				{
					NetTopologySuite.Geometries.Coordinate coordinate = feature.Geometry.Coordinates[coordinateIndex];
					Vector2 coordinateXY = new Vector2(coordinate.X, coordinate.Y);
					settingShapeData.points.Add(coordinateXY);
				}

				shapeFeature.data = settingShapeData;
			}

			return shapeFeature;
		}

		public static void SaveSettingsDataModelToFile(SettingsDataModel settingsDataModel,
			string fileName = @"settings.json")
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

			using (StreamWriter sw = new StreamWriter(fileName))
			{
				using (JsonWriter writer = new JsonTextWriter(sw))
				{
					serializer.Serialize(writer, settingsDataModel);
				}
			}
		}
	}
}
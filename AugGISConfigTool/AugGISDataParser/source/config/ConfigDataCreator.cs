using DotSpatial.Data;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AugGISDataParser
{
	public static class ConfigDataCreator
	{
		public static JsonConfigObject CreateConfigDataModelFromSettings(SettingsDataModel a_settingsDataModel)
		{
			JsonConfigObject jsonConfigObject = new JsonConfigObject();
			
			jsonConfigObject.dataModel.coordinate0 = a_settingsDataModel.coordinate0.ToArray();
			jsonConfigObject.dataModel.coordinate1 = a_settingsDataModel.coordinate1.ToArray();
			jsonConfigObject.dataModel.region = a_settingsDataModel.region;
			jsonConfigObject.dataModel.projection = a_settingsDataModel.projection;

			foreach (VectorLayerSetting vectorLayerSetting in a_settingsDataModel.vectorLayerSettings)
			{
				if (vectorLayerSetting.shapefile == null)
				{
					Console.WriteLine("Error: Vector Layer Settings shape file is Null!");
					continue;
				}
				ConfigVectorLayer configVectorLayer = new ConfigVectorLayer();
				jsonConfigObject.dataModel.vectorLayers.Add(configVectorLayer);

				configVectorLayer.name = vectorLayerSetting.name;
				configVectorLayer.@short = vectorLayerSetting.name;

				foreach (string tag in vectorLayerSetting.tags)
				{
					configVectorLayer.tags.Add(tag);
				}

				List<string> types = vectorLayerSetting.attributeKeyToValues[vectorLayerSetting.type];

				foreach (string type in types)
				{
					configVectorLayer.layerTypes.Add(new LayerType() { name = type });
				}

				foreach (IFeature shapeFeature in vectorLayerSetting.shapefile.Features)
				{
					ConfigVectorLayer.LayerData configLayerData = new ConfigVectorLayer.LayerData
					{
						points = new double[ shapeFeature.Geometry.Coordinates.Length, 2]
					};

					for (int coordinateIndex = 0; coordinateIndex <  shapeFeature.Geometry.Coordinates.Length; coordinateIndex++)
					{
						Coordinate coordinate = shapeFeature.Geometry.Coordinates[coordinateIndex];
						configLayerData.points[coordinateIndex, 0] = coordinate.X;
						configLayerData.points[coordinateIndex, 1] = coordinate.X;
					}
					
					configLayerData.gaps = new double[0,0]; //TODO handle gaps
					
					List<string> attributes = vectorLayerSetting.attributeKeyToValues[vectorLayerSetting.type];
					
					int attributeIndex = shapeFeature.DataRow.Table.Columns.IndexOf(vectorLayerSetting.type);
					string value = shapeFeature.DataRow[attributeIndex].ToString();
					
					configLayerData.typeIndices.Add(attributes.IndexOf(value));
					configVectorLayer.layerData.Add(configLayerData);
				}
			}

			foreach (RasterLayerSetting rasterLayerSetting in a_settingsDataModel.rasterLayerSettings)
			{
				ConfigRasterLayer configRasterLayer = new ConfigRasterLayer();
				configRasterLayer.name = rasterLayerSetting.name;
				configRasterLayer.@short = rasterLayerSetting.name;	//TODO handle @short name (maybe make it a setting)
				
				configRasterLayer.rasterScale = rasterLayerSetting.rasterScale;
				configRasterLayer.rasterLayerTypes = rasterLayerSetting.rasterLayerTypes;
				configRasterLayer.rasterMappings = rasterLayerSetting.rasterMappings;
				configRasterLayer.tags = rasterLayerSetting.tags;
				
				configRasterLayer.rasterFilePath = rasterLayerSetting.rasterFilePath; //TODO replace with png filepath
				
				jsonConfigObject.dataModel.rasterLayers.Add(configRasterLayer);
			}

			return jsonConfigObject;
		}
		
		public static void SaveConfigObjectToFile(JsonConfigObject a_configObject, string a_fileName = @"config.json")
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.Formatting = Formatting.Indented;
			
			using (StreamWriter sw = new StreamWriter(a_fileName))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				serializer.Serialize(writer, a_configObject);
			}
		}
	}
}
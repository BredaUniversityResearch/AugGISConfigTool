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

			foreach (SettingsShapeFeature shapeFeature in a_settingsDataModel.shapeFeatures)
			{
				ConfigVectorLayer configVectorLayer = new ConfigVectorLayer();
				jsonConfigObject.dataModel.vectorLayers.Add(configVectorLayer);

				configVectorLayer.name = shapeFeature.name;
				configVectorLayer.@short = shapeFeature.name;

				foreach (string tag in shapeFeature.tags)
				{
					configVectorLayer.tags.Add(tag);
				}

				List<string> types = shapeFeature.attributeKeyToValues[shapeFeature.type];

				foreach (string type in types)
				{
					configVectorLayer.layerTypes.Add(new ConfigVectorLayer.LayerType() { name = type });
				}

				foreach (SettingsShapeFeature.Data settingData in shapeFeature.data)
				{
					ConfigVectorLayer.LayerData configLayerData = new ConfigVectorLayer.LayerData();
					
					configLayerData.points = new double[ settingData.points.Count, 2];
					for (int pointIndex = 0; pointIndex < settingData.points.Count; pointIndex++)
					{
						Vector2 point = settingData.points[pointIndex];
						configLayerData.points[pointIndex, 0] = point.x;
						configLayerData.points[pointIndex, 1] = point.y;
					}
					
					configLayerData.gaps = new double[0,0]; //TODO handle gaps

					List<string> attributes = shapeFeature.attributeKeyToValues[shapeFeature.type];

					var feature = shapeFeature;
					var attribute = settingData.attributes.Single(x => x.key == feature.type);
					string value = attribute.value;
					
					configLayerData.typeIndices.Add(attributes.IndexOf(value));
					configVectorLayer.layerData.Add(configLayerData);
				}
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
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace AugGISDataParser
{
    public static class ConfigDataCreator
    {
        public static async Task DownloadTerrestrisImage(string filePath, string bbox)
        {
            var url = $"https://ows.terrestris.de/osm/service"
                + $"?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap"
                + $"&LAYERS=OSM-WMS&STYLES=default&SRS=EPSG:3035"
                + $"&BBOX={bbox}&WIDTH=2048&HEIGHT=2048"
                + $"&FORMAT=image/png";

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(filePath, imageBytes);
        }

        public static JsonConfigObject CreateConfigDataModelFromSettings(SettingsDataModel a_settingsDataModel)
        {
            JsonConfigObject jsonConfigObject = new JsonConfigObject();

            jsonConfigObject.dataModel.coordinate0 = a_settingsDataModel.coordinate0.ToArray();
            jsonConfigObject.dataModel.coordinate1 = a_settingsDataModel.coordinate1.ToArray();
            jsonConfigObject.dataModel.region = a_settingsDataModel.region;
            jsonConfigObject.dataModel.projection = a_settingsDataModel.projection;

            foreach (VectorLayerSetting vectorLayerSetting in a_settingsDataModel.vectorLayerSettings)
            {
                if (vectorLayerSetting.features == null)
                {
                    Console.WriteLine("Error: Vector Layer Settings features are Null!");
                    continue;
                }

                ConfigVectorLayer configVectorLayer = new ConfigVectorLayer();
                jsonConfigObject.dataModel.vectorLayers.Add(configVectorLayer);

                configVectorLayer.name = vectorLayerSetting.name;
                configVectorLayer.@short = vectorLayerSetting.name;
                configVectorLayer.layerTypeData = vectorLayerSetting.layerTypeData;

                foreach (string tag in vectorLayerSetting.tags)
                {
                    configVectorLayer.tags.Add(tag);
                }

                List<object?> typeValues = vectorLayerSetting.attributeKeyToValues[vectorLayerSetting.selectedTypeKey];

                foreach (VectorFeature feature in vectorLayerSetting.features)
                {
                    ConfigVectorLayer.LayerData configLayerData = new ConfigVectorLayer.LayerData
                    {
                        points = new double[feature.coordinates.Length, 2]
                    };

                    for (int coordinateIndex = 0; coordinateIndex < feature.coordinates.Length; coordinateIndex++)
                    {
                        configLayerData.points[coordinateIndex, 0] = feature.coordinates[coordinateIndex][0];
                        configLayerData.points[coordinateIndex, 1] = feature.coordinates[coordinateIndex][1];
                    }

                    configLayerData.gaps = new double[0, 0]; // TODO handle gaps

                    feature.attributes.TryGetValue(vectorLayerSetting.selectedTypeKey, out object? value);
                    configLayerData.typeIndices.Add(typeValues.IndexOf(value));

                    foreach (string metaKey in vectorLayerSetting.attributeKeyToValues.Keys)
                    {
                        feature.attributes.TryGetValue(metaKey, out object? metaValue);
                        configLayerData.metaIndices.Add(metaKey, metaValue?.ToString() ?? "");
                    }

                    configVectorLayer.layerData.Add(configLayerData);
                }
            }

            foreach (RasterLayerSetting rasterLayerSetting in a_settingsDataModel.rasterLayerSettings)
            {
                ConfigRasterLayer configRasterLayer = new ConfigRasterLayer();
                configRasterLayer.name = rasterLayerSetting.name;
                configRasterLayer.@short = rasterLayerSetting.name;

                configRasterLayer.rasterScale = rasterLayerSetting.rasterScale;
                configRasterLayer.rasterLayerTypes = rasterLayerSetting.rasterLayerTypes;
                configRasterLayer.rasterMappings = rasterLayerSetting.rasterMappings;
                configRasterLayer.tags = rasterLayerSetting.tags;
                configRasterLayer.originalRasterFilePath = rasterLayerSetting.rasterFilePath;
                configRasterLayer.coordinate0[0] = rasterLayerSetting.extentsMin.x;
                configRasterLayer.coordinate0[1] = rasterLayerSetting.extentsMin.y;
                configRasterLayer.coordinate1[0] = rasterLayerSetting.extentsMax.x;
                configRasterLayer.coordinate1[1] = rasterLayerSetting.extentsMax.y;

                jsonConfigObject.dataModel.rasterLayers.Add(configRasterLayer);
            }

            // Create basemap raster layer if basemap generation is selected
            if (a_settingsDataModel.generateBasemap)
            {
                ConfigRasterLayer configRasterLayer = new ConfigRasterLayer();
                configRasterLayer.name = "Basemap_Generated";
                configRasterLayer.@short = "Basemap";

                configRasterLayer.rasterScale = new RasterScale();
                configRasterLayer.rasterLayerTypes = new List<LayerTypeData> { };
                configRasterLayer.rasterMappings = new List<RasterMapping> { };
                configRasterLayer.tags = new List<string> { "Raster" };
                configRasterLayer.originalRasterFilePath = "terrestris";
                configRasterLayer.coordinate0[0] = a_settingsDataModel.coordinate0.x;
                configRasterLayer.coordinate0[1] = a_settingsDataModel.coordinate0.y;
                configRasterLayer.coordinate1[0] = a_settingsDataModel.coordinate1.x;
                configRasterLayer.coordinate1[1] = a_settingsDataModel.coordinate1.y;

                jsonConfigObject.dataModel.rasterLayers.Add(configRasterLayer);
            }

            return jsonConfigObject;
        }

        public static void SaveConfigObjectToFile(JsonConfigObject a_configObject, string a_directoryPath)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.Formatting = Formatting.Indented;

            string configRootDirectoryPath = a_directoryPath + "/config/";
            string rasterDirectoryPath = configRootDirectoryPath + "/Rastermaps/";

            if (!Directory.Exists(configRootDirectoryPath))
            {
                Directory.CreateDirectory(configRootDirectoryPath);
                Directory.CreateDirectory(rasterDirectoryPath);
            }

            foreach (ConfigRasterLayer rasterLayerSetting in a_configObject.dataModel.rasterLayers)
            {
                if (rasterLayerSetting.originalRasterFilePath == "terrestris")
                {
                    // TODO: add a proper ping check to the terrestris server
                    bool ping = true;
                    if (!ping)
                    {
                        throw new Exception("Cannot reach terrestris server to generate basemap!");
                    }

                    string bbox = (((int)a_configObject.dataModel.coordinate0[0]) + ","
                       + (int)a_configObject.dataModel.coordinate0[1] + ","
                       + (int)a_configObject.dataModel.coordinate1[0] + ","
                       + (int)a_configObject.dataModel.coordinate1[1]);

                    rasterLayerSetting.rasterFilePath = rasterDirectoryPath + $"raster{rasterLayerSetting.name}.png";

                    DownloadTerrestrisImage(rasterLayerSetting.rasterFilePath, bbox).Wait();
                }
                else
                {
                    using (Image image = Image.Load(rasterLayerSetting.originalRasterFilePath))
                    {
                        double regionBottomLeftX = a_configObject.dataModel.coordinate0[0];
                        double regionBottomLeftY = a_configObject.dataModel.coordinate0[1];
                        double regionTopRightX = a_configObject.dataModel.coordinate1[0];
                        double regionTopRightY = a_configObject.dataModel.coordinate1[1];

                        double rasterInputBottomLeftX = rasterLayerSetting.coordinate0[0];
                        double rasterInputBottomLeftY = rasterLayerSetting.coordinate0[1];
                        double rasterInputTopRightX = rasterLayerSetting.coordinate1[0];
                        double rasterInputTopRightY = rasterLayerSetting.coordinate1[1];

                        int originalWidth = image.Width;
                        int originalHeight = image.Height;

                        double coordinateToPixelWidthFactor = originalWidth / (rasterInputTopRightX - rasterInputBottomLeftX);
                        double coordinateToPixelHeightFactor = originalHeight / (rasterInputTopRightY - rasterInputBottomLeftY);

                        double outputPixel0XRealNumber = (regionBottomLeftX - rasterInputBottomLeftX) * coordinateToPixelWidthFactor;
                        double outputPixel0YRealNumber = (regionBottomLeftY - rasterInputBottomLeftY) * coordinateToPixelHeightFactor;
                        double outputPixel1XRealNumber = (regionTopRightX - rasterInputBottomLeftX) * coordinateToPixelWidthFactor;
                        double outputPixel1YRealNumber = (regionTopRightY - rasterInputBottomLeftY) * coordinateToPixelHeightFactor;

                        int outputPixel0X = (int)outputPixel0XRealNumber;
                        int outputPixel0Y = (int)outputPixel0YRealNumber;
                        int outputPixel1X = (int)Math.Ceiling(outputPixel1XRealNumber);
                        int outputPixel1Y = (int)Math.Ceiling(outputPixel1YRealNumber);

                        int regionWidth = outputPixel1X - outputPixel0X;
                        int regionHeight = outputPixel1Y - outputPixel0Y;

                        if (regionWidth <= 0 || regionHeight <= 0)
                        {
                            throw new Exception("Invalid region size!");
                        }

                        regionWidth = Math.Clamp(regionWidth, 0, originalWidth);
                        regionHeight = Math.Clamp(regionHeight, 0, originalHeight);

                        double pixelToCoordinateWidthFactor = (rasterInputTopRightX - rasterInputBottomLeftX) / originalWidth;
                        double pixelToCoordinateHeightFactor = (rasterInputTopRightY - rasterInputBottomLeftY) / originalHeight;

                        rasterLayerSetting.coordinate0[0] = regionBottomLeftX - (outputPixel0XRealNumber - outputPixel0X) * pixelToCoordinateWidthFactor;
                        rasterLayerSetting.coordinate0[1] = regionBottomLeftY - (outputPixel0YRealNumber - outputPixel0Y) * pixelToCoordinateHeightFactor;
                        rasterLayerSetting.coordinate1[0] = regionTopRightX - (outputPixel1X - outputPixel1XRealNumber) * pixelToCoordinateWidthFactor;
                        rasterLayerSetting.coordinate1[1] = regionTopRightY - (outputPixel1Y - outputPixel1YRealNumber) * pixelToCoordinateHeightFactor;

                        // convert to pixel coordinates
                        outputPixel0Y = originalHeight - regionHeight - outputPixel0Y;

                        outputPixel0X = Math.Max(0, Math.Min(originalWidth, outputPixel0X));
                        outputPixel0Y = Math.Max(0, Math.Min(originalHeight, outputPixel0Y));

                        Rectangle cutRect = new Rectangle(outputPixel0X, outputPixel0Y, regionWidth, regionHeight);
                        image.Mutate(a_context => a_context.Crop(cutRect));

                        rasterLayerSetting.rasterFilePath = rasterDirectoryPath + $"raster{rasterLayerSetting.name}.png";
                        image.SaveAsPng(rasterLayerSetting.rasterFilePath);
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(configRootDirectoryPath + "config.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, a_configObject);
            }

            string zipPath = a_directoryPath + "/config.zip";
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(configRootDirectoryPath, zipPath);
        }
    }
}

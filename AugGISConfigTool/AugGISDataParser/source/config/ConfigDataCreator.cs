using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using DotSpatial.Data;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

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
                    configVectorLayer.layerTypes.Add(new LayerType() {name = type});
                }

                foreach (IFeature shapeFeature in vectorLayerSetting.shapefile.Features)
                {
                    ConfigVectorLayer.LayerData configLayerData = new ConfigVectorLayer.LayerData
                    {
                        points = new double[shapeFeature.Geometry.Coordinates.Length, 2]
                    };

                    for (int coordinateIndex = 0;
                         coordinateIndex < shapeFeature.Geometry.Coordinates.Length;
                         coordinateIndex++)
                    {
                        Coordinate coordinate = shapeFeature.Geometry.Coordinates[coordinateIndex];
                        configLayerData.points[coordinateIndex, 0] = coordinate.X;
                        configLayerData.points[coordinateIndex, 1] = coordinate.X;
                    }

                    configLayerData.gaps = new double[0, 0]; //TODO handle gaps

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
                configRasterLayer.@short = rasterLayerSetting.name; //TODO handle @short name (maybe make it a setting)

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
                using (Image image = Image.Load(rasterLayerSetting.originalRasterFilePath))
                {
                    int originalWidth = image.Width;
                    int originalHeight = image.Height;

                    double coordinateToPixelWidthFactor = originalWidth /
                                                          (rasterLayerSetting.coordinate1[0] -
                                                           rasterLayerSetting.coordinate0[0]);

                    double coordinateToPixelHeightFactor = originalHeight /
                                                           (rasterLayerSetting.coordinate1[1] -
                                                            rasterLayerSetting.coordinate0[1]);

                    double outputPixel0XRealNumber =
                        (a_configObject.dataModel.coordinate0[0] - rasterLayerSetting.coordinate0[0]) *
                        coordinateToPixelWidthFactor;
                    double outputPixel0YRealNumber =
                        (a_configObject.dataModel.coordinate0[1] - rasterLayerSetting.coordinate0[1]) *
                        coordinateToPixelHeightFactor;

                    double outputPixel1XRealNumber =
                        (a_configObject.dataModel.coordinate1[0] - rasterLayerSetting.coordinate0[0]) *
                        coordinateToPixelWidthFactor;
                    double outputPixel1YRealNumber =
                        (a_configObject.dataModel.coordinate1[1] - rasterLayerSetting.coordinate0[1]) *
                        coordinateToPixelHeightFactor;

                    int outputPixel0X = (int) outputPixel0XRealNumber;
                    int outputPixel0Y = (int) outputPixel0YRealNumber;

                    int outputPixel1X = (int) Math.Ceiling(outputPixel1XRealNumber);
                    int outputPixel1Y = (int) Math.Ceiling(outputPixel1YRealNumber);

                    int regionWidth = outputPixel1X - outputPixel0X - 1;
                    int regionHeight = outputPixel1Y - outputPixel0Y - 1;

                    if (regionWidth <= 0 || regionHeight <= 0)
                    {
                        throw new Exception("Invalid region size!");
                    }

                    image.Mutate(a_context => a_context.Crop(regionWidth, regionHeight));

                    rasterLayerSetting.rasterFilePath = rasterDirectoryPath + $"raster{rasterLayerSetting.name}.png";
                    image.SaveAsPng(rasterLayerSetting.rasterFilePath);
                }
            }

            using (StreamWriter sw = new StreamWriter(configRootDirectoryPath + "config.json"))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, a_configObject);
                }
            }
        }
    }
}
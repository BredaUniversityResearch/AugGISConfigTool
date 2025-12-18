using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using DotSpatial.Data;
using DotSpatial.Projections;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Rectangle = SixLabors.ImageSharp.Rectangle;

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
                if (vectorLayerSetting.featureSet == null)
                {
                    Console.WriteLine("Error: Vector Layer Settings shape file is Null!");
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

                foreach (IFeature shapeFeature in vectorLayerSetting.featureSet.Features)
                {
                    ConfigVectorLayer.LayerData configLayerData = new ConfigVectorLayer.LayerData
                    {
                        points = new double[shapeFeature.Geometry.Coordinates.Length, 2]
                    };

                    for (int coordinateIndex = 0;
                         coordinateIndex < shapeFeature.Geometry.Coordinates.Length;
                         coordinateIndex++)
                    {
                        var coordinate = shapeFeature.Geometry.Coordinates[coordinateIndex];
                        configLayerData.points[coordinateIndex, 0] = coordinate.X;
                        configLayerData.points[coordinateIndex, 1] = coordinate.Y;
                    }

                    configLayerData.gaps = new double[0, 0]; //TODO handle gaps

                    List<object?> attributes = vectorLayerSetting.attributeKeyToValues[vectorLayerSetting.selectedTypeKey];

                    int attributeIndex = shapeFeature.DataRow.Table.Columns.IndexOf(vectorLayerSetting.selectedTypeKey);
                    object? value = shapeFeature.DataRow[attributeIndex];

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

                    int outputPixel0X = (int) outputPixel0XRealNumber;
                    int outputPixel0Y = (int) outputPixel0YRealNumber;

                    int outputPixel1X = (int) Math.Ceiling(outputPixel1XRealNumber);
                    int outputPixel1Y = (int) Math.Ceiling(outputPixel1YRealNumber);

                    int regionWidth = outputPixel1X - outputPixel0X;
                    int regionHeight = outputPixel1Y - outputPixel0Y;

                    if (regionWidth <= 0 || regionHeight <= 0)
                    {
                        throw new Exception("Invalid region size!");
                    }
                    
                    regionWidth = Math.Clamp(regionWidth, 0, originalWidth);
                    regionHeight = Math.Clamp(regionHeight,0, originalHeight);
                    
                    double pixelToCoordinateWidthFactor = (rasterInputTopRightX - rasterInputBottomLeftX) / originalWidth;
                    double pixelToCoordinateHeightFactor = (rasterInputTopRightY - rasterInputBottomLeftY) / originalHeight;

                    rasterLayerSetting.coordinate0[0] = regionBottomLeftX -
                                                        (outputPixel0XRealNumber - outputPixel0X) *
                                                        pixelToCoordinateWidthFactor;
                    rasterLayerSetting.coordinate0[1] = regionBottomLeftY -
                                                        (outputPixel0YRealNumber - outputPixel0Y) *
                                                        pixelToCoordinateHeightFactor;
                    
                    rasterLayerSetting.coordinate1[0] = regionTopRightX -
                                                        (outputPixel1X - outputPixel1XRealNumber) *
                                                        pixelToCoordinateWidthFactor;
                    
                    rasterLayerSetting.coordinate1[1] = regionTopRightY -
                                                        (outputPixel1Y - outputPixel1YRealNumber) *
                                                        pixelToCoordinateHeightFactor;
                    
                    // finally convert to pixel coordinates.
                    outputPixel0Y = originalHeight - regionHeight - outputPixel0Y;

                    // clamp by image input size
                    outputPixel0X = Math.Max(0, Math.Min(originalWidth, outputPixel0X));
                    outputPixel0Y =  Math.Max(0,  Math.Min(originalHeight, outputPixel0Y));

                    Rectangle cutRect = new Rectangle(outputPixel0X, outputPixel0Y, regionWidth, regionHeight);
                    image.Mutate(a_context => a_context.Crop(cutRect));

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

            string zipPath = a_directoryPath + "/config.zip";

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(configRootDirectoryPath,zipPath);
        }
    }
}
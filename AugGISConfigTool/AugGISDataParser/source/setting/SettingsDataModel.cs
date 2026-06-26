using System.Linq;

namespace AugGISDataParser
{
    public class SettingsDataModel
    {
        public string region = "";

        public string projection =
            "+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs";

        public Vector2 coordinate0;
        public Vector2 coordinate1;

        public List<VectorLayerSetting> vectorLayerSettings = new List<VectorLayerSetting>();
        public List<RasterLayerSetting> rasterLayerSettings = new List<RasterLayerSetting>();

        public bool generateBasemap;

        public void OnAfterLoad()
        {
            foreach (VectorLayerSetting vectorLayerSetting in vectorLayerSettings)
            {
                if (vectorLayerSetting.features == null)
                {
                    if (vectorLayerSetting.shapeFilePath.EndsWith(".shp"))
                    {
                        vectorLayerSetting.features =
                            SettingsDataCreator.ParseShapeFile(vectorLayerSetting.shapeFilePath).features;
                    }
                    else if (vectorLayerSetting.shapeFilePath.EndsWith(".json") ||
                             vectorLayerSetting.shapeFilePath.EndsWith(".geojson") ||
                             vectorLayerSetting.shapeFilePath.EndsWith(".gpx"))
                    {
                        GeoJsonFeatureSets sets = vectorLayerSetting.shapeFilePath.EndsWith(".gpx")
                            ? SettingsDataCreator.GetFeatureSetFromGpx(vectorLayerSetting.shapeFilePath)
                            : SettingsDataCreator.GetFeatureSetFromGeoJson(vectorLayerSetting.shapeFilePath);

                        if (vectorLayerSetting.tags.Contains("Point"))
                            vectorLayerSetting.features = sets.pointFeatures;
                        else if (vectorLayerSetting.tags.Contains("Line"))
                            vectorLayerSetting.features = sets.lineFeatures;
                        else if (vectorLayerSetting.tags.Contains("Polygon"))
                            vectorLayerSetting.features = sets.polygonFeatures;
                        else
                            throw new Exception("Layer does NOT contain any supported tag");
                    }
                }

                if (vectorLayerSetting.selectedTypeKey == string.Empty &&
                    vectorLayerSetting.attributeKeyToValues.Count > 0)
                {
                    vectorLayerSetting.selectedTypeKey = vectorLayerSetting.attributeKeyToValues.Keys.ElementAt(0);
                }
            }
        }
    }
}

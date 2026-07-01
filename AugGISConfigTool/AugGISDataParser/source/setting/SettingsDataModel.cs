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
                        GeoJsonFeatureSets? sets = vectorLayerSetting.shapeFilePath.EndsWith(".gpx")
                            ? SettingsDataCreator.GetFeatureSetFromGpx(vectorLayerSetting.shapeFilePath)
                            : SettingsDataCreator.GetFeatureSetFromGeoJson(vectorLayerSetting.shapeFilePath);

                        if (sets == null)
                        {
                            Console.WriteLine("[Warning] Could not reload features for layer '{0}' from '{1}'.",
                                vectorLayerSetting.name, vectorLayerSetting.shapeFilePath);
                        }
                        else if (vectorLayerSetting.tags.Contains("Point"))
                            vectorLayerSetting.features = sets.Value.pointFeatures;
                        else if (vectorLayerSetting.tags.Contains("Line"))
                            vectorLayerSetting.features = sets.Value.lineFeatures;
                        else if (vectorLayerSetting.tags.Contains("Polygon"))
                            vectorLayerSetting.features = sets.Value.polygonFeatures;
                        else
                            throw new Exception("Layer does NOT contain any supported tag");
                    }
                }

                if (vectorLayerSetting.selectedTypeKey == string.Empty &&
                    vectorLayerSetting.attributeKeyToValues.Count > 0)
                {
                    vectorLayerSetting.selectedTypeKey = vectorLayerSetting.attributeKeyToValues.Keys.ElementAt(0);
                }

                // Freshly parsed layers have a selected key but no type list yet — generate it so types
                // show immediately. Saved settings already carry layerTypeData, so the guard skips them.
                if (vectorLayerSetting.layerTypeData.Count == 0 && vectorLayerSetting.selectedTypeKey != string.Empty)
                {
                    vectorLayerSetting.SelectTypeAttribute(vectorLayerSetting.selectedTypeKey);
                }
            }
        }
    }
}

using Newtonsoft.Json;

namespace AugGISDataParser
{
    public class VectorLayerSetting
    {
        public string name = string.Empty;
        public string selectedTypeKey = string.Empty;

        public List<string> tags = new List<string>();

        public string shapeFilePath = string.Empty;

        // Was: [JsonIgnore] public FeatureSet? featureSet  (DotSpatial, Windows-only)
        [JsonIgnore] public List<VectorFeature>? features = null;

        public Dictionary<string, List<object?>> attributeKeyToValues = new Dictionary<string, List<object?>>();

        public List<LayerTypeData> layerTypeData = new List<LayerTypeData>();

        [JsonIgnore] public Vector2 extentsMin;
        [JsonIgnore] public Vector2 extentsMax;
    }
}

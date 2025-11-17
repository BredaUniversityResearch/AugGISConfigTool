using Newtonsoft.Json;

namespace AugGISDataParser;

public class RasterScale
{
    public enum EInterpolation
    {
        Lin = 0,
        Quad,
        LinGrouped,
        Count
    }
    
    public class InterpolationGroup
    {
        [JsonProperty("normalised_input_value")]
        public double normalisedInputValue;
        [JsonProperty("min_output_value")]
        public int minOutputValue;
    }
    
    [JsonProperty("min_value")]
    public int minValue;
    [JsonProperty("max_value")]
    public int maxValue;
    
    public EInterpolation interpolation;

    [JsonProperty("groups")]
    public List<InterpolationGroup> interpolationGroups = new List<InterpolationGroup>();
}
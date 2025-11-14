using Newtonsoft.Json;

namespace AugGISDataParser;

public class RasterMapping
{
    public int min;
    public int max;
    [JsonProperty("type")]
    public int typeIndex;
}
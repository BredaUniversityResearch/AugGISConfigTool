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
    
    public struct InterpolationGroup
    {
        public double normalisedInputValue;
        public int minOutputValue;
    }
    
    public int minValue;
    public int maxValue;
    
    public EInterpolation interpolation;

    public List<InterpolationGroup> interpolationGroups = new List<InterpolationGroup>();
}
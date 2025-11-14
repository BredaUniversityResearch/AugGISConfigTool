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
    
    public int minValue;
    public int maxValue;
    
    public EInterpolation interpolation;
}
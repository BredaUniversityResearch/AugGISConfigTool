namespace AugGISDataParser;

public class LayerTypeData
{
    public string name = string.Empty;
    public int value = 0;
    
    public bool displayPolygon = false;
    public string polygonColor = "#FFFFFF";
    public string polygonPatternName = "0";
    public bool innerGlowEnabled = false;
    public int innerGlowRadius = 0;
    public int innerGlowIterations = 0;
    public int innerGlowMultiplier = 0;
    public int innerGlowPixelSize = 0;
    
    public bool displayLines = false;
    public string lineColor = "#000000";
    public int lineWidth = 1;
    public string lineIcon = "";
    public string linePatternType = "Solid";

    public bool displayPoints = false;
    public string pointColor = "#000000";
    public int pointSize = 0;
    public string pointSpriteName = "None";

    public string description = "";
}
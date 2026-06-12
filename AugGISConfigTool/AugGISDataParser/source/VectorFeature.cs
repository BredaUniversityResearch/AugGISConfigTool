namespace AugGISDataParser;

/// <summary>
/// A single vector feature after parsing and reprojection to EPSG:3035.
/// This replaces DotSpatial's IFeature / DataRow with a small, library-agnostic
/// record so the rest of the pipeline (export, GUI) doesn't depend on any GIS library.
/// </summary>
public sealed class VectorFeature
{
    /// <summary>Geometry vertices in EPSG:3035, each entry is [x, y].</summary>
    public double[][] coordinates = System.Array.Empty<double[]>();

    /// <summary>Attribute values keyed by column name (strings, numbers, etc.).</summary>
    public Dictionary<string, object?> attributes = new();
}

namespace AugGISDataParser;

/// <summary>
/// GeoJSON geometries split by type, after reprojection to EPSG:3035.
/// (Was DotSpatial FeatureSets; now library-agnostic VectorFeature lists.)
/// </summary>
public struct GeoJsonFeatureSets
{
    public List<VectorFeature>? pointFeatures;
    public List<VectorFeature>? lineFeatures;
    public List<VectorFeature>? polygonFeatures;

    public GeoJsonFeatureSets(List<VectorFeature> a_points, List<VectorFeature> a_lines, List<VectorFeature> a_polygons)
    {
        pointFeatures = a_points;
        lineFeatures = a_lines;
        polygonFeatures = a_polygons;
    }
}

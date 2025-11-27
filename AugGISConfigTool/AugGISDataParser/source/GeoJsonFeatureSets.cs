using DotSpatial.Data;

namespace AugGISDataParser;

public struct GeoJsonFeatureSets
{
	public FeatureSet? pointFeatureSet = null;
	public FeatureSet? lineFeatureSet = null;
	public FeatureSet? polygonFeatureSet = null;

	public GeoJsonFeatureSets(FeatureSet a_pointSet, FeatureSet a_lineSet, FeatureSet a_polygonSet)
	{
		pointFeatureSet = a_pointSet;
		lineFeatureSet = a_lineSet;
		polygonFeatureSet = a_polygonSet;
	}
}
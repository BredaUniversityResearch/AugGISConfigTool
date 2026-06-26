using System.Linq;
using System.Xml;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;            // GeoJsonReader
using NetTopologySuite.IO.Esri;       // Shapefile.ReadAllFeatures
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace AugGISDataParser
{
    public static class SettingsDataCreator
    {
        private const int Wgs84Epsg = 4326;   // GeoJSON default CRS (lon/lat)
        private const int Etrs89Epsg = 3035;  // target CRS: ETRS89-LAEA

        private static readonly GeoJsonReader JsonReader = new GeoJsonReader();

        // ---------------------------------------------------------------------
        // Entry point: parse a folder of GIS files into a settings model.
        // ---------------------------------------------------------------------
        public static SettingsDataModel CreateSettingsDataModelFromGisData(string a_gisDataDirectoryPath)
        {
            GdalSetup.EnsureConfigured();

            SettingsDataModel settingsDataModel = new SettingsDataModel();

            string[] files = Directory.GetFiles(a_gisDataDirectoryPath);
            List<string> shpFiles = new();
            List<string> geoJsonFiles = new();
            List<string> gpxFiles = new();
            List<string> rasterTifFiles = new();

            foreach (string file in files)
            {
                if (file.EndsWith(".shp")) shpFiles.Add(file);
                else if (file.EndsWith(".json") || file.EndsWith(".geojson")) geoJsonFiles.Add(file);
                else if (file.EndsWith(".gpx")) gpxFiles.Add(file);
                else if (file.EndsWith(".tiff") || file.EndsWith(".tif")) rasterTifFiles.Add(file);
                else Console.WriteLine("[Warning] Unsupported GIS file detected: {0}", file);
            }

            Vector2 coordinateMin = new Vector2(double.MaxValue, double.MaxValue);
            Vector2 coordinateMax = new Vector2(double.MinValue, double.MinValue);

            // ---- shapefiles ----
            foreach (string shpFilePath in shpFiles)
            {
                VectorLayerSetting setting = ParseShapeFile(shpFilePath);
                setting.shapeFilePath = shpFilePath;
                settingsDataModel.vectorLayerSettings.Add(setting);
            }

            // ---- geojson ----
            foreach (string geoJsonFile in geoJsonFiles)
                AddFeatureSetLayers(settingsDataModel, GetFeatureSetFromGeoJson(geoJsonFile),
                    Path.GetFileName(geoJsonFile), geoJsonFile);

            // ---- gpx (waypoints -> points, routes & tracks -> lines) ----
            foreach (string gpxFile in gpxFiles)
                AddFeatureSetLayers(settingsDataModel, GetFeatureSetFromGpx(gpxFile),
                    Path.GetFileName(gpxFile), gpxFile);

            // ---- rasters ----
            foreach (string rasterFile in rasterTifFiles)
            {
                RasterLayerSetting r = ParseGeoTifFile(rasterFile);
                if (r.extentsMin.x < coordinateMin.x) coordinateMin.x = r.extentsMin.x;
                if (r.extentsMin.y < coordinateMin.y) coordinateMin.y = r.extentsMin.y;
                if (r.extentsMax.x > coordinateMax.x) coordinateMax.x = r.extentsMax.x;
                if (r.extentsMax.y > coordinateMax.y) coordinateMax.y = r.extentsMax.y;
                settingsDataModel.rasterLayerSettings.Add(r);
            }

            // ---- fold vector extents into the global extent ----
            foreach (VectorLayerSetting v in settingsDataModel.vectorLayerSettings)
            {
                if (v.extentsMin.x < coordinateMin.x) coordinateMin.x = v.extentsMin.x;
                if (v.extentsMin.y < coordinateMin.y) coordinateMin.y = v.extentsMin.y;
                if (v.extentsMax.x > coordinateMax.x) coordinateMax.x = v.extentsMax.x;
                if (v.extentsMax.y > coordinateMax.y) coordinateMax.y = v.extentsMax.y;
            }

            settingsDataModel.coordinate0 = coordinateMin;
            settingsDataModel.coordinate1 = coordinateMax;
            settingsDataModel.generateBasemap = false;

            settingsDataModel.OnAfterLoad();
            return settingsDataModel;
        }

        // ---------------------------------------------------------------------
        // Shapefile -> VectorLayerSetting (reprojected to EPSG:3035)
        // ---------------------------------------------------------------------
        public static VectorLayerSetting ParseShapeFile(string a_shpFilePath)
        {
            GdalSetup.EnsureConfigured();

            // Source CRS from the sibling .prj (WKT). If absent, assume data is already ETRS89-LAEA.
            CoordinateTransformation? transform = BuildTransformFromPrj(a_shpFilePath);

            List<Feature> ntsFeatures = Shapefile.ReadAllFeatures(a_shpFilePath).ToList();
            string tag = ntsFeatures.Count > 0 ? GeometryTag(ntsFeatures[0].Geometry) : string.Empty;

            List<VectorFeature> features = new(ntsFeatures.Count);
            foreach (IFeature f in ntsFeatures)
                features.Add(ConvertFeature(f, transform, false));

            VectorLayerSetting setting = BuildVectorLayer(features, tag);
            setting.name = Path.GetFileNameWithoutExtension(a_shpFilePath);
            return setting;
        }

        // ---------------------------------------------------------------------
        // GeoJSON -> point/line/polygon VectorFeature lists (4326 -> 3035)
        // ---------------------------------------------------------------------
        public static GeoJsonFeatureSets GetFeatureSetFromGeoJson(string a_geoJsonPath)
        {
            GdalSetup.EnsureConfigured();
            FeatureCollection featureCollection = JsonReader.Read<FeatureCollection>(File.ReadAllText(a_geoJsonPath));
            return BuildFeatureSets(featureCollection);
        }

        // ---------------------------------------------------------------------
        // GPX -> point/line VectorFeature lists (GPX is always WGS84, 4326 -> 3035).
        // Waypoints become points; routes and tracks become lines.
        // ---------------------------------------------------------------------
        public static GeoJsonFeatureSets GetFeatureSetFromGpx(string a_gpxPath)
        {
            GdalSetup.EnsureConfigured();
            return BuildFeatureSets(ReadGpxAsFeatures(a_gpxPath));
        }

        private static List<IFeature> ReadGpxAsFeatures(string a_gpxPath)
        {
            GeometryFactory factory = new GeometryFactory(new PrecisionModel(), Wgs84Epsg);
            List<IFeature> features = new();

            GpxFile gpx;
            using (XmlReader xml = XmlReader.Create(a_gpxPath))
                gpx = GpxFile.ReadFrom(xml, new GpxReaderSettings());

            foreach (GpxWaypoint waypoint in gpx.Waypoints)
                features.Add(WaypointToPoint(factory, waypoint));

            foreach (GpxRoute route in gpx.Routes)
            {
                IFeature? line = WaypointsToLine(factory, route.Waypoints, route.Name);
                if (line != null) features.Add(line);
            }

            foreach (GpxTrack track in gpx.Tracks)
            {
                List<GpxWaypoint> trackPoints = new();
                foreach (GpxTrackSegment segment in track.Segments)
                    trackPoints.AddRange(segment.Waypoints);

                IFeature? line = WaypointsToLine(factory, trackPoints, track.Name);
                if (line != null) features.Add(line);
            }

            return features;
        }

        private static IFeature WaypointToPoint(GeometryFactory a_factory, GpxWaypoint a_waypoint)
        {
            Point geometry = a_factory.CreatePoint(
                new Coordinate((double)a_waypoint.Longitude, (double)a_waypoint.Latitude));

            AttributesTable attributes = new();
            if (a_waypoint.Name != null) attributes.Add("name", a_waypoint.Name);
            if (a_waypoint.Description != null) attributes.Add("description", a_waypoint.Description);
            if (a_waypoint.Comment != null) attributes.Add("comment", a_waypoint.Comment);
            if (a_waypoint.SymbolText != null) attributes.Add("symbol", a_waypoint.SymbolText);
            if (a_waypoint.ElevationInMeters != null) attributes.Add("ele", a_waypoint.ElevationInMeters);

            return new Feature(geometry, attributes);
        }

        private static IFeature? WaypointsToLine(GeometryFactory a_factory,
            IEnumerable<GpxWaypoint> a_waypoints, string? a_name)
        {
            Coordinate[] coordinates = a_waypoints
                .Select(w => new Coordinate((double)w.Longitude, (double)w.Latitude))
                .ToArray();

            if (coordinates.Length < 2) return null; // a line needs at least two points

            LineString geometry = a_factory.CreateLineString(coordinates);
            AttributesTable attributes = new();
            if (a_name != null) attributes.Add("name", a_name);

            return new Feature(geometry, attributes);
        }

        // ---------------------------------------------------------------------
        // Shared by GeoJSON and GPX: split WGS84 NTS features by geometry type,
        // reproject to EPSG:3035, and stringify attributes (matches the original
        // tool's behaviour for these JSON/GPX sources).
        // ---------------------------------------------------------------------
        private static GeoJsonFeatureSets BuildFeatureSets(IEnumerable<IFeature> a_features)
        {
            CoordinateTransformation transform = BuildTransform(Wgs84Epsg);

            List<VectorFeature> points = new();
            List<VectorFeature> lines = new();
            List<VectorFeature> polygons = new();

            foreach (IFeature f in a_features)
            {
                string tag = GeometryTag(f.Geometry);
                VectorFeature vf = ConvertFeature(f, transform, true);
                switch (tag)
                {
                    case "Point": points.Add(vf); break;
                    case "Line": lines.Add(vf); break;
                    case "Polygon": polygons.Add(vf); break;
                    default: throw new Exception("Unknown geometry type: " + f.Geometry.GeometryType);
                }
            }

            return new GeoJsonFeatureSets(points, lines, polygons);
        }

        private static void AddFeatureSetLayers(SettingsDataModel a_model, GeoJsonFeatureSets a_sets,
            string a_baseName, string a_sourcePath)
        {
            if (a_sets.pointFeatures is { Count: > 0 } pts)
                a_model.vectorLayerSettings.Add(MakeLayer(pts, "Point", a_baseName + "_point", a_sourcePath));
            if (a_sets.lineFeatures is { Count: > 0 } lines)
                a_model.vectorLayerSettings.Add(MakeLayer(lines, "Line", a_baseName + "_line", a_sourcePath));
            if (a_sets.polygonFeatures is { Count: > 0 } polys)
                a_model.vectorLayerSettings.Add(MakeLayer(polys, "Polygon", a_baseName + "_polygon", a_sourcePath));
        }

        private static VectorLayerSetting MakeLayer(List<VectorFeature> a_features, string a_tag,
            string a_name, string a_sourcePath)
        {
            VectorLayerSetting layer = BuildVectorLayer(a_features, a_tag);
            layer.name = a_name;
            layer.shapeFilePath = a_sourcePath;
            return layer;
        }

        // ---------------------------------------------------------------------
        // GeoTIFF -> RasterLayerSetting (extent reprojected to EPSG:3035)
        // ---------------------------------------------------------------------
        private static RasterLayerSetting ParseGeoTifFile(string a_rasterFilePath)
        {
            GdalSetup.EnsureConfigured();

            RasterLayerSetting raster = new RasterLayerSetting { rasterFilePath = a_rasterFilePath };

            using Dataset ds = Gdal.Open(a_rasterFilePath, Access.GA_ReadOnly);

            double[] gt = new double[6];
            ds.GetGeoTransform(gt);
            int w = ds.RasterXSize;
            int h = ds.RasterYSize;

            // affine transform corners (top-left origin)
            double cx0 = gt[0];
            double cy0 = gt[3];
            double cx1 = gt[0] + w * gt[1] + h * gt[2];
            double cy1 = gt[3] + w * gt[4] + h * gt[5];

            double minX = Math.Min(cx0, cx1), maxX = Math.Max(cx0, cx1);
            double minY = Math.Min(cy0, cy1), maxY = Math.Max(cy0, cy1);

            CoordinateTransformation? transform = BuildTransformFromWkt(ds.GetProjection());
            (minX, minY) = TransformXy(transform, minX, minY);
            (maxX, maxY) = TransformXy(transform, maxX, maxY);

            raster.name = Path.GetFileNameWithoutExtension(a_rasterFilePath);
            raster.extentsMin = new Vector2(Math.Min(minX, maxX), Math.Min(minY, maxY));
            raster.extentsMax = new Vector2(Math.Max(minX, maxX), Math.Max(minY, maxY));
            raster.tags.Add("Raster");
            return raster;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------
        private static VectorLayerSetting BuildVectorLayer(List<VectorFeature> a_features, string a_tag)
        {
            VectorLayerSetting setting = new VectorLayerSetting { features = a_features };
            if (!string.IsNullOrEmpty(a_tag)) setting.tags.Add(a_tag);

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (VectorFeature feature in a_features)
            {
                foreach (double[] c in feature.coordinates)
                {
                    if (c[0] < minX) minX = c[0];
                    if (c[1] < minY) minY = c[1];
                    if (c[0] > maxX) maxX = c[0];
                    if (c[1] > maxY) maxY = c[1];
                }

                foreach (KeyValuePair<string, object?> kv in feature.attributes)
                {
                    if (kv.Value == null) continue; // skip nulls, as the original skipped DBNull
                    if (!setting.attributeKeyToValues.TryGetValue(kv.Key, out List<object?>? values))
                    {
                        values = new List<object?>();
                        setting.attributeKeyToValues[kv.Key] = values;
                    }
                    if (!values.Contains(kv.Value)) values.Add(kv.Value);
                }
            }

            setting.extentsMin = new Vector2(minX, minY);
            setting.extentsMax = new Vector2(maxX, maxY);
            return setting;
        }

        private static VectorFeature ConvertFeature(IFeature a_feature, CoordinateTransformation? a_transform, bool a_stringifyAttributes)
        {
            Coordinate[] src = a_feature.Geometry.Coordinates;
            double[][] coords = new double[src.Length][];
            for (int i = 0; i < src.Length; i++)
            {
                (double x, double y) = TransformXy(a_transform, src[i].X, src[i].Y);
                coords[i] = new[] { x, y };
            }

            Dictionary<string, object?> attrs = new();
            IAttributesTable? table = a_feature.Attributes;
            if (table != null)
            {
                foreach (string name in table.GetNames())
                {
                    object? raw = table[name];
                    // The original tool stored GeoJSON attributes in string-typed columns, so a JSON
                    // bool/number became "True"/"123". We mirror that for GeoJSON to keep settings.json
                    // identical. Shapefiles keep their native .dbf types, exactly as before.
                    attrs[name] = a_stringifyAttributes ? raw?.ToString() : raw;
                }
            }

            return new VectorFeature { coordinates = coords, attributes = attrs };
        }

        private static string GeometryTag(Geometry g)
        {
            string t = g.GeometryType ?? string.Empty;
            if (t.Contains("Polygon", StringComparison.OrdinalIgnoreCase)) return "Polygon";
            if (t.Contains("Line", StringComparison.OrdinalIgnoreCase)) return "Line";
            if (t.Contains("Point", StringComparison.OrdinalIgnoreCase)) return "Point";
            return t;
        }

        private static (double x, double y) TransformXy(CoordinateTransformation? a_transform, double x, double y)
        {
            if (a_transform == null) return (x, y);
            double[] p = { x, y, 0 };
            a_transform.TransformPoint(p); // transforms in place
            return (p[0], p[1]);
        }

        private static CoordinateTransformation BuildTransform(int a_srcEpsg)
        {
            SpatialReference src = new SpatialReference(string.Empty);
            src.ImportFromEPSG(a_srcEpsg);
            // PROJ 6+/GDAL 3 default to authority axis order (lat/lon for 4326);
            // force x=easting/lon, y=northing/lat so our X/Y math stays correct.
            src.SetAxisMappingStrategy(OSGeo.OSR.AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

            SpatialReference dst = new SpatialReference(string.Empty);
            dst.ImportFromEPSG(Etrs89Epsg);
            dst.SetAxisMappingStrategy(OSGeo.OSR.AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

            return new CoordinateTransformation(src, dst);
        }

        private static CoordinateTransformation? BuildTransformFromPrj(string a_shpFilePath)
        {
            string prjPath = Path.ChangeExtension(a_shpFilePath, ".prj");
            if (!File.Exists(prjPath)) return null; // no .prj -> assume already ETRS89-LAEA
            string wkt = File.ReadAllText(prjPath);
            return BuildTransformFromWkt(wkt);
        }

        private static CoordinateTransformation? BuildTransformFromWkt(string? a_wkt)
        {
            if (string.IsNullOrWhiteSpace(a_wkt)) return null; // unknown source -> assume already 3035

            SpatialReference src = new SpatialReference(string.Empty);
            src.ImportFromWkt(ref a_wkt);
            src.SetAxisMappingStrategy(OSGeo.OSR.AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

            SpatialReference dst = new SpatialReference(string.Empty);
            dst.ImportFromEPSG(Etrs89Epsg);
            dst.SetAxisMappingStrategy(OSGeo.OSR.AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);

            return new CoordinateTransformation(src, dst);
        }

        // ---------------------------------------------------------------------
        // Save / load (unchanged — Newtonsoft only)
        // ---------------------------------------------------------------------
        public static void SaveSettingsDataModelToFile(SettingsDataModel a_settingsDataModel, string a_fileName = @"settings.json")
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

            using StreamWriter sw = new StreamWriter(a_fileName);
            using JsonWriter writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, a_settingsDataModel);
        }

        public static SettingsDataModel? LoadSettingsDataModelFromFile(string a_settingsFilePath = @"settings.json")
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.Formatting = Newtonsoft.Json.Formatting.Indented;

            SettingsDataModel? settingsDataModel;
            using (StreamReader sr = new StreamReader(a_settingsFilePath))
            using (JsonTextReader reader = new JsonTextReader(sr))
            {
                settingsDataModel = serializer.Deserialize<SettingsDataModel>(reader);
            }

            settingsDataModel?.OnAfterLoad();
            return settingsDataModel;
        }
    }
}

using MaxRev.Gdal.Core;

namespace AugGISDataParser;

/// <summary>
/// One-time GDAL/PROJ initialisation.
/// MaxRev.Gdal ships the native GDAL + PROJ runtimes (win-x64, linux-x64, linux-arm64, osx),
/// so there is NO system GDAL install to manage — we just call ConfigureAll() once.
/// Every parser entry point that touches GDAL or OSR calls EnsureConfigured() first.
/// </summary>
public static class GdalSetup
{
    private static readonly object Lock = new();
    private static bool _configured;

    public static void EnsureConfigured()
    {
        if (_configured) return;
        lock (Lock)
        {
            if (_configured) return;
            GdalBase.ConfigureAll(); // registers all drivers + sets PROJ data paths (proj.db)
            _configured = true;
        }
    }
}

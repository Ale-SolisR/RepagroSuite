namespace Rastreo.Api.Services;

/// <summary>
/// Cachea en memoria los assets de marca usados en los PDFs/reportes.
/// El logo se carga desde la BD en el arranque (alta calidad, PNG original);
/// si no estuviera, cae al archivo bundled en Assets/.
/// </summary>
public static class BrandAssets
{
    private static byte[]? _logo;
    private static bool _triedFile;

    /// <summary>Inyecta el logo cargado desde BD en el arranque.</summary>
    public static void SetLogo(byte[]? bytes)
    {
        if (bytes is { Length: > 0 }) _logo = bytes;
    }

    /// <summary>Logo REPAGRO (PNG). Prioriza el cargado desde BD; cae al archivo bundled.</summary>
    public static byte[]? LogoRepagro()
    {
        if (_logo is { Length: > 0 }) return _logo;
        if (!_triedFile)
        {
            _triedFile = true;
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "repagro-logo.png");
                if (File.Exists(path)) _logo = File.ReadAllBytes(path);
            }
            catch { /* sin logo: los PDFs simplemente lo omiten */ }
        }
        return _logo;
    }
}

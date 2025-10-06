using OfficeOpenXml;

namespace OilTrading.Api.Services;

/// <summary>
/// Static configuration for EPPlus licensing
/// </summary>
public static class EPPlusLicenseConfig
{
    private static bool _licenseSet = false;

    /// <summary>
    /// Configure EPPlus license for non-commercial use
    /// This should be called once at application startup
    /// </summary>
    public static void ConfigureLicense()
    {
        if (!_licenseSet)
        {
            try
            {
                // For EPPlus 8+, set license context to non-commercial
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                _licenseSet = true;
            }
            catch (Exception)
            {
                // If setting license fails, continue without Excel functionality
                // This prevents the application from failing to start
            }
        }
    }
}
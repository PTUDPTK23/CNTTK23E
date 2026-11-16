using System;
using QRCoder;

namespace EventFlow.WebClient.Services;

public static class QrCodeHelper
{
    /// <summary>
    /// Generates a standard, scannable SVG QR Code using QRCoder.
    /// Compatible with all smartphone cameras, Google Lens, and 2D barcode scanners.
    /// </summary>
    public static string GenerateSvgQr(string code, int pixelsPerModule = 8)
    {
        if (string.IsNullOrWhiteSpace(code)) code = "EVENTFLOW";

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.M);
            var qrCode = new SvgQRCode(qrCodeData);
            return qrCode.GetGraphic(pixelsPerModule, "#1E293B", "#FFFFFF", drawQuietZones: true);
        }
        catch
        {
            // Fallback safe SVG if any error occurs
            return $"<svg xmlns='http://www.w3.org/2000/svg' width='160' height='160' viewBox='0 0 160 160'><rect width='160' height='160' fill='#FFFFFF' stroke='#CBD5E1'/><text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle' font-family='monospace' font-weight='bold' fill='#2563EB'>{code}</text></svg>";
        }
    }
}

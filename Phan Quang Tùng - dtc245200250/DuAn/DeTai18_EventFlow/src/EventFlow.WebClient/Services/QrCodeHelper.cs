using System;
using System.Text;

namespace EventFlow.WebClient.Services;

public static class QrCodeHelper
{
    /// <summary>
    /// Generates a clean, deterministic SVG QR-style visual code for a registration code.
    /// Perfectly vector-scaled, 100% self-contained without any external CDN.
    /// </summary>
    public static string GenerateSvgQr(string code, int size = 180)
    {
        if (string.IsNullOrWhiteSpace(code)) code = "EVENTFLOW";
        
        // Generate pseudo-deterministic 15x15 matrix from code hash
        var hash = 17;
        foreach (var ch in code) hash = hash * 31 + ch;
        var rand = new Random(Math.Abs(hash));

        const int gridSize = 17;
        var matrix = new bool[gridSize, gridSize];

        // Finder patterns in 3 corners (7x7)
        void DrawFinder(int startX, int startY)
        {
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4))
                    {
                        matrix[startX + r, startY + c] = true;
                    }
                }
            }
        }

        DrawFinder(0, 0);
        DrawFinder(0, gridSize - 7);
        DrawFinder(gridSize - 7, 0);

        // Fill data area
        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                // Skip finder areas
                if ((r < 8 && c < 8) || (r < 8 && c >= gridSize - 8) || (r >= gridSize - 8 && c < 8))
                {
                    continue;
                }
                matrix[r, c] = rand.Next(100) < 45;
            }
        }

        var cellSize = size / (double)gridSize;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {size} {size}' width='{size}' height='{size}'>");
        sb.Append($"<rect width='{size}' height='{size}' fill='#FFFFFF' rx='8'/>");

        for (int r = 0; r < gridSize; r++)
        {
            for (int c = 0; c < gridSize; c++)
            {
                if (matrix[r, c])
                {
                    var x = c * cellSize;
                    var y = r * cellSize;
                    sb.Append($"<rect x='{x:F1}' y='{y:F1}' width='{cellSize:F1}' height='{cellSize:F1}' fill='#1E293B'/>");
                }
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}

using SkiaSharp;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// DEVELOPMENT ONLY: draws a simple postcard (sky gradient, sun, hills, a
/// title) as a WebP image, so demo packages have photos without downloading
/// anything - the same SkiaSharp library the upload pipeline uses.
/// </summary>
internal static class DemoPostcard
{
    private const int Width = 1600;
    private const int Height = 1000;

    /// <param name="title">Large text at the bottom - the package's name.</param>
    /// <param name="subtitle">Smaller text under it, e.g. the destination.</param>
    /// <param name="skyTop">Hex colour at the top of the sky, e.g. "#0EA5E9".</param>
    /// <param name="skyBottom">Hex colour at the horizon.</param>
    /// <param name="variant">0 = cover (sun high, big title); 1, 2 = other angles of the same scene.</param>
    public static byte[] Draw(string title, string subtitle, string skyTop, string skyBottom, int variant)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        var top = SKColor.Parse(skyTop);
        var bottom = SKColor.Parse(skyBottom);

        // Sky: a vertical gradient.
        using (var sky = new SKPaint { IsAntialias = true })
        {
            sky.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, Height), [top, bottom], SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, Width, Height, sky);
        }

        // Sun: moves with the variant, so the three photos differ.
        using (var sun = new SKPaint { IsAntialias = true, Color = new SKColor(255, 244, 214, 230) })
            canvas.DrawCircle(Width * (0.72f - 0.22f * variant), Height * (0.28f + 0.08f * variant), 110, sun);

        // Three layers of hills, darker towards the front.
        for (var layer = 0; layer < 3; layer++)
        {
            // SkiaSharp 4 builds a shape with SKPathBuilder; Detach() hands over the finished path.
            using var builder = new SKPathBuilder();
            var baseY = Height * (0.58f + 0.12f * layer);
            builder.MoveTo(0, Height);
            builder.LineTo(0, baseY);
            for (var x = 0; x <= Width; x += 200)
            {
                var wave = (float)Math.Sin((x / 260.0) + layer * 1.7 + variant) * (70 - layer * 15);
                builder.LineTo(x, baseY + wave);
            }
            builder.LineTo(Width, Height);
            builder.Close();
            using var path = builder.Detach();

            using var hill = new SKPaint { IsAntialias = true, Color = Shade(bottom, 0.55f - 0.15f * layer) };
            canvas.DrawPath(path, hill);
        }

        // A dark band at the bottom keeps white text readable on any colours.
        using (var band = new SKPaint { Color = new SKColor(0, 0, 0, 110) })
            canvas.DrawRect(0, Height - 230, Width, 230, band);

        using var bold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? SKTypeface.Default;
        using var regular = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal) ?? SKTypeface.Default;
        using var titleFont = new SKFont(bold, variant == 0 ? 84 : 64);
        using var subtitleFont = new SKFont(regular, 40);
        using var white = new SKPaint { IsAntialias = true, Color = SKColors.White };
        using var softWhite = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 210) };
        canvas.DrawText(title, 70, Height - 120, SKTextAlign.Left, titleFont, white);
        canvas.DrawText(subtitle, 72, Height - 55, SKTextAlign.Left, subtitleFont, softWhite);

        using var image = surface.Snapshot();
        using var webp = image.Encode(SKEncodedImageFormat.Webp, 80);
        return webp.ToArray();
    }

    /// <summary>The colour made darker: factor 0.4 = 40% of its brightness.</summary>
    private static SKColor Shade(SKColor color, float factor) =>
        new((byte)(color.Red * factor), (byte)(color.Green * factor), (byte)(color.Blue * factor));
}

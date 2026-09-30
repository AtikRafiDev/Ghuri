using Ghuri.Application.Abstractions.Ports;
using SkiaSharp;

namespace Ghuri.Infrastructure.Images;

/// <summary>
/// IImageProcessor on SkiaSharp (MIT licence - blueprint 9.3 names
/// "SkiaImageProcessor"; ImageSharp was avoided for its revenue-based licence).
/// </summary>
/// <remarks>
/// The file type is decided by SkiaSharp reading the bytes themselves, never by
/// the file name or the browser's Content-Type - a "photo.jpg" that is really
/// an HTML page or a script simply fails to decode. Re-encoding also drops all
/// metadata (EXIF: GPS location, camera serial number).
/// </remarks>
internal sealed class SkiaImageProcessor : IImageProcessor
{
    // A small file can still claim a gigantic canvas (a "decompression bomb":
    // a few KB that unpack into gigabytes of pixels). 50 megapixels is well
    // above any real camera photo.
    private const long MaxSourcePixels = 50_000_000;

    // 80 is the usual sweet spot: no visible loss, a fraction of the JPEG size.
    private const int WebPQuality = 80;

    public async Task<ProcessedImage?> ResizeToWebPAsync(Stream image, int maxLongEdge, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken);

        using var data = SKData.CreateCopy(buffer.ToArray());
        using var codec = SKCodec.Create(data);
        if (codec is null
            || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp)
            || (long)codec.Info.Width * codec.Info.Height > MaxSourcePixels)
            return null;

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null)
            return null; // the header looked fine but the pixel data is broken

        using var source = SKImage.FromBitmap(decoded);
        using var upright = Render(source, codec.EncodedOrigin, maxLongEdge);
        using var webp = upright.Encode(SKEncodedImageFormat.Webp, WebPQuality);
        return new ProcessedImage(webp.ToArray(), upright.Width, upright.Height);
    }

    /// <summary>Draws the source upright and shrunk in one pass.</summary>
    internal static SKImage Render(SKImage source, SKEncodedOrigin origin, int maxLongEdge)
    {
        var sideways = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = sideways ? source.Height : source.Width;
        var height = sideways ? source.Width : source.Height;

        var scale = Math.Min(1.0, (double)maxLongEdge / Math.Max(width, height)); // shrink only, never enlarge
        var outWidth = Math.Max(1, (int)Math.Round(width * scale));
        var outHeight = Math.Max(1, (int)Math.Round(height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(outWidth, outHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent); // PNG transparency survives into the WebP
        canvas.Scale((float)outWidth / width, (float)outHeight / height);
        TurnUpright(canvas, origin, width, height);
        // Mipmaps = pre-shrunk copies, so a big downscale stays smooth instead of grainy.
        canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        return surface.Snapshot();
    }

    /// <summary>
    /// The EXIF orientation note, applied (width/height = the UPRIGHT size).
    /// Canvas calls stack up, so the LAST call listed acts on the picture first.
    /// </summary>
    private static void TurnUpright(SKCanvas canvas, SKEncodedOrigin origin, int width, int height)
    {
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: // mirrored left-right
                canvas.Translate(width, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight: // upside down
                canvas.Translate(width, height);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft: // mirrored top-bottom
                canvas.Translate(0, height);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop: // mirrored along the diagonal
                canvas.Scale(-1, 1);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightTop: // the usual phone portrait: turn 90° clockwise
                canvas.Translate(width, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom: // mirrored along the other diagonal
                canvas.Translate(width, height);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.LeftBottom: // turn 90° anticlockwise
                canvas.Translate(0, height);
                canvas.RotateDegrees(-90);
                break;
        }
    }
}

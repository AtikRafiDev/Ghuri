namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Turns an uploaded picture into a web-ready one (blueprint 9.3:
/// IImageProcessor.ResizeToWebPAsync). Infrastructure does the pixel work;
/// handlers only ask for the result.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Decodes the image, turns it upright (phone photos are often stored
    /// sideways plus a "rotate me" note), shrinks it so its longest side is
    /// at most maxLongEdge pixels, and re-encodes it as WebP.
    /// </summary>
    /// <returns>Null when the bytes are not a JPEG, PNG or WebP it can read.</returns>
    Task<ProcessedImage?> ResizeToWebPAsync(Stream image, int maxLongEdge, CancellationToken cancellationToken);
}

/// <summary>The finished WebP file and its final size in pixels.</summary>
public sealed record ProcessedImage(byte[] Bytes, int Width, int Height);

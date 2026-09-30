namespace Ghuri.Application.Features.Files.Commands.UploadFile;

/// <param name="Id">Save this on the destination/category/package (its ImageFileId).</param>
/// <param name="Url">For an immediate preview in the admin form.</param>
/// <param name="Width">Final width in pixels, after resizing.</param>
/// <param name="Height">Final height in pixels, after resizing.</param>
public sealed record UploadedFileDto(Guid Id, string Url, int Width, int Height);

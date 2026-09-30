using System.Security.Cryptography;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Files.Commands.UploadFile;

internal sealed class UploadFileHandler(
    IImageProcessor images,
    IFileStorage storage,
    IFileObjectRepository files,
    ICurrentUser currentUser,
    TimeProvider clock) : ICommandHandler<UploadFileCommand, UploadedFileDto>
{
    // Blueprint: "large 1920px". Big enough for a full-width hero image on a
    // laptop, small enough that a 6 MB phone photo becomes ~300 KB.
    private const int MaxLongEdgePixels = 1920;

    public async ValueTask<Result<UploadedFileDto>> Handle(UploadFileCommand command, CancellationToken cancellationToken)
    {
        var image = await images.ResizeToWebPAsync(command.Content, MaxLongEdgePixels, cancellationToken);
        if (image is null)
            return FileErrors.NotAnImage;

        // A fresh time-ordered GUID as the name: unguessable, never collides,
        // and nothing the uploader typed ever becomes part of a path.
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var storageKey = $"images/{nowUtc:yyyy}/{nowUtc:MM}/{Guid.CreateVersion7()}.webp";

        // Written to disk BEFORE the database row is saved: if saving the row
        // then fails, the worst case is an unused file on disk - never a row
        // pointing at a file that doesn't exist.
        using (var content = new MemoryStream(image.Bytes, writable: false))
            await storage.SaveAsync(storageKey, content, cancellationToken);

        var file = FileObject.Create(
            storageKey,
            Path.GetFileName(command.FileName),
            contentType: "image/webp",
            sizeBytes: image.Bytes.Length,
            sha256: Convert.ToHexStringLower(SHA256.HashData(image.Bytes)),
            isPublic: true, // catalogue images are shown to every visitor
            nowUtc,
            uploadedBy: currentUser.UserId);
        files.Add(file);

        return new UploadedFileDto(file.Id, storage.GetPublicUrl(storageKey), image.Width, image.Height);
    }
}

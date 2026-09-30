using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Files.Commands.UploadFile;
using Ghuri.Domain.Entities.Ops;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Files;

/// <summary>File uploads (blueprint section 11, "Shared: POST files (multipart)").</summary>
[ApiController]
[Route("api/v1/files")]
[Authorize(Policy = Policies.AdminArea)] // only staff upload catalogue images for now
public sealed class FilesController(ISender sender) : ControllerBase
{
    // The 10 MB file itself plus room for the multipart wrapping around it.
    // Anything bigger is refused while it's still arriving, before it is read
    // into memory; a file between 10 and 11 MB gets the validator's clear
    // "larger than 10 MB" message instead.
    private const long MaxRequestBytes = FileObject.MaxSizeBytes + 1024 * 1024;

    /// <summary>Upload one image (form field "file"). Returns its id and URL; stored as WebP, max 1920 px.</summary>
    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await sender.Send(new UploadFileCommand(content, file.FileName, file.Length), cancellationToken);

        // 201 + Location = the new image's own address.
        return result.IsSuccess ? Created(result.Value.Url, result.Value) : ResultExtensions.ToProblem(result.Error);
    }
}

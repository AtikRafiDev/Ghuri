using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Files.Commands.UploadFile;

/// <summary>
/// Stores one uploaded image (blueprint Day 3: "UploadFile command - type/size
/// check, resize to WebP"). Returns the new file's id, which a destination's
/// or package's gallery then lists (e.g. imageFileIds when saving a destination).
/// </summary>
/// <param name="Content">The raw upload. The caller owns it and disposes it.</param>
/// <param name="FileName">The name on the uploader's computer - kept only as information.</param>
/// <param name="SizeBytes">The upload's size, checked before any decoding work starts.</param>
public sealed record UploadFileCommand(Stream Content, string FileName, long SizeBytes) : ICommand<UploadedFileDto>;

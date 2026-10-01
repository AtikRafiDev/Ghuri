using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.SetPackageImages;

/// <summary>
/// Makes the gallery exactly this list of uploaded files (POST /api/v1/files),
/// in display order - the first is the cover. One command instead of
/// Add/Remove/SetCover: the gallery upload component always sends the whole list.
/// </summary>
public sealed record SetPackageImagesCommand(Guid Id, IReadOnlyList<Guid> ImageFileIds) : ICommand;

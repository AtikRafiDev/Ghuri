using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.ArchivePackage;

/// <summary>Takes a package off the public site but keeps it, so it can be fixed and published again.</summary>
public sealed record ArchivePackageCommand(Guid Id) : ICommand;

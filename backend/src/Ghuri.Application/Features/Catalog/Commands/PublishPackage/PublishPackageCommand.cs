using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.PublishPackage;

/// <summary>Puts a Draft or Archived package on the public site, if it passes every publish rule.</summary>
public sealed record PublishPackageCommand(Guid Id) : ICommand;

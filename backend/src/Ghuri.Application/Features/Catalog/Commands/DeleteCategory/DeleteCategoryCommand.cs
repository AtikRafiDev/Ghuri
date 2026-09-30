using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.DeleteCategory;

/// <summary>Soft delete - hidden everywhere, name/slug free again, row kept for the audit trail.</summary>
public sealed record DeleteCategoryCommand(Guid Id) : ICommand;

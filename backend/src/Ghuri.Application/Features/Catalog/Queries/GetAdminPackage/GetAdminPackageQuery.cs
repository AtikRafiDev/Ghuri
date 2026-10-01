using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetAdminPackage;

/// <summary>One package with everything the edit page and its tabs need (blueprint: GET admin/packages/{id}).</summary>
public sealed record GetAdminPackageQuery(Guid Id) : IQuery<AdminPackageDto>;

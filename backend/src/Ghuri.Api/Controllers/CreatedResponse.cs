namespace Ghuri.Api.Controllers;

/// <summary>Body of a 201 Created answer: the new record's id, to use in later PUT/DELETE calls.</summary>
public sealed record CreatedResponse(Guid Id);

using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Commands.SavePackageItinerary;

/// <summary>Makes the itinerary exactly these days: the first in the list is Day 1, the second Day 2...</summary>
public sealed record SavePackageItineraryCommand(Guid Id, IReadOnlyList<ItineraryDayInput> Days) : ICommand;

/// <summary>One day as the form sends it. Meals are flags, e.g. "B,L,D" for breakfast, lunch and dinner included.</summary>
public sealed record ItineraryDayInput(string Title, string Description, string? Meals, string? Accommodation);

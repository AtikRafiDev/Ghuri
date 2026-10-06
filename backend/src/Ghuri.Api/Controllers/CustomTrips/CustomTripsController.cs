using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.CustomTrips.Commands.CancelCustomTrip;
using Ghuri.Application.Features.CustomTrips.Commands.SubmitCustomTrip;
using Ghuri.Application.Features.CustomTrips.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.CustomTrips;

/// <summary>The customer's own custom trip requests (17-day plan, Day 13). Login required.</summary>
[ApiController]
[Route("api/v1/custom-trips")]
[Authorize]
public sealed class CustomTripsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Ask for a trip: { startDate, adults, children, infants, hotelLevel: 1 budget · 2 standard · 3 premium,
    /// budgetPerPerson, notes, legs: [{ destinationId, nights, transferToNext: 1 none · 2 bus · 3 train · 4 air · 5 car · 6 launch }] }.
    /// 201 = { tripNo, startDate, endDate, totalNights }.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitCustomTripCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/custom-trips/{result.Value.TripNo}", result.Value)
            : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>All your requests, newest first ("My trips").</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new GetMyCustomTripsQuery(), cancellationToken)).ToActionResult();

    /// <summary>One of your requests with its stops, quote and timeline. Someone else's is 404.</summary>
    [HttpGet("{tripNo}")]
    public async Task<IActionResult> Get(string tripNo, CancellationToken cancellationToken) =>
        (await sender.Send(new GetMyCustomTripQuery(tripNo), cancellationToken)).ToActionResult();

    /// <summary>Withdraw it - body { "reason": "..." } (optional). Only before accepting a quote.</summary>
    [HttpPost("{tripNo}/cancel")]
    public async Task<IActionResult> Cancel(string tripNo, CancelTripRequest? request, CancellationToken cancellationToken) =>
        (await sender.Send(new CancelCustomTripCommand(tripNo, request?.Reason), cancellationToken)).ToActionResult();
}

/// <summary>Body of POST custom-trips/{tripNo}/cancel.</summary>
public sealed record CancelTripRequest(string? Reason);

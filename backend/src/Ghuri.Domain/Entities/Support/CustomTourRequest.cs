using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Support;

/// <summary>A request for a tour not in the catalogue (blueprint: support.CustomTourRequests [A]).</summary>
public sealed class CustomTourRequest : AggregateRoot, IAuditable
{
    /// <summary>Human-readable code like CR1001, generated from a SQL SEQUENCE (see AppDbContext).</summary>
    public string RequestNo { get; private set; } = string.Empty;

    /// <summary>Null for a visitor who isn't logged in.</summary>
    public Guid? UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public PhoneNumber Phone { get; private set; } = null!;
    public string? Email { get; private set; }

    /// <summary>Free text - not a real Destination FK, since the whole point is a place not in the catalogue.</summary>
    public string DestinationText { get; private set; } = string.Empty;

    public DateOnly? PreferredStartDate { get; private set; }
    public byte? Days { get; private set; }
    public byte Adults { get; private set; }
    public byte Children { get; private set; }
    public decimal? BudgetPerPerson { get; private set; }
    public string? Message { get; private set; }
    public CustomTourRequestStatus Status { get; private set; }
    public Guid? AssignedTo { get; private set; }

    private CustomTourRequest()
    {
    }

    public static CustomTourRequest Create(
        string requestNo, string name, PhoneNumber phone, string destinationText, byte adults, byte children,
        Guid? userId = null, string? email = null, DateOnly? preferredStartDate = null, byte? days = null,
        decimal? budgetPerPerson = null, string? message = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestNo);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationText);

        return new CustomTourRequest
        {
            RequestNo = requestNo,
            UserId = userId,
            Name = name,
            Phone = phone,
            Email = email,
            DestinationText = destinationText,
            PreferredStartDate = preferredStartDate,
            Days = days,
            Adults = adults,
            Children = children,
            BudgetPerPerson = budgetPerPerson,
            Message = message,
            Status = CustomTourRequestStatus.New
        };
    }
}

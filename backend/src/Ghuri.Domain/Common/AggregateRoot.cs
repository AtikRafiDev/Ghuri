namespace Ghuri.Domain.Common;

/// <summary>
/// A BaseEntity that is also the "entry point" for a group of related
/// entities that must always change together consistently (e.g. a
/// TourPackage together with its ItineraryDays and PackageImages - you
/// never load or save an ItineraryDay by itself). Only aggregate roots
/// get their own repository (blueprint section 7) and can raise domain
/// events; the entities inside an aggregate are reached only through it.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Called by Infrastructure right after the events have been copied
    /// into the outbox table during SaveChanges - never called by
    /// Application or Api code directly.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

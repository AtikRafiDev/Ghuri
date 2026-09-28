using Ghuri.Application.Common;

namespace Ghuri.Application.Abstractions.Messaging;

/// <summary>
/// A read that never changes data, e.g. SearchPackages. Kept as a separate
/// type from ICommand on purpose: TransactionBehavior is constrained to
/// commands only, so a query can never accidentally open a write
/// transaction (blueprint: "commands write, queries read").
/// </summary>
public interface IQuery<TResponse> : Mediator.IQuery<Result<TResponse>>;

public interface IQueryHandler<in TQuery, TResponse> : Mediator.IQueryHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;

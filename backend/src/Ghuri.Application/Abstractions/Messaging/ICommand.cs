using Ghuri.Application.Common;

namespace Ghuri.Application.Abstractions.Messaging;

// These wrap Mediator's own interfaces with one extra rule: the response is
// ALWAYS a Result (or Result<T>). A handler physically cannot be written to
// return a raw value or throw for an expected failure - the compiler
// refuses. That's what lets the pipeline behaviors treat every use case
// the same way.

/// <summary>A state change with no return value, e.g. CancelBooking. Handled by exactly one ICommandHandler.</summary>
public interface ICommand : Mediator.ICommand<Result>;

/// <summary>A state change that returns something, e.g. CreateBooking returns the new booking's id and number.</summary>
public interface ICommand<TResponse> : Mediator.ICommand<Result<TResponse>>;

public interface ICommandHandler<in TCommand> : Mediator.ICommandHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : Mediator.ICommandHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Common;
using Mediator;

namespace Ghuri.Application.Behaviors;

/// <summary>
/// Wraps every COMMAND in one database transaction: commit if the handler
/// returned success, roll everything back if it returned a failure
/// (blueprint section 7.2).
/// </summary>
/// <remarks>
/// "where TMessage : IBaseCommand" is the important line - Mediator only
/// applies this behavior to commands. Queries skip it entirely, so reads
/// never pay for a transaction they don't need and can never write.
/// </remarks>
public sealed class TransactionBehavior<TMessage, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IBaseCommand
    where TResponse : IResult
{
    public ValueTask<TResponse> Handle(
        TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken) =>
        new(unitOfWork.ExecuteInTransactionAsync(
            ct => next(message, ct).AsTask(),
            response => response.IsSuccess,
            cancellationToken));
}

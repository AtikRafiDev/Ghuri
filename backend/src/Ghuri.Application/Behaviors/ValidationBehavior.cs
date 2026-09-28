using FluentValidation;
using Ghuri.Application.Common;
using Mediator;

namespace Ghuri.Application.Behaviors;

/// <summary>
/// Runs every FluentValidation validator for a message BEFORE its handler.
/// Handlers can then assume their input is well-formed (Adults >= 1, a
/// valid phone number...) and only deal with business rules.
/// </summary>
/// <remarks>
/// Returns a failed Result instead of throwing - bad input is an expected
/// failure, not a crash (blueprint's Result pattern). TResponse.CreateFailure
/// is the static-abstract factory from IResultFactory, which is how this one
/// generic class can build a Result&lt;Guid&gt;, Result&lt;BookingDto&gt;, etc.
/// </remarks>
public sealed class ValidationBehavior<TMessage, TResponse>(IEnumerable<IValidator<TMessage>> validators)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IResultFactory<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators as IValidator<TMessage>[] ?? validators.ToArray();
        if (validatorList.Length == 0)
            return await next(message, cancellationToken);

        var context = new ValidationContext<TMessage>(message);
        var results = await Task.WhenAll(validatorList.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(r => r.Errors).ToList();
        if (failures.Count == 0)
            return await next(message, cancellationToken);

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        // The handler never runs at all.
        return TResponse.CreateFailure(new ValidationError(errors));
    }
}

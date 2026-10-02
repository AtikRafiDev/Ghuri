using System.Text.Json;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Payments.Commands.RecordGatewayReturn;

/// <summary>
/// The customer's browser came back from the gateway's page (success, fail or
/// cancel) with the gateway's form fields. Day 9: save them as a
/// PaymentEvent - the raw record of what arrived. Day 10 adds the real work:
/// checking the payment with SSLCommerz's validation API and confirming the booking.
/// </summary>
/// <remarks>
/// Nothing here is trusted yet: anyone can POST these fields. That's why
/// this only RECORDS them - a booking is only ever confirmed after the
/// gateway itself confirms the payment (Day 10).
/// </remarks>
public sealed record RecordGatewayReturnCommand(PaymentEventType EventType, IReadOnlyDictionary<string, string> Fields) : ICommand;

internal sealed class RecordGatewayReturnHandler(IPaymentRepository payments, TimeProvider clock)
    : ICommandHandler<RecordGatewayReturnCommand>
{
    public async ValueTask<Result> Handle(RecordGatewayReturnCommand command, CancellationToken cancellationToken)
    {
        var transactionId = command.Fields.GetValueOrDefault("tran_id");
        if (string.IsNullOrWhiteSpace(transactionId))
            return Result.Success(); // nothing to tie it to - not even worth keeping

        // "SuccessReturn:PAY100001:<val_id>" - the same message twice (a
        // refresh, the back button) is saved once: (Provider, id) is unique.
        var valId = command.Fields.GetValueOrDefault("val_id");
        var eventId = Truncate($"{command.EventType}:{transactionId}:{(string.IsNullOrWhiteSpace(valId) ? "-" : valId)}", 100);
        if (await payments.EventExistsAsync(PaymentProvider.SslCommerz, eventId, cancellationToken))
            return Result.Success();

        var payment = await payments.GetByPaymentNoAsync(transactionId, cancellationToken);
        payments.AddEvent(PaymentEvent.Receive(
            PaymentProvider.SslCommerz, command.EventType, eventId,
            JsonSerializer.Serialize(command.Fields), clock.GetUtcNow().UtcDateTime, payment?.Id));

        return Result.Success();
    }

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];
}

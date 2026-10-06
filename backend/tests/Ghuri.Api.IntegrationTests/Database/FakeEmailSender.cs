using System.Collections.Concurrent;
using Ghuri.Application.Abstractions.Ports;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>Keeps every email the app "sends" so a test can read them. Nothing leaves the machine.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    public ConcurrentQueue<EmailMessage> Sent { get; } = new();

    /// <summary>When set, sending throws this - "the mail server is down".</summary>
    public Exception? FailWith { get; set; }

    public void Reset()
    {
        Sent.Clear();
        FailWith = null;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
            throw FailWith;

        Sent.Enqueue(message);
        return Task.CompletedTask;
    }
}

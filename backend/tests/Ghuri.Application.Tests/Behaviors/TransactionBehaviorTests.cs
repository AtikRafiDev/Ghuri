using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Behaviors;
using Ghuri.Application.Common;

namespace Ghuri.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private sealed record TestCommand : ICommand;

    /// <summary>Fake unit of work that records whether it would have committed, instead of touching a database.</summary>
    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool? Committed { get; private set; }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> work, Func<T, bool> shouldCommit, CancellationToken cancellationToken = default)
        {
            var result = await work(cancellationToken);
            Committed = shouldCommit(result);
            return result;
        }
    }

    [Fact]
    public async Task SuccessfulHandler_Commits()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        await behavior.Handle(new TestCommand(), (_, _) => ValueTask.FromResult(Result.Success()), CancellationToken.None);

        Assert.True(unitOfWork.Committed);
    }

    [Fact]
    public async Task FailedHandler_RollsBack_EvenThoughNothingWasThrown()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        await behavior.Handle(
            new TestCommand(),
            (_, _) => ValueTask.FromResult(Result.Failure(Error.Failure("coupon_invalid", "Coupon is invalid."))),
            CancellationToken.None);

        Assert.False(unitOfWork.Committed);
    }

    [Fact]
    public async Task FailedHandler_Commits_WhenTheErrorAsksToKeepChanges()
    {
        // e.g. wrong password: the answer is 401, but the failed-attempt
        // count must still be saved or the account never locks.
        var unitOfWork = new RecordingUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);
        var error = Error.Unauthorized("invalid_credentials", "Wrong phone or password.") with { CommitChanges = true };

        var result = await behavior.Handle(
            new TestCommand(), (_, _) => ValueTask.FromResult(Result.Failure(error)), CancellationToken.None);

        Assert.True(unitOfWork.Committed);
        Assert.True(result.IsFailure);
    }
}

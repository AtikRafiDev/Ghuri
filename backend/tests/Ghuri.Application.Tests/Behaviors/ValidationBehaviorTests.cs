using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Behaviors;
using Ghuri.Application.Common;

namespace Ghuri.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    // A throwaway command that exists only for these tests.
    private sealed record TestCommand(int Adults) : ICommand<Guid>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() => RuleFor(c => c.Adults).GreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task InvalidInput_ReturnsValidationError_AndNeverRunsTheHandler()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>([new TestCommandValidator()]);
        var handlerRan = false;

        var result = await behavior.Handle(
            new TestCommand(Adults: 0),
            (_, _) => { handlerRan = true; return ValueTask.FromResult<Result<Guid>>(Guid.NewGuid()); },
            CancellationToken.None);

        Assert.False(handlerRan);
        Assert.True(result.IsFailure);
        var validation = Assert.IsType<ValidationError>(result.Error);
        Assert.True(validation.Errors.ContainsKey(nameof(TestCommand.Adults)));
    }

    [Fact]
    public async Task ValidInput_RunsTheHandler()
    {
        var behavior = new ValidationBehavior<TestCommand, Result<Guid>>([new TestCommandValidator()]);
        var expectedId = Guid.NewGuid();

        var result = await behavior.Handle(
            new TestCommand(Adults: 2),
            (_, _) => ValueTask.FromResult<Result<Guid>>(expectedId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedId, result.Value);
    }
}

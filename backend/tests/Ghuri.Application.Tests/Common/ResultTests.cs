using Ghuri.Application.Common;

namespace Ghuri.Application.Tests.Common;

public class ResultTests
{
    private static readonly Error SampleError = Error.Conflict("not_enough_seats", "Not enough seats left.");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_CarriesTheError()
    {
        var result = Result.Failure(SampleError);

        Assert.True(result.IsFailure);
        Assert.Equal("not_enough_seats", result.Error.Code);
    }

    [Fact]
    public void Failure_WithErrorNone_IsRejected()
    {
        // A "failure" with no reason would be impossible to show or debug.
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        Result<int> result = 42; // implicit conversion from the value

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ReadingValue_Throws()
    {
        Result<int> result = SampleError; // implicit conversion from an Error

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}

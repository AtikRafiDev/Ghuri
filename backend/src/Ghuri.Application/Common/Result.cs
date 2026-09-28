namespace Ghuri.Application.Common;

/// <summary>
/// What every Result has in common, so pipeline behaviors can check
/// "did it succeed?" without knowing the exact Result&lt;T&gt; type.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error Error { get; }
}

/// <summary>
/// Lets generic code (ValidationBehavior) build a FAILED result of whatever
/// type a handler returns - Result, Result&lt;Guid&gt;, Result&lt;BookingDto&gt; -
/// without reflection. "static abstract" means each Result type supplies
/// its own static CreateFailure method, and the compiler checks that it
/// exists.
/// </summary>
public interface IResultFactory<TSelf> where TSelf : IResultFactory<TSelf>
{
    static abstract TSelf CreateFailure(Error error);
}

/// <summary>
/// The outcome of a use case with no return value: either success, or a
/// failure carrying an Error. Handlers return this instead of throwing for
/// expected problems.
/// </summary>
public class Result : IResult, IResultFactory<Result>
{
    protected Result(bool isSuccess, Error error)
    {
        // Guard against the two impossible combinations, so a Result can
        // never lie about itself.
        if (isSuccess && error != Error.None)
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        if (!isSuccess && error == Error.None)
            throw new ArgumentException("A failed result must carry an error.", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Lets a handler write "return Errors.X;" instead of "return Result.Failure(Errors.X);".</summary>
    public static implicit operator Result(Error error) => Failure(error);

    static Result IResultFactory<Result>.CreateFailure(Error error) => Failure(error);
}

/// <summary>A Result that also carries a value when it succeeds.</summary>
public sealed class Result<TValue> : Result, IResultFactory<Result<TValue>>
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Throws if the result failed - reading the value of a failure is
    /// always a bug, and failing loudly beats silently passing a null
    /// onward.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the Value of a failed result.");

    /// <summary>Lets a handler write "return bookingDto;" instead of "return Result.Success(bookingDto);".</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    static Result<TValue> IResultFactory<Result<TValue>>.CreateFailure(Error error) => Failure<TValue>(error);
}

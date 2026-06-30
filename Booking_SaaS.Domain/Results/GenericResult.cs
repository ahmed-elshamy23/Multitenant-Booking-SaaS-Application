namespace Booking_SaaS.Domain.Results;

public sealed class Result<T> : Result
{
    private Result(T value)
    {
        Value = value;
    }

    private Result(Error error) : base(error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Value = default;
    }

    public T? Value { get; }

    public static implicit operator Result<T>(Error error)
    {
        return new Result<T>(error);
    }

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }
}
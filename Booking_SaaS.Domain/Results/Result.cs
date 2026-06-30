namespace Booking_SaaS.Domain.Results;

public class Result
{
    protected Result()
    {
        IsSuccess = true;
    }

    protected Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public bool IsSuccess { get; }
    public Error? Error { get; }

    public static implicit operator Result(Error error)
    {
        return new Result(error);
    }

    public static Result Success()
    {
        return new Result();
    }

    public static Result Failure(Error error)
    {
        return new Result(error);
    }
}
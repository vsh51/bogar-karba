namespace Domain.Common;

public class Result
{
    protected Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(true, string.Empty);

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result Failure(string message) => new(false, message);

    public static Result<T> Failure<T>(string message) => Result<T>.CreateFailure(message);
}

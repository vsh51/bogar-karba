namespace Domain.Common;

public class Result<T> : Result
{
    private Result(bool isSuccess, string message, T? value)
        : base(isSuccess, message)
    {
        Value = value;
    }

    public T? Value { get; }

    internal static Result<T> CreateSuccess(T value) => new(true, string.Empty, value);

    internal static Result<T> CreateFailure(string message) => new(false, message, default);
}

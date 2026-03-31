using System.Diagnostics.CodeAnalysis;

namespace Application.Common;

public class Result<T>
{
    protected Result(T? value, bool succeeded, string? errorMessage)
    {
        Value = value;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    public T? Value { get; }

    public bool Succeeded { get; }

    public string? ErrorMessage { get; }

    public static implicit operator Result<T>(T value) => Success(value);

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Standard Result pattern implementation")]
    public static Result<T> Success(T value) => new(value, true, null);

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Standard Result pattern implementation")]
    public static Result<T> Failure(string errorMessage) => new(default, false, errorMessage);
}

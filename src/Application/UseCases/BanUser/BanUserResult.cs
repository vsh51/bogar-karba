namespace Application.UseCases.BanUser;

public enum BanUserErrorType
{
    None,
    NotFound,
    Unexpected,
}

public class BanUserResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public BanUserErrorType ErrorType { get; init; } = BanUserErrorType.None;

    public static BanUserResult Success() => new() { Succeeded = true };

    public static BanUserResult NotFound(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage, ErrorType = BanUserErrorType.NotFound };

    public static BanUserResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage, ErrorType = BanUserErrorType.Unexpected };
}

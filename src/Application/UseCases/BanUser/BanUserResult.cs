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

    public BanUserErrorType ErrorType { get; init; } = BanUserErrorType.None;

    public static BanUserResult Success() => new() { Succeeded = true };

    public static BanUserResult NotFound() =>
        new() { Succeeded = false, ErrorType = BanUserErrorType.NotFound };

    public static BanUserResult Failure() =>
        new() { Succeeded = false, ErrorType = BanUserErrorType.Unexpected };
}

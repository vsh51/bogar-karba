namespace Application.UseCases.BanUser;

public class BanUserResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public static BanUserResult Success() => new() { Succeeded = true };

    public static BanUserResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}

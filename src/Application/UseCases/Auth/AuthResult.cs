namespace Application.UseCases.Auth;

public class AuthResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public static AuthResult Success() => new() { Succeeded = true };

    public static AuthResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}

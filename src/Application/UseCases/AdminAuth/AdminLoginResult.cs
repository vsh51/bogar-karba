namespace Application.UseCases.AdminAuth;

public class AdminLoginResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public static AdminLoginResult Success() => new() { Succeeded = true };

    public static AdminLoginResult Failure(string errorMessage) => new() { Succeeded = false, ErrorMessage = errorMessage };
}

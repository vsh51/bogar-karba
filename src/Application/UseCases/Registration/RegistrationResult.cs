namespace Application.UseCases.Registration;

public class RegistrationResult
{
    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public static RegistrationResult Success() => new() { Succeeded = true };

    public static RegistrationResult Failure(string errorMessage) =>
        new() { Succeeded = false, ErrorMessage = errorMessage };
}

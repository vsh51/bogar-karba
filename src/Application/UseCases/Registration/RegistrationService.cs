using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Registration;

public class RegistrationService : IRegistrationService
{
    private readonly IRegistrationUserRepository _repository;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        IRegistrationUserRepository repository,
        ILogger<RegistrationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RegistrationResult> RegisterAsync(string name, string surname, string email, string password)
    {
        _logger.LogInformation("Registration attempt for email '{Email}'", email);

        var exists = await _repository.UserExistsAsync(email);
        if (exists)
        {
            _logger.LogWarning("Registration failed: email '{Email}' is already taken", email);
            return RegistrationResult.Failure("Email is already taken.");
        }

        var (succeeded, errors) = await _repository.CreateUserAsync(name, surname, email, password, UserStatus.Active);
        if (!succeeded)
        {
            var errorMessage = string.Join(" ", errors);
            _logger.LogWarning("Registration failed for email '{Email}': {Errors}", email, errorMessage);
            return RegistrationResult.Failure(errorMessage);
        }

        _logger.LogInformation("User '{Email}' registered successfully", email);
        return RegistrationResult.Success();
    }
}

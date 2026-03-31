using Application.Common;
using Application.Enums;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.RegisterUser;

public class RegisterUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository repository,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(RegisterUserCommand command)
    {
        _logger.LogInformation("Registration attempt for email '{Email}'", command.Email);

        var exists = await _repository.UserExistsAsync(command.Email, UserLookupMode.ByEmail);
        if (exists)
        {
            _logger.LogWarning("Registration failed: email '{Email}' is already taken", command.Email);
            return Result<bool>.Failure("Email is already taken.");
        }

        var (succeeded, errors) = await _repository.CreateUserAsync(
            command.Name, command.Surname, command.Email, command.Password, UserStatus.Active);
        if (!succeeded)
        {
            var errorMessage = string.Join(" ", errors);
            _logger.LogWarning("Registration failed for email '{Email}': {Errors}", command.Email, errorMessage);
            return Result<bool>.Failure(errorMessage);
        }

        _logger.LogInformation("User '{Email}' registered successfully", command.Email);
        return true;
    }
}

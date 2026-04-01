using Application.Common;
using Application.Enums;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository repository,
    ILogger<RegisterUserCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(RegisterUserCommand command)
    {
        logger.LogInformation("Registration attempt for email '{Email}'", command.Email);

        var exists = await repository.UserExistsAsync(command.Email, UserLookupMode.ByEmail);
        if (exists)
        {
            logger.LogWarning("Registration failed: email '{Email}' is already taken", command.Email);
            return "Email is already taken.";
        }

        var (succeeded, errors) = await repository.CreateUserAsync(
            command.Name, command.Surname, command.Email, command.Password, UserStatus.Active);
        if (!succeeded)
        {
            var errorMessage = string.Join(" ", errors);
            logger.LogWarning("Registration failed for email '{Email}': {Errors}", command.Email, errorMessage);
            return errorMessage;
        }

        logger.LogInformation("User '{Email}' registered successfully", command.Email);
        return true;
    }
}

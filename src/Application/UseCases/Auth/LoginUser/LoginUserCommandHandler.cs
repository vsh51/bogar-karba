using Application.Common;
using Application.Enums;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.LoginUser;

public class LoginUserCommandHandler(
    IUserRepository repository,
    ISignInService signInService,
    ILogger<LoginUserCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(LoginUserCommand command)
    {
        logger.LogInformation("Login attempt for '{Email}'", command.Email);

        if (!await repository.UserExistsAsync(command.Email, UserLookupMode.ByEmail))
        {
            logger.LogWarning("Login failed: user '{Email}' not found", command.Email);
            return "Invalid email or password.";
        }

        if (!await repository.IsActiveAsync(command.Email, UserLookupMode.ByEmail))
        {
            logger.LogWarning("Login denied for '{Email}': account is not active", command.Email);
            return "Your account is blocked.";
        }

        if (!await repository.CheckPasswordAsync(command.Email, command.Password, UserLookupMode.ByEmail))
        {
            logger.LogWarning("Login failed: invalid password for '{Email}'", command.Email);
            return "Invalid email or password.";
        }

        await signInService.SignInAsync(command.Email, UserLookupMode.ByEmail);
        logger.LogInformation("User '{Email}' logged in successfully", command.Email);
        return true;
    }
}

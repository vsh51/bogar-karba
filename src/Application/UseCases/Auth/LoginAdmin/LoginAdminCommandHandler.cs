using Application.Common;
using Application.Enums;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.LoginAdmin;

public sealed class LoginAdminCommandHandler(
    IUserRepository repository,
    ISignInService signInService,
    ILogger<LoginAdminCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(LoginAdminCommand command)
    {
        logger.LogInformation("Admin login attempt for user '{UserName}'", command.UserName);

        if (!await repository.UserExistsAsync(command.UserName, UserLookupMode.ByUserName))
        {
            logger.LogWarning("Admin login failed: user '{UserName}' not found", command.UserName);
            return "Invalid username or password.";
        }

        var roles = await repository.GetRolesAsync(command.UserName, UserLookupMode.ByUserName);
        if (!roles.Contains("Admin"))
        {
            logger.LogWarning("Login denied for user '{UserName}': not an admin", command.UserName);
            return "Invalid username or password.";
        }

        if (!await repository.CheckPasswordAsync(command.UserName, command.Password, UserLookupMode.ByUserName))
        {
            logger.LogWarning("Admin login failed: invalid password for user '{UserName}'", command.UserName);
            return "Invalid username or password.";
        }

        await signInService.SignInAsync(command.UserName, UserLookupMode.ByUserName);
        logger.LogInformation("Admin '{UserName}' logged in successfully", command.UserName);
        return true;
    }
}

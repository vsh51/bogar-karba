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
        var mode = command.LoginIdentifier.Contains('@')
            ? UserLookupMode.ByEmail
            : UserLookupMode.ByUserName;

        logger.LogInformation(
            "Admin login attempt for '{Identifier}' using mode {Mode}",
            command.LoginIdentifier,
            mode);

        if (!await repository.UserExistsAsync(command.LoginIdentifier, mode))
        {
            logger.LogWarning(
                "Admin login failed: user '{Identifier}' not found",
                command.LoginIdentifier);
            return "Invalid login or password.";
        }

        var roles = await repository.GetRolesAsync(command.LoginIdentifier, mode);
        if (!roles.Contains("Admin"))
        {
            logger.LogWarning(
                "Login denied for user '{Identifier}': not an admin",
                command.LoginIdentifier);
            return "Invalid login or password.";
        }

        if (!await repository.CheckPasswordAsync(command.LoginIdentifier, command.Password, mode))
        {
            logger.LogWarning(
                "Admin login failed: invalid password for user '{Identifier}'",
                command.LoginIdentifier);
            return "Invalid login or password.";
        }

        await signInService.SignInAsync(command.LoginIdentifier, mode);
        logger.LogInformation(
            "Admin '{Identifier}' logged in successfully",
            command.LoginIdentifier);

        return true;
    }
}

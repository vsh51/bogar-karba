using Application.Common;
using Application.Enums;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.LoginUser;

public sealed class LoginUserCommandHandler(
    IUserRepository repository,
    ISignInService signInService,
    ILogger<LoginUserCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(LoginUserCommand command)
    {
        var mode = command.LoginIdentifier.Contains('@')
            ? UserLookupMode.ByEmail
            : UserLookupMode.ByUserName;

        logger.LogInformation(
            "Login attempt for '{Identifier}' using mode {Mode}",
            command.LoginIdentifier,
            mode);

        if (!await repository.UserExistsAsync(command.LoginIdentifier, mode))
        {
            logger.LogWarning(
                "Login failed: user '{Identifier}' not found",
                command.LoginIdentifier);
            return "Invalid login or password.";
        }

        if (!await repository.IsActiveAsync(command.LoginIdentifier, mode))
        {
            logger.LogWarning(
                "Login denied for '{Identifier}': account is not active",
                command.LoginIdentifier);
            return "Your account is blocked.";
        }

        if (!await repository.CheckPasswordAsync(command.LoginIdentifier, command.Password, mode))
        {
            logger.LogWarning(
                "Login failed: invalid password for '{Identifier}'",
                command.LoginIdentifier);
            return "Invalid login or password.";
        }

        await signInService.SignInAsync(command.LoginIdentifier, mode);
        logger.LogInformation(
            "User '{Identifier}' logged in successfully",
            command.LoginIdentifier);

        return true;
    }
}

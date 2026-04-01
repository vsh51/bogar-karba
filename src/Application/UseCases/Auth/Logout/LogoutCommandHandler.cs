using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.Logout;

public class LogoutCommandHandler(
    ISignInService signInService,
    ILogger<LogoutCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(LogoutCommand command)
    {
        logger.LogInformation("Logout requested at {Time}", command.RequestedAt);
        await signInService.SignOutAsync();
        logger.LogInformation("Logout completed at {Time}", DateTime.UtcNow);
        return true;
    }
}

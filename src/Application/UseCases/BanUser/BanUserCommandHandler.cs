using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.BanUser;

public class BanUserCommandHandler(
    IUserRepository repository,
    ILogger<BanUserCommandHandler> logger)
{
    public async Task<Result<bool>> HandleAsync(BanUserCommand command)
    {
        logger.LogInformation("Account blocking started for user {UserId}", command.UserId);

        var banned = await repository.BanUserAsync(command.UserId);
        if (!banned)
        {
            logger.LogWarning("Account blocking failed: user {UserId} was not found", command.UserId);
            return ResultErrors.UserNotFound;
        }

        logger.LogInformation("Account blocking completed successfully for user {UserId}", command.UserId);
        return true;
    }
}

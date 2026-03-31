using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.BanUser;

public class BanUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly ILogger<BanUserCommandHandler> _logger;

    public BanUserCommandHandler(IUserRepository repository, ILogger<BanUserCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(BanUserCommand command)
    {
        _logger.LogInformation("Account blocking started for user {UserId}", command.UserId);

        var banned = await _repository.BanUserAsync(command.UserId);
        if (!banned)
        {
            _logger.LogWarning("Account blocking failed: user {UserId} was not found", command.UserId);
            return Result<bool>.Failure(ResultErrors.UserNotFound);
        }

        _logger.LogInformation("Account blocking completed successfully for user {UserId}", command.UserId);
        return true;
    }
}

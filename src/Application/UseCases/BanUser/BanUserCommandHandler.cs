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

    public async Task<BanUserResult> HandleAsync(BanUserCommand command)
    {
        _logger.LogInformation("Account blocking started for user {UserId}", command.UserId);

        try
        {
            var banned = await _repository.BanUserAsync(command.UserId);
            if (!banned)
            {
                _logger.LogWarning("Account blocking failed: user {UserId} was not found", command.UserId);
                return BanUserResult.Failure("User not found.");
            }

            _logger.LogInformation("Account blocking completed successfully for user {UserId}", command.UserId);
            return BanUserResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account blocking failed with exception for user {UserId}", command.UserId);
            return BanUserResult.Failure($"Failed to ban user {command.UserId}");
        }
    }
}

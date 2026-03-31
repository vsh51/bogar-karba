using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.Logout;

public class LogoutCommandHandler
{
    private readonly ISignInService _signInService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        ISignInService signInService,
        ILogger<LogoutCommandHandler> logger)
    {
        _signInService = signInService;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(LogoutCommand command)
    {
        _logger.LogInformation("Logout requested at {Time}", command.RequestedAt);
        await _signInService.SignOutAsync();
        _logger.LogInformation("Logout completed at {Time}", DateTime.UtcNow);
        return true;
    }
}

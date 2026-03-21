using Application.Enums;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.LoginAdmin;

public class LoginAdminCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly ISignInService _signInService;
    private readonly ILogger<LoginAdminCommandHandler> _logger;

    public LoginAdminCommandHandler(
        IUserRepository repository,
        ISignInService signInService,
        ILogger<LoginAdminCommandHandler> logger)
    {
        _repository = repository;
        _signInService = signInService;
        _logger = logger;
    }

    public async Task<AuthResult> HandleAsync(LoginAdminCommand command)
    {
        _logger.LogInformation("Admin login attempt for user '{UserName}'", command.UserName);

        if (!await _repository.UserExistsAsync(command.UserName, UserLookupMode.ByUserName))
        {
            _logger.LogWarning("Admin login failed: user '{UserName}' not found", command.UserName);
            return AuthResult.Failure("Invalid username or password.");
        }

        var roles = await _repository.GetRolesAsync(command.UserName, UserLookupMode.ByUserName);
        if (!roles.Contains("Admin"))
        {
            _logger.LogWarning("Login denied for user '{UserName}': not an admin", command.UserName);
            return AuthResult.Failure("Invalid username or password.");
        }

        if (!await _repository.CheckPasswordAsync(command.UserName, command.Password, UserLookupMode.ByUserName))
        {
            _logger.LogWarning("Admin login failed: invalid password for user '{UserName}'", command.UserName);
            return AuthResult.Failure("Invalid username or password.");
        }

        await _signInService.SignInAsync(command.UserName, UserLookupMode.ByUserName);
        _logger.LogInformation("Admin '{UserName}' logged in successfully", command.UserName);
        return AuthResult.Success();
    }
}

using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth.LoginUser;

public class LoginUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly ISignInService _signInService;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IUserRepository repository,
        ISignInService signInService,
        ILogger<LoginUserCommandHandler> logger)
    {
        _repository = repository;
        _signInService = signInService;
        _logger = logger;
    }

    public async Task<AuthResult> HandleAsync(LoginUserCommand command)
    {
        _logger.LogInformation("Login attempt for '{Email}'", command.Email);

        if (!await _repository.UserExistsAsync(command.Email, UserLookupMode.ByEmail))
        {
            _logger.LogWarning("Login failed: user '{Email}' not found", command.Email);
            return AuthResult.Failure("Invalid email or password.");
        }

        if (!await _repository.IsActiveAsync(command.Email, UserLookupMode.ByEmail))
        {
            _logger.LogWarning("Login denied for '{Email}': account is not active", command.Email);
            return AuthResult.Failure("Invalid email or password.");
        }

        if (!await _repository.CheckPasswordAsync(command.Email, command.Password, UserLookupMode.ByEmail))
        {
            _logger.LogWarning("Login failed: invalid password for '{Email}'", command.Email);
            return AuthResult.Failure("Invalid email or password.");
        }

        await _signInService.SignInAsync(command.Email, UserLookupMode.ByEmail);
        _logger.LogInformation("User '{Email}' logged in successfully", command.Email);
        return AuthResult.Success();
    }
}

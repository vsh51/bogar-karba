using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Auth;

public class AuthService : IAuthService
{
    private readonly IAuthUserRepository _repository;
    private readonly IAuthSignInService _signInService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthUserRepository repository,
        IAuthSignInService signInService,
        ILogger<AuthService> logger)
    {
        _repository = repository;
        _signInService = signInService;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        _logger.LogInformation("Login attempt for '{Email}'", email);

        if (!await _repository.UserExistsAsync(email))
        {
            _logger.LogWarning("Login failed: user '{Email}' not found", email);
            return false;
        }

        if (!await _repository.IsActiveAsync(email))
        {
            _logger.LogWarning("Login denied for '{Email}': account is not active", email);
            return false;
        }

        if (!await _repository.CheckPasswordAsync(email, password))
        {
            _logger.LogWarning("Login failed: invalid password for '{Email}'", email);
            return false;
        }

        await _signInService.SignInAsync(email);
        _logger.LogInformation("User '{Email}' logged in successfully", email);
        return true;
    }

    public async Task LogoutAsync()
    {
        _logger.LogInformation("User logout requested");
        await _signInService.SignOutAsync();
    }
}

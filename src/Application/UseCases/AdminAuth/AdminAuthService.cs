using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.AdminAuth;

public class AdminAuthService : IAdminAuthService
{
    private readonly IAdminUserRepository _repository;
    private readonly IAdminSignInService _signInService;
    private readonly ILogger<AdminAuthService> _logger;

    public AdminAuthService(
        IAdminUserRepository repository,
        IAdminSignInService signInService,
        ILogger<AdminAuthService> logger)
    {
        _repository = repository;
        _signInService = signInService;
        _logger = logger;
    }

    public async Task<AdminLoginResult> LoginAsync(string userName, string password)
    {
        _logger.LogInformation("Admin login attempt for user '{UserName}'", userName);

        var userExists = await _repository.UserExistsAsync(userName);
        if (!userExists)
        {
            _logger.LogWarning("Admin login failed: user '{UserName}' not found", userName);
            return AdminLoginResult.Failure("Invalid username or password.");
        }

        var roles = await _repository.GetRolesAsync(userName);
        if (!roles.Contains("Admin"))
        {
            _logger.LogWarning("Login denied for user '{UserName}': not an admin", userName);
            return AdminLoginResult.Failure("Invalid username or password.");
        }

        var passwordValid = await _repository.CheckPasswordAsync(userName, password);
        if (!passwordValid)
        {
            _logger.LogWarning("Admin login failed: invalid password for user '{UserName}'", userName);
            return AdminLoginResult.Failure("Invalid username or password.");
        }

        await _signInService.SignInAsync(userName);
        _logger.LogInformation("Admin '{UserName}' logged in successfully", userName);
        return AdminLoginResult.Success();
    }

    public async Task LogoutAsync()
    {
        _logger.LogInformation("Admin logout requested");
        await _signInService.SignOutAsync();
        _logger.LogInformation("Admin logged out successfully");
    }
}

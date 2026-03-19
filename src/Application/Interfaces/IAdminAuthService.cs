using Application.UseCases.AdminAuth;

namespace Application.Interfaces;

public interface IAdminAuthService
{
    Task<AdminLoginResult> LoginAsync(string userName, string password);

    Task LogoutAsync();
}

using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AuthUserRepository : IAuthUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthUserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<bool> IsActiveAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is not null && user.AccountStatus == UserStatus.Active;
    }

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(user, password);
    }
}

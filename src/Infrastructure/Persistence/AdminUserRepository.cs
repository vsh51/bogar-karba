using Application.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> UserExistsAsync(string userName)
    {
        var identityUser = await _userManager.FindByNameAsync(userName);
        return identityUser is not null;
    }

    public async Task<bool> CheckPasswordAsync(string userName, string password)
    {
        var identityUser = await _userManager.FindByNameAsync(userName);
        if (identityUser is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(identityUser, password);
    }

    public async Task<IList<string>> GetRolesAsync(string userName)
    {
        var identityUser = await _userManager.FindByNameAsync(userName);
        if (identityUser is null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(identityUser);
    }
}

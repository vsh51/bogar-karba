using Application.Interfaces;
using Domain.Entities;
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

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        var identityUser = await _userManager.FindByNameAsync(userName);
        return identityUser?.ToDomainUser();
    }

    public async Task<bool> CheckPasswordAsync(User user, string password)
    {
        var identityUser = await _userManager.FindByIdAsync(user.Id);
        if (identityUser is null)
        {
            return false;
        }

        return await _userManager.CheckPasswordAsync(identityUser, password);
    }

    public async Task<IList<string>> GetRolesAsync(User user)
    {
        var identityUser = await _userManager.FindByIdAsync(user.Id);
        if (identityUser is null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(identityUser);
    }
}

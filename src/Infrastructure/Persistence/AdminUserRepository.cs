using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUser?> GetByUserNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName);
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }
}

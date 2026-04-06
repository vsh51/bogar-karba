using Application.Enums;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserRepository(UserManager<ApplicationUser> userManager) : IUserRepository
{
    public async Task<bool> UserExistsAsync(string identifier, UserLookupMode lookupMode)
    {
        return await FindUserAsync(identifier, lookupMode) is not null;
    }

    public async Task<bool> IsActiveAsync(string identifier, UserLookupMode lookupMode)
    {
        var user = await FindUserAsync(identifier, lookupMode);
        return user is not null && user.AccountStatus == UserStatus.Active;
    }

    public async Task<bool> CheckPasswordAsync(string identifier, string password, UserLookupMode lookupMode)
    {
        var user = await FindUserAsync(identifier, lookupMode);
        if (user is null)
        {
            return false;
        }

        return await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IList<string>> GetRolesAsync(string identifier, UserLookupMode lookupMode)
    {
        var user = await FindUserAsync(identifier, lookupMode);
        if (user is null)
        {
            return new List<string>();
        }

        return await userManager.GetRolesAsync(user);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(
        string name, string surname, string email, string password, UserStatus accountStatus)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = name,
            Surname = surname,
            AccountStatus = accountStatus,
        };

        var result = await userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<bool> BanUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        user.AccountStatus = UserStatus.Banned;
        var result = await userManager.UpdateSecurityStampAsync(user);
        return result.Succeeded;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await userManager.Users.CountAsync();
    }

    public async Task<Dictionary<string, string>> GetUsernamesByIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        return await userManager.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Id);
    }

    private async Task<ApplicationUser?> FindUserAsync(string identifier, UserLookupMode lookupMode)
    {
        return lookupMode switch
        {
            UserLookupMode.ByEmail => await userManager.FindByEmailAsync(identifier),
            UserLookupMode.ByUserName => await userManager.FindByNameAsync(identifier),
            _ => null,
        };
    }
}

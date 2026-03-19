using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class RegistrationUserRepository : IRegistrationUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegistrationUserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email) is not null;
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

        var result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }
}

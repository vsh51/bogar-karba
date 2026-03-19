using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AdminSignInService : IAdminSignInService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminSignInService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task SignInAsync(User user)
    {
        var identityUser = await _userManager.FindByIdAsync(user.Id);
        if (identityUser is not null)
        {
            await _signInManager.SignInAsync(identityUser, isPersistent: false);
        }
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

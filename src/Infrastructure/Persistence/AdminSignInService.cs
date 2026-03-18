using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AdminSignInService : IAdminSignInService
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AdminSignInService(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task SignInAsync(ApplicationUser user)
    {
        await _signInManager.SignInAsync(user, isPersistent: false);
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

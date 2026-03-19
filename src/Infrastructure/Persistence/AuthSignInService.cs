using Application.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AuthSignInService : IAuthSignInService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthSignInService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task SignInAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
        }
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}

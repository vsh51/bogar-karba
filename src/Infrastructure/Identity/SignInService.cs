using Application.Enums;
using Application.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class SignInService : ISignInService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public SignInService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task SignInAsync(string identifier, UserLookupMode lookupMode)
    {
        var user = lookupMode switch
        {
            UserLookupMode.ByEmail => await _userManager.FindByEmailAsync(identifier),
            UserLookupMode.ByUserName => await _userManager.FindByNameAsync(identifier),
            _ => null,
        };

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

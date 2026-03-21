using Application.Enums;

namespace Application.Interfaces;

public interface ISignInService
{
    Task SignInAsync(string identifier, UserLookupMode lookupMode);

    Task SignOutAsync();
}

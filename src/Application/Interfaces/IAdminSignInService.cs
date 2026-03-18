using Domain.Entities;

namespace Application.Interfaces;

public interface IAdminSignInService
{
    Task SignInAsync(ApplicationUser user);

    Task SignOutAsync();
}

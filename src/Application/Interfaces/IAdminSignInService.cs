using Domain.Entities;

namespace Application.Interfaces;

public interface IAdminSignInService
{
    Task SignInAsync(User user);

    Task SignOutAsync();
}

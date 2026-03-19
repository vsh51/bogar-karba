namespace Application.Interfaces;

public interface IAdminSignInService
{
    Task SignInAsync(string userName);

    Task SignOutAsync();
}

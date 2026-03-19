namespace Application.Interfaces;

public interface IAuthSignInService
{
    Task SignInAsync(string email);

    Task SignOutAsync();
}

namespace Application.Interfaces;

public interface IAuthUserRepository
{
    Task<bool> UserExistsAsync(string email);

    Task<bool> IsActiveAsync(string email);

    Task<bool> CheckPasswordAsync(string email, string password);
}

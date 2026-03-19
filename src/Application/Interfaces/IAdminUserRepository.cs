namespace Application.Interfaces;

public interface IAdminUserRepository
{
    Task<bool> UserExistsAsync(string userName);

    Task<bool> CheckPasswordAsync(string userName, string password);

    Task<IList<string>> GetRolesAsync(string userName);
}

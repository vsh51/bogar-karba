using Domain.Entities;

namespace Application.Interfaces;

public interface IAdminUserRepository
{
    Task<User?> GetByUserNameAsync(string userName);

    Task<bool> CheckPasswordAsync(User user, string password);

    Task<IList<string>> GetRolesAsync(User user);
}

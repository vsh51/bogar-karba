using Domain.Entities;

namespace Application.Interfaces;

public interface IAdminUserRepository
{
    Task<ApplicationUser?> GetByUserNameAsync(string userName);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    Task<IList<string>> GetRolesAsync(ApplicationUser user);
}

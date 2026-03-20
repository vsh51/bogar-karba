using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<bool> UserExistsAsync(string identifier, UserLookupMode lookupMode);

    Task<bool> CheckPasswordAsync(string identifier, string password, UserLookupMode lookupMode);

    Task<bool> IsActiveAsync(string identifier, UserLookupMode lookupMode);

    Task<IList<string>> GetRolesAsync(string identifier, UserLookupMode lookupMode);

    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(
        string name, string surname, string email, string password, UserStatus accountStatus);
}

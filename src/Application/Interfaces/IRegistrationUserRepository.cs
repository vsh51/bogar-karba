using Domain.Entities;

namespace Application.Interfaces;

public interface IRegistrationUserRepository
{
    Task<bool> UserExistsAsync(string email);

    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(
        string name, string surname, string email, string password, UserStatus accountStatus);
}

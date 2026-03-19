using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public UserStatus AccountStatus { get; set; } = UserStatus.Active;

    public User ToDomainUser()
    {
        return new User
        {
            Id = Id,
            UserName = UserName ?? string.Empty,
            AccountStatus = AccountStatus,
        };
    }
}

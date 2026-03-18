using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public enum UserStatus
{
    Active,
    Banned,
    Pending,
}

public class ApplicationUser : IdentityUser
{
    public UserStatus AccountStatus { get; set; } = UserStatus.Active;

    public List<Checklist> Checklists { get; set; } = new();
}

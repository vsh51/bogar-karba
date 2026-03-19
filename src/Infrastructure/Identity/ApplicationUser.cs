using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public UserStatus AccountStatus { get; set; } = UserStatus.Active;

    public List<Checklist> Checklists { get; set; } = new();
}

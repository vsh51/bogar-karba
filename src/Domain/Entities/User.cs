namespace Domain.Entities;

public enum UserStatus
{
    Active,
    Banned,
    Pending
}

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus AccountStatus { get; set; } = UserStatus.Active;
    public List<Checklist> Checklists { get; set; } = new();
}
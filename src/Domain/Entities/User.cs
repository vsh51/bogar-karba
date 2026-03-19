namespace Domain.Entities;

public enum UserStatus
{
    Active,
    Banned,
    Pending,
}

public class User
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public UserStatus AccountStatus { get; set; } = UserStatus.Active;

    public List<Checklist> Checklists { get; set; } = new();
}

using Domain.Entities;

namespace Web.Models.Admin;

public sealed class AdminUserViewModel
{
    public string Id { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public UserStatus Status { get; init; }

    public bool IsBanned => Status == UserStatus.Banned;
}

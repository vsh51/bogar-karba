using Domain.Entities;

namespace Application.DTOs.User;

public sealed class UserSummaryDto
{
    public string Id { get; init; } = string.Empty;

    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Surname { get; init; } = string.Empty;

    public UserStatus Status { get; init; }
}

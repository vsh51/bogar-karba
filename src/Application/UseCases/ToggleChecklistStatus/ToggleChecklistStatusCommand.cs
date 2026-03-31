using Domain.Entities;

namespace Application.UseCases.ToggleChecklistStatus;

public sealed record ToggleChecklistStatusCommand(Guid Id, ChecklistStatus NewStatus, string? OwnerId = null);

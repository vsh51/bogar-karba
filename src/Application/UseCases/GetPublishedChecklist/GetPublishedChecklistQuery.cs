namespace Application.UseCases.GetPublishedChecklist;

public sealed record GetPublishedChecklistQuery(Guid Id, string? OwnerId = null);

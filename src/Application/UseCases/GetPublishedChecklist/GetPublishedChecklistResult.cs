using Application.DTOs.Checklist;

namespace Application.UseCases.GetPublishedChecklist;

public sealed class GetPublishedChecklistResult
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<ChecklistSectionDto> Sections { get; init; } = Array.Empty<ChecklistSectionDto>();
}

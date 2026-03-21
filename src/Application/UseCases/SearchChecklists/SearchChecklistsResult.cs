using Application.DTOs.Checklist;

namespace Application.UseCases.SearchChecklists;

public sealed record SearchChecklistsResult(List<ChecklistSummaryDto> Checklists);

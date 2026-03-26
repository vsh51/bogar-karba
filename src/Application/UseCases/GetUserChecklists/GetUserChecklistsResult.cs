using Application.DTOs.Checklist;

namespace Application.UseCases.GetUserChecklists;

public sealed record GetUserChecklistsResult(List<ChecklistSummaryDto> Checklists);

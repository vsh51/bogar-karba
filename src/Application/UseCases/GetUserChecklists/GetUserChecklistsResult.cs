using Application.DTOs.Checklist;

namespace Application.UseCases.GetUserChecklists;

public record GetUserChecklistsResult(
    List<ChecklistSummaryDto> Checklists,
    bool Succeeded = true,
    string? ErrorMessage = null);

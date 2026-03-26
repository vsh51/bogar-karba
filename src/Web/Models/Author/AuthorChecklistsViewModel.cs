using Application.DTOs.Checklist;

namespace Web.Models.Author;

public class AuthorChecklistsViewModel
{
    public List<ChecklistSummaryDto> Checklists { get; set; } = new();
}

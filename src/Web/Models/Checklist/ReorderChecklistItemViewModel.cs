namespace Web.Models.Checklist;

public sealed class ReorderChecklistItemViewModel
{
    public required Guid TaskId { get; set; }

    public required Guid TargetSectionId { get; set; }

    public required int NewPosition { get; set; }
}

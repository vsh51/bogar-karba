namespace Web.Models.Admin;

public sealed class DashboardViewModel
{
    public int TotalChecklists { get; init; }

    public int TotalUsers { get; init; }

    public int PublishedChecklists { get; init; }

    public int DraftChecklists { get; init; }

    public int ArchivedChecklists { get; init; }
}

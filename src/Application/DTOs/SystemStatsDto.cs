namespace Application.DTOs;

public sealed record SystemStatsDto(int TotalChecklists, int TotalUsers, int PublishedChecklists, int DraftChecklists, int ArchivedChecklists);

namespace Application.Common;

public sealed record DeadlineInfo(
    DateOnly Deadline,
    bool IsOutdated,
    string RemainingText);

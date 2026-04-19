namespace Application.Common;

public static class DeadlineValidator
{
    public static string? Validate(DateOnly? deadline, DateOnly today, int maxYears)
    {
        if (deadline is null)
        {
            return null;
        }

        if (deadline < today)
        {
            return ResultErrors.DeadlineInPast;
        }

        if (deadline > today.AddYears(maxYears))
        {
            return ResultErrors.DeadlineTooFar;
        }

        return null;
    }
}

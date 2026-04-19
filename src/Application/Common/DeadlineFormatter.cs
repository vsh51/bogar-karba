using System.Globalization;

namespace Application.Common;

public static class DeadlineFormatter
{
    private const int DaysThreshold = 60;
    private const int MonthsThreshold = 730;

    public static DeadlineInfo? Describe(DateOnly? deadline, DateOnly today)
    {
        if (deadline is null)
        {
            return null;
        }

        var daysDiff = deadline.Value.DayNumber - today.DayNumber;
        var isOutdated = daysDiff < 0;
        var remaining = isOutdated ? "Outdated" : FormatRemaining(daysDiff);

        return new DeadlineInfo(deadline.Value, isOutdated, remaining);
    }

    private static string FormatRemaining(int daysDiff)
    {
        if (daysDiff < DaysThreshold)
        {
            return Pluralize(daysDiff, "day") + " left";
        }

        if (daysDiff < MonthsThreshold)
        {
            var months = daysDiff / 30;
            return Pluralize(months, "month") + " left";
        }

        var years = daysDiff / 365;
        return Pluralize(years, "year") + " left";
    }

    private static string Pluralize(int value, string unit) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{value} {unit}{(value == 1 ? string.Empty : "s")}");
}

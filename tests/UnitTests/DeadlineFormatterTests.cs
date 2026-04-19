using Application.Common;

namespace UnitTests;

public class DeadlineFormatterTests
{
    private static readonly DateOnly Today = new(2026, 4, 19);

    [Fact]
    public void Describe_NullDeadline_ReturnsNull()
    {
        var info = DeadlineFormatter.Describe(null, Today);

        Assert.Null(info);
    }

    [Fact]
    public void Describe_SameDay_NotOutdated_ZeroDaysLeft()
    {
        var info = DeadlineFormatter.Describe(Today, Today);

        Assert.NotNull(info);
        Assert.False(info!.IsOutdated);
        Assert.Equal("0 days left", info.RemainingText);
    }

    [Fact]
    public void Describe_OneDayAhead_SingularDay()
    {
        var info = DeadlineFormatter.Describe(Today.AddDays(1), Today);

        Assert.NotNull(info);
        Assert.Equal("1 day left", info!.RemainingText);
    }

    [Fact]
    public void Describe_FortyFiveDaysAhead_ReturnsDays()
    {
        var info = DeadlineFormatter.Describe(Today.AddDays(45), Today);

        Assert.Equal("45 days left", info!.RemainingText);
    }

    [Fact]
    public void Describe_NinetyDaysAhead_ReturnsMonths()
    {
        var info = DeadlineFormatter.Describe(Today.AddDays(90), Today);

        Assert.Equal("3 months left", info!.RemainingText);
    }

    [Fact]
    public void Describe_OneMonthSingular()
    {
        var info = DeadlineFormatter.Describe(Today.AddDays(60), Today);

        Assert.Equal("2 months left", info!.RemainingText);
    }

    [Fact]
    public void Describe_TwoYearsAhead_ReturnsYears()
    {
        var info = DeadlineFormatter.Describe(Today.AddYears(2), Today);

        Assert.EndsWith("years left", info!.RemainingText);
    }

    [Fact]
    public void Describe_PastDate_IsOutdated()
    {
        var info = DeadlineFormatter.Describe(Today.AddDays(-1), Today);

        Assert.NotNull(info);
        Assert.True(info!.IsOutdated);
        Assert.Equal("Outdated", info.RemainingText);
    }
}

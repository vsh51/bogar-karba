using Application.Common;

namespace UnitTests;

public class DeadlineValidatorTests
{
    private const int MaxYears = 3;
    private static readonly DateOnly Today = new(2026, 4, 19);

    [Fact]
    public void Validate_NullDeadline_ReturnsNoError()
    {
        var error = DeadlineValidator.Validate(null, Today, MaxYears);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_DeadlineToday_ReturnsNoError()
    {
        var error = DeadlineValidator.Validate(Today, Today, MaxYears);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_DeadlineInPast_ReturnsPastError()
    {
        var error = DeadlineValidator.Validate(Today.AddDays(-1), Today, MaxYears);

        Assert.Equal(ResultErrors.DeadlineInPast, error);
    }

    [Fact]
    public void Validate_DeadlineAtMaxBoundary_ReturnsNoError()
    {
        var error = DeadlineValidator.Validate(Today.AddYears(MaxYears), Today, MaxYears);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_DeadlineBeyondMax_ReturnsTooFarError()
    {
        var error = DeadlineValidator.Validate(Today.AddYears(MaxYears).AddDays(1), Today, MaxYears);

        Assert.Equal(ResultErrors.DeadlineTooFar, error);
    }
}

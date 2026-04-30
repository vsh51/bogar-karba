using Application.UseCases.QuickCreateChecklist;

namespace UnitTests;

public class QuickCreateChecklistParserTests
{
    private static readonly string[] FullExampleLines =
    [
        "# My Trip",
        string.Empty,
        "Two-day plan.",
        "Second description line.",
        string.Empty,
        "## Before leaving",
        "- [ ] Buy tickets",
        "- [ ] Charge phone",
        string.Empty,
        "## In the city",
        "- [ ] Visit square",
        "- Try local coffee"
    ];

    private static readonly string[] ExpectedFirstSectionTasks = ["Buy tickets", "Charge phone"];
    private static readonly string[] ExpectedSecondSectionTasks = ["Visit square", "Try local coffee"];

    [Fact]
    public void Parse_FullExample_ProducesExpectedStructure()
    {
        var input = string.Join('\n', FullExampleLines);

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.True(result.Succeeded);
        var parsed = result.Value!;
        Assert.Equal("My Trip", parsed.Title);
        Assert.Equal("Two-day plan.\nSecond description line.", parsed.Description);
        Assert.Equal(2, parsed.Sections.Count);

        Assert.Equal("Before leaving", parsed.Sections[0].Name);
        Assert.Equal(0, parsed.Sections[0].Position);
        Assert.Equal(ExpectedFirstSectionTasks, parsed.Sections[0].Tasks);

        Assert.Equal("In the city", parsed.Sections[1].Name);
        Assert.Equal(1, parsed.Sections[1].Position);
        Assert.Equal(ExpectedSecondSectionTasks, parsed.Sections[1].Tasks);
    }

    [Fact]
    public void Parse_TasksWithoutSection_GoIntoDefaultSection()
    {
        var input = "# Title\n- [ ] Task 1\n- [ ] Task 2";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.True(result.Succeeded);
        var parsed = result.Value!;
        Assert.Single(parsed.Sections);
        Assert.Equal(QuickCreateChecklistParser.DefaultSectionName, parsed.Sections[0].Name);
        Assert.Equal(2, parsed.Sections[0].Tasks.Count);
    }

    [Fact]
    public void Parse_HandlesCarriageReturnsAndCrlfLineEndings()
    {
        var input = "# Title\r\n- [ ] Task 1\r- [ ] Task 2";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Sections[0].Tasks.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n\n   ")]
    public void Parse_EmptyInput_ReturnsEmptyInputError(string? input)
    {
        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Equal(QuickCreateErrors.EmptyInput, result.ErrorMessage);
    }

    [Fact]
    public void Parse_NoTitleLine_ReturnsTitleMustBeFirstError()
    {
        var input = "- [ ] Task without title";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Contains("title", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_TitleEmptyAfterMarker_ReturnsEmptyTitleError()
    {
        var input = "# \n- [ ] Task";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Contains("title cannot be empty", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DuplicateTitle_ReturnsMultipleTitlesError()
    {
        var input = "# First\n# Second";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Contains("only one title", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_EmptySectionName_ReturnsEmptySectionNameError()
    {
        var input = "# Title\n## \n- [ ] Task";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Contains("section name", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_EmptyTaskContent_ReturnsEmptyTaskContentError()
    {
        var input = "# Title\n- [ ] ";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Contains("task content", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_DescriptionLineAfterTask_ReturnsUnrecognizedLineError()
    {
        var input = "# Title\n- [ ] Task\nstray description";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.False(result.Succeeded);
        Assert.Contains("unrecognized format", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_OnlyTitleNoTasks_ReturnsSuccessWithEmptySections()
    {
        var input = "# Title";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Sections);
    }

    [Fact]
    public void Parse_TaskWithoutBrackets_IsAccepted()
    {
        var input = "# Title\n- Plain task";

        var result = QuickCreateChecklistParser.Parse(input);

        Assert.True(result.Succeeded);
        Assert.Equal("Plain task", result.Value!.Sections[0].Tasks[0]);
    }

    [Theory]
    [InlineData("- [ ] Pending")]
    [InlineData("- Plain task")]
    public void Parse_UncheckedMarkers_AreAccepted(string line)
    {
        var result = QuickCreateChecklistParser.Parse("# Title\n" + line);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Sections[0].Tasks);
    }

    [Theory]
    [InlineData("- [x] Done")]
    [InlineData("- [X] Done")]
    [InlineData("- [+] Done")]
    public void Parse_CheckedMarkers_AreRejected(string line)
    {
        var result = QuickCreateChecklistParser.Parse("# Title\n" + line);

        Assert.False(result.Succeeded);
        Assert.Contains("unchecked", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }
}

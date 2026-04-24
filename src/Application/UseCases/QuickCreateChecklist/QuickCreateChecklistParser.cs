using System.Globalization;
using Application.Common;

namespace Application.UseCases.QuickCreateChecklist;

public static class QuickCreateChecklistParser
{
    public const string DefaultSectionName = "General";

    public static Result<ParsedQuickChecklist> Parse(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return QuickCreateErrors.EmptyInput;
        }

        var lines = rawText.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        string? title = null;
        var descriptionLines = new List<string>();
        var sections = new List<ParsedQuickSection>();
        ParsedQuickSection? currentSection = null;
        var seenTaskOrSection = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.Trim();
            var lineNumber = i + 1;

            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '#')
            {
                var hashes = 0;
                while (hashes < line.Length && line[hashes] == '#')
                {
                    hashes++;
                }

                if (hashes > 2)
                {
                    return FormatError(QuickCreateErrors.UnrecognizedLine, lineNumber);
                }

                var rest = line[hashes..].Trim();

                if (hashes == 1)
                {
                    if (title is not null)
                    {
                        return FormatError(QuickCreateErrors.MultipleTitles, lineNumber);
                    }

                    if (rest.Length == 0)
                    {
                        return FormatError(QuickCreateErrors.EmptyTitle, lineNumber);
                    }

                    title = rest;
                    continue;
                }

                if (title is null)
                {
                    return FormatError(QuickCreateErrors.TitleMustBeFirst, lineNumber);
                }

                if (rest.Length == 0)
                {
                    return FormatError(QuickCreateErrors.EmptySectionName, lineNumber);
                }

                currentSection = new ParsedQuickSection(rest, sections.Count, new List<string>());
                sections.Add(currentSection);
                seenTaskOrSection = true;
                continue;
            }

            if (title is null)
            {
                return FormatError(QuickCreateErrors.TitleMustBeFirst, lineNumber);
            }

            if (TryParseTaskLine(line, out var taskContent, out var checkedMarker))
            {
                if (checkedMarker)
                {
                    return FormatError(QuickCreateErrors.CheckedItemNotAllowed, lineNumber);
                }

                if (taskContent.Length == 0)
                {
                    return FormatError(QuickCreateErrors.EmptyTaskContent, lineNumber);
                }

                if (currentSection is null)
                {
                    currentSection = new ParsedQuickSection(DefaultSectionName, 0, new List<string>());
                    sections.Add(currentSection);
                }

                currentSection.Tasks.Add(taskContent);
                seenTaskOrSection = true;
                continue;
            }

            if (!seenTaskOrSection)
            {
                descriptionLines.Add(line);
                continue;
            }

            return FormatError(QuickCreateErrors.UnrecognizedLine, lineNumber);
        }

        if (title is null)
        {
            return QuickCreateErrors.TitleRequired;
        }

        var description = string.Join('\n', descriptionLines).Trim();
        return new ParsedQuickChecklist(title, description, sections);
    }

    private static bool TryParseTaskLine(string line, out string content, out bool checkedMarker)
    {
        content = string.Empty;
        checkedMarker = false;

        if (!line.StartsWith("- ", StringComparison.Ordinal))
        {
            return false;
        }

        var rest = line[2..].TrimStart();

        if (rest.Length >= 3 && rest[0] == '[' && rest[2] == ']'
            && (rest[1] == ' ' || rest[1] == '+' || rest[1] == 'x' || rest[1] == 'X'))
        {
            checkedMarker = rest[1] != ' ';
            content = rest[3..].Trim();
            return true;
        }

        content = rest.Trim();
        return true;
    }

    private static string FormatError(string template, int lineNumber)
        => string.Format(CultureInfo.InvariantCulture, template, lineNumber);
}

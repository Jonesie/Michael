using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Michael.Analysis.Models;
using Michael.Fixes.Abstractions;

namespace Michael.Fixes;

public sealed partial class TemplateFixScriptGenerator : IFixScriptGenerator
{
    private const int MaxSamplesPerIssue = 2;
    private const int ContextLineRadius = 2;
    private const string DefaultScriptTemplate = """
# Michael generated fix script
# Rank: [[rank]]
# Target files: [[targetFileCount]]

param(
    [string]$RepoPath = (Get-Location).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Prompt = @'
You are generating a safe, minimal code fix suggestion for a .NET build issue.

Issue details:
[[issueDetails]]

Files that need fixing (apply to all listed files if confirmed):
[[fileList]]

Required interaction flow:
1) First describe the proposed fix and show the planned edits/diff.
2) Ask for confirmation before making edits.
3) Apply the fix across all listed target files only if the user confirms.
4) If the user declines, do not modify files.

Sample file lines (with line numbers):
[[samples]]

Include:
- root-cause explanation,
- exact file edits (unified diff preferred),
- validation steps (dotnet build/test commands).
'@

Push-Location $RepoPath
try {
    copilot -i "agent --prompt $Prompt"
}
finally {
    Pop-Location
}
""";

    public IReadOnlyDictionary<int, string> Generate(
        string outputDirectory,
        IReadOnlyList<RankedIssue> rankedIssues,
        string? scriptTemplateText = null,
        string? scriptFileExtension = null)
    {
        Directory.CreateDirectory(outputDirectory);

        var resolvedScriptTemplate = string.IsNullOrWhiteSpace(scriptTemplateText)
            ? DefaultScriptTemplate
            : scriptTemplateText;
        var resolvedScriptExtension = NormalizeScriptFileExtension(scriptFileExtension);

        var fileNamesByRank = new Dictionary<int, string>();

        foreach (var issue in rankedIssues.OrderBy(entry => entry.Rank))
        {
            var fileName = $"fix-rank-{issue.Rank}{resolvedScriptExtension}";
            var fullPath = Path.Combine(outputDirectory, fileName);
            var targetLocations = BuildTargetLocations(issue.Files);
            var issueDetails = BuildIssueDetails(issue);
            var fileList = BuildFileList(targetLocations);
            var samples = BuildSamplesSection(issue.Files);
            var script = BuildScript(
                issue.Rank,
                targetLocations.Count,
                issueDetails,
                fileList,
                samples,
                resolvedScriptTemplate);

            File.WriteAllText(fullPath, script);
            fileNamesByRank[issue.Rank] = fileName;
        }

        return fileNamesByRank;
    }

    private static string NormalizeScriptFileExtension(string? scriptFileExtension)
    {
        if (string.IsNullOrWhiteSpace(scriptFileExtension))
        {
            return ".ps1";
        }

        var trimmed = scriptFileExtension.Trim();
        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : $".{trimmed}";
    }

    private static string BuildScript(
        int rank,
        int targetFileCount,
        string issueDetails,
        string fileList,
        string samples,
        string scriptTemplate)
    {
        var script = scriptTemplate;
        script = ReplaceToken(script, "rank", rank.ToString(CultureInfo.InvariantCulture));
        script = ReplaceToken(script, "targetFileCount", targetFileCount.ToString(CultureInfo.InvariantCulture));
        script = ReplaceToken(script, "issueDetails", issueDetails);
        script = ReplaceToken(script, "fileList", fileList);
        script = ReplaceToken(script, "samples", samples);

        EnsureNoUnresolvedTokens(script, rank);

        return script;
    }

    private static string ReplaceToken(string template, string tokenName, string value)
    {
        var squarePattern = $@"\[\s*\[\s*{Regex.Escape(tokenName)}\s*\]\s*\]";
        return Regex.Replace(template, squarePattern, value, RegexOptions.CultureInvariant);
    }

    private static void EnsureNoUnresolvedTokens(string script, int rank)
    {
        var matches = UnresolvedTokenRegex().Matches(script);
        if (matches.Count == 0)
        {
            return;
        }

        var unresolvedTokens = matches
            .Select(match => match.Groups["tokenSquare"].Value)
            .Select(token => Regex.Replace(token, @"\s+", " ").Trim())
            .Where(token => token.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(token => token, StringComparer.Ordinal)
            .Select(token => $"[[{token}]]")
            .ToArray();

        throw new InvalidOperationException(
            $"Unresolved template placeholders for rank {rank}: {string.Join(", ", unresolvedTokens)}");
    }

    private static string BuildIssueDetails(RankedIssue issue)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"- Rank: {issue.Rank}");
        builder.AppendLine($"- Severity: {issue.Severity}");
        builder.AppendLine($"- Frequency: {issue.Frequency}");
        builder.AppendLine($"- Error: {issue.Message}");

        return builder.ToString().TrimEnd();
    }

    private static string BuildFileList(IReadOnlyList<string> targetLocations)
    {
        var builder = new StringBuilder();

        if (targetLocations.Count == 0)
        {
            builder.Append("- No concrete target files were identified.");
            return builder.ToString();
        }

        foreach (var location in targetLocations)
        {
            builder.AppendLine($"- {location}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildSamplesSection(IReadOnlyList<string> locations)
    {
        var builder = new StringBuilder();
        var samples = BuildSamples(locations);

        if (samples.Count == 0)
        {
            builder.Append("- No source file sample was available.");
            return builder.ToString();
        }

        foreach (var sample in samples)
        {
            builder.AppendLine($"- File: {sample.FilePath}");
            foreach (var line in sample.Lines)
            {
                builder.AppendLine($"  {line}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static IReadOnlyList<FileSample> BuildSamples(IReadOnlyList<string> locations)
    {
        var samples = new List<FileSample>();

        foreach (var location in locations)
        {
            if (samples.Count >= MaxSamplesPerIssue)
            {
                break;
            }

            var (path, lineNumber) = SplitLocation(location);
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !File.Exists(path))
            {
                continue;
            }

            var sampleLines = ReadSampleLines(path, lineNumber);
            if (sampleLines.Count == 0)
            {
                continue;
            }

            samples.Add(new FileSample(path, sampleLines));
        }

        return samples;
    }

    private static IReadOnlyList<string> BuildTargetLocations(IReadOnlyList<string> locations)
    {
        return locations
            .Where(location => !string.IsNullOrWhiteSpace(location) && location != "(no-file)")
            .Select(location => location.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(location => location, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> ReadSampleLines(string filePath, int? preferredLine)
    {
        string[] allLines;

        try
        {
            allLines = File.ReadAllLines(filePath);
        }
        catch
        {
            return Array.Empty<string>();
        }

        if (allLines.Length == 0)
        {
            return Array.Empty<string>();
        }

        var start = preferredLine.HasValue
            ? Math.Max(1, preferredLine.Value - ContextLineRadius)
            : 1;

        var end = preferredLine.HasValue
            ? Math.Min(allLines.Length, preferredLine.Value + ContextLineRadius)
            : Math.Min(allLines.Length, 3);

        var lines = new List<string>();
        for (var index = start; index <= end; index++)
        {
            lines.Add($"L{index}: {allLines[index - 1]}");
        }

        return lines;
    }

    private static (string Path, int? Line) SplitLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location) || location == "(no-file)")
        {
            return (string.Empty, null);
        }

        var match = FileLocationWithLineRegex().Match(location.Trim());
        if (!match.Success)
        {
            return (location.Trim(), null);
        }

        var path = match.Groups["path"].Value.Trim();
        var line = int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture);

        return (path, line);
    }

    [GeneratedRegex(@"^(?<path>.+)\((?<line>\d+)(,(?<column>\d+))?\)$", RegexOptions.CultureInvariant)]
    private static partial Regex FileLocationWithLineRegex();

    [GeneratedRegex(@"\[\s*\[\s*(?<tokenSquare>.*?)\s*\]\s*\]", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex UnresolvedTokenRegex();

    private sealed record FileSample(string FilePath, IReadOnlyList<string> Lines);
}

using System.Text;
using System.Text.RegularExpressions;
using Michael.Analysis.Models;
using Michael.Fixes.Abstractions;

namespace Michael.Fixes;

public sealed partial class CopilotCliFixScriptGenerator : IFixScriptGenerator
{
    private const int MaxSamplesPerIssue = 2;
    private const int ContextLineRadius = 2;

    public IReadOnlyDictionary<int, string> Generate(string outputDirectory, IReadOnlyList<RankedIssue> rankedIssues)
    {
        Directory.CreateDirectory(outputDirectory);

        var fileNamesByRank = new Dictionary<int, string>();

        foreach (var issue in rankedIssues.OrderBy(entry => entry.Rank))
        {
            var fileName = $"fix-rank-{issue.Rank}.ps1";
            var fullPath = Path.Combine(outputDirectory, fileName);
            var prompt = BuildPrompt(issue);
            var targetLocations = BuildTargetLocations(issue.Files);
            var script = BuildScript(issue.Rank, prompt, targetLocations);

            File.WriteAllText(fullPath, script);
            fileNamesByRank[issue.Rank] = fileName;
        }

        return fileNamesByRank;
    }

    private static string BuildScript(int rank, string prompt, IReadOnlyList<string> targetLocations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Michael generated fix script");
        builder.AppendLine($"# Rank: {rank}");
        builder.AppendLine($"# Target files: {targetLocations.Count}");
        builder.AppendLine();
        builder.AppendLine("param(");
        builder.AppendLine("    [string]$RepoPath = (Get-Location).Path");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine("Set-StrictMode -Version Latest");
        builder.AppendLine("$ErrorActionPreference = 'Stop'");
        builder.AppendLine();
        builder.AppendLine("$Prompt = @'");
        builder.AppendLine(prompt);
        builder.AppendLine("'@");
        builder.AppendLine();
        builder.AppendLine("Push-Location $RepoPath");
        builder.AppendLine("try {");
        builder.AppendLine("    copilot -i \"agent --prompt $Prompt\"");
        builder.AppendLine("}");
        builder.AppendLine("finally {");
        builder.AppendLine("    Pop-Location");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string BuildPrompt(RankedIssue issue)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are generating a safe, minimal code fix suggestion for a .NET build issue.");
        builder.AppendLine();
        builder.AppendLine("Issue details:");
        builder.AppendLine($"- Rank: {issue.Rank}");
        builder.AppendLine($"- Severity: {issue.Severity}");
        builder.AppendLine($"- Frequency: {issue.Frequency}");
        builder.AppendLine($"- Error: {issue.Message}");
        builder.AppendLine();
        builder.AppendLine("Files that need fixing (apply to all listed files if confirmed):");

        var targetLocations = BuildTargetLocations(issue.Files);
        if (targetLocations.Count == 0)
        {
            builder.AppendLine("- No concrete target files were identified.");
        }
        else
        {
            foreach (var location in targetLocations)
            {
                builder.AppendLine($"- {location}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Required interaction flow:");
        builder.AppendLine("1) First describe the proposed fix and show the planned edits/diff.");
        builder.AppendLine("2) Ask for confirmation before making edits.");
        builder.AppendLine("3) Apply the fix across all listed target files only if the user confirms.");
        builder.AppendLine("4) If the user declines, do not modify files.");
        builder.AppendLine();
        builder.AppendLine("Sample file lines (with line numbers):");

        var samples = BuildSamples(issue.Files);
        if (samples.Count == 0)
        {
            builder.AppendLine("- No source file sample was available.");
        }
        else
        {
            foreach (var sample in samples)
            {
                builder.AppendLine($"- File: {sample.FilePath}");
                foreach (var line in sample.Lines)
                {
                    builder.AppendLine($"  {line}");
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("Include:");
        builder.AppendLine("- root-cause explanation,");
        builder.AppendLine("- exact file edits (unified diff preferred),");
        builder.AppendLine("- validation steps (dotnet build/test commands).");

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
        var line = int.Parse(match.Groups["line"].Value);

        return (path, line);
    }

    [GeneratedRegex(@"^(?<path>.+)\((?<line>\d+)(,(?<column>\d+))?\)$", RegexOptions.CultureInvariant)]
    private static partial Regex FileLocationWithLineRegex();

    private sealed record FileSample(string FilePath, IReadOnlyList<string> Lines);
}

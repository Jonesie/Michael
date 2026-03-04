using System.Text;
using System.Text.Json;
using Michael.Analysis.Models;
using Michael.Cli.Abstractions;
using Michael.Cli.Models;

namespace Michael.Cli;

public sealed class FileReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void Write(string outputDirectory, ReportMetadata metadata, IReadOnlyList<RankedIssue> rankedIssues)
    {
        Directory.CreateDirectory(outputDirectory);

        var jsonPath = Path.Combine(outputDirectory, "issues.json");
        var markdownPath = Path.Combine(outputDirectory, "summary.md");

        var payload = new
        {
            metadata,
            issues = rankedIssues
        };

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, JsonOptions));
        File.WriteAllText(markdownPath, BuildMarkdown(metadata, rankedIssues));
    }

    private static string BuildMarkdown(ReportMetadata metadata, IReadOnlyList<RankedIssue> rankedIssues)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Michael Analysis Summary");
        builder.AppendLine();
        builder.AppendLine("## Metadata");
        builder.AppendLine();
        builder.AppendLine($"- Generated (UTC): {metadata.GeneratedAtUtc:O}");
        builder.AppendLine($"- Version: {metadata.Version}");
        builder.AppendLine($"- Input: {metadata.InputSource}");
        builder.AppendLine($"- Output: {metadata.OutputDirectory}");
        builder.AppendLine($"- Analyse only: {metadata.AnalyseOnly}");
        builder.AppendLine($"- Limit: {(metadata.Limit.HasValue ? metadata.Limit.Value : "none")}");
        builder.AppendLine($"- Git branch: {metadata.GitBranch ?? "(not set)"}");
        builder.AppendLine($"- AI tool: {metadata.AiTool ?? "(not set)"}");
        builder.AppendLine($"- AI model: {metadata.AiModel ?? "(not set)"}");
        builder.AppendLine($"- Parsed issues: {metadata.ParsedIssueCount}");
        builder.AppendLine($"- Summaries: {metadata.SummaryCount}");
        builder.AppendLine($"- Ranked issues: {metadata.RankedCount}");
        builder.AppendLine();
        builder.AppendLine("## Ranked Issues");
        builder.AppendLine();

        if (rankedIssues.Count == 0)
        {
            builder.AppendLine("No issues were identified in the input log.");
            return builder.ToString();
        }

        builder.AppendLine("| Rank | Severity | Frequency | Confidence | Score | Key |");
        builder.AppendLine("|---:|---|---:|---:|---:|---|");

        foreach (var issue in rankedIssues)
        {
            builder.AppendLine($"| {issue.Rank} | {issue.Severity} | {issue.Frequency} | {issue.Confidence:F2} | {issue.Score:F2} | {EscapePipe(issue.Key)} |");
            builder.AppendLine($"  - {issue.Explanation}");
        }

        return builder.ToString();
    }

    private static string EscapePipe(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
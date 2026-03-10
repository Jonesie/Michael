using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Michael.Analysis.Models;
using Michael.Cli.Abstractions;
using Michael.Cli.Models;
using Michael.Cli.Serialization;

namespace Michael.Cli;

public sealed class FileReportWriter : IReportWriter
{
    private static readonly Regex FileLocationWithLineRegex = new(
        @"^(?<path>.+)\((?<line>\d+)(,(?<column>\d+))?\)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public void Write(
        string outputDirectory,
        ReportMetadata metadata,
        IReadOnlyList<RankedIssue> rankedIssues,
        IReadOnlyDictionary<int, string>? fixScriptFileNamesByRank = null)
    {
        Directory.CreateDirectory(outputDirectory);

        var jsonPath = Path.Combine(outputDirectory, "issues.json");
        var markdownPath = Path.Combine(outputDirectory, "summary.md");
        var htmlPath = Path.Combine(outputDirectory, "summary.html");

        var payload = new IssuesReportPayload(metadata, rankedIssues);

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(payload, MichaelCliJsonContext.Default.IssuesReportPayload));
        File.WriteAllText(markdownPath, BuildMarkdown(metadata, rankedIssues, fixScriptFileNamesByRank));
        File.WriteAllText(htmlPath, BuildHtml(metadata, rankedIssues, fixScriptFileNamesByRank));
    }

    private static string BuildMarkdown(
        ReportMetadata metadata,
        IReadOnlyList<RankedIssue> rankedIssues,
        IReadOnlyDictionary<int, string>? fixScriptFileNamesByRank)
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
        var detectedToolsText = BuildDetectedToolsText(metadata.DetectedTools);
        builder.AppendLine($"- Detected tools/frameworks: {detectedToolsText}");
        builder.AppendLine($"- Parsed issues: {metadata.ParsedIssueCount}");
        builder.AppendLine($"- Summaries: {metadata.SummaryCount}");
        builder.AppendLine($"- Ranked issues: {metadata.RankedCount}");
        if (!string.IsNullOrWhiteSpace(metadata.FixesZipFile))
        {
            builder.AppendLine($"- Fixes archive: {BuildFixesZipLinkMarkdown(metadata.OutputDirectory, metadata.FixesZipFile)}");
        }
        builder.AppendLine();
        builder.AppendLine("## Ranked Issues");
        builder.AppendLine();

        if (rankedIssues.Count == 0)
        {
            builder.AppendLine("No issues were identified in the input log.");
            return builder.ToString();
        }

        builder.AppendLine("| Rank | Severity | Frequency | Details |");
        builder.AppendLine("|---:|---|---:|---|");

        foreach (var issue in rankedIssues)
        {
            var filesDetails = issue.Files.Count == 0
                ? "<details><summary><strong>Files</strong></summary>(no-file)</details>"
                : BuildFilesDetailsMarkdown(issue.Files);

            var details = $"<strong>Error Message</strong><br/>{EscapePipe(Truncate(issue.Message, 90))}<br/>{filesDetails}";
            if (ShouldIncludeFixDetails(metadata, issue.Rank, fixScriptFileNamesByRank))
            {
                var fixDetails = BuildFixDetailsMarkdown(issue.Rank, metadata.OutputDirectory, fixScriptFileNamesByRank);
                details += $"<br/>{fixDetails}";
            }

            builder.AppendLine($"| {issue.Rank} | {issue.Severity} | {issue.Frequency} | {details} |");
        }

        return builder.ToString();
    }

    private static string EscapePipe(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 1)] + "…";
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "(no-file)";
        }

        return path.Replace('\\', '/');
    }

    private static string BuildFilesDetailsMarkdown(IReadOnlyList<string> files)
    {
        var normalized = files
            .Select(NormalizePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var content = string.Join("<br/>", normalized.Select(BuildFileLinkMarkdown));
        return $"<details><summary><strong>Files</strong> ({normalized.Length})</summary>{content}</details>";
    }

    private static string BuildFilesDetailsHtml(IReadOnlyList<string> files)
    {
        var normalized = files
            .Select(NormalizePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var builder = new StringBuilder();
        builder.Append($"<details><summary><strong>Files</strong> ({normalized.Length})</summary><ul>");

        foreach (var file in normalized)
        {
            builder.Append($"<li>{BuildFileLinkHtml(file)}</li>");
        }

        builder.Append("</ul></details>");
        return builder.ToString();
    }

    private static string BuildHtml(
        ReportMetadata metadata,
        IReadOnlyList<RankedIssue> rankedIssues,
        IReadOnlyDictionary<int, string>? fixScriptFileNamesByRank)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine("  <title>Michael Analysis Summary</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: Arial, Helvetica, sans-serif; margin: 24px; color: #1f2937; }");
        builder.AppendLine("    h1, h2 { margin-bottom: 8px; }");
        builder.AppendLine("    ul { margin-top: 0; }");
        builder.AppendLine("    .table-wrap { overflow-x: auto; }");
        builder.AppendLine("    table { border-collapse: collapse; width: 100%; min-width: 900px; }");
        builder.AppendLine("    th, td { border: 1px solid #d1d5db; padding: 8px; vertical-align: top; text-align: left; }");
        builder.AppendLine("    th { background: #f3f4f6; }");
        builder.AppendLine("    td.num { text-align: right; white-space: nowrap; }");
        builder.AppendLine("    .files { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 12px; }");
        builder.AppendLine("    details > summary { cursor: pointer; } ");
        builder.AppendLine("    details ul { margin-top: 8px; padding-left: 20px; } ");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>Michael Analysis Summary</h1>");
        builder.AppendLine("  <h2>Metadata</h2>");
        builder.AppendLine("  <ul>");
        builder.AppendLine($"    <li><strong>Generated (UTC):</strong> {HtmlEncode(metadata.GeneratedAtUtc.ToString("O"))}</li>");
        builder.AppendLine($"    <li><strong>Version:</strong> {HtmlEncode(metadata.Version)}</li>");
        builder.AppendLine($"    <li><strong>Input:</strong> {HtmlEncode(metadata.InputSource)}</li>");
        builder.AppendLine($"    <li><strong>Output:</strong> {HtmlEncode(metadata.OutputDirectory)}</li>");
        builder.AppendLine($"    <li><strong>Analyse only:</strong> {metadata.AnalyseOnly}</li>");
        builder.AppendLine($"    <li><strong>Limit:</strong> {(metadata.Limit.HasValue ? metadata.Limit.Value : "none")}</li>");
        builder.AppendLine($"    <li><strong>Detected tools/frameworks:</strong> {HtmlEncode(BuildDetectedToolsText(metadata.DetectedTools))}</li>");
        builder.AppendLine($"    <li><strong>Parsed issues:</strong> {metadata.ParsedIssueCount}</li>");
        builder.AppendLine($"    <li><strong>Summaries:</strong> {metadata.SummaryCount}</li>");
        builder.AppendLine($"    <li><strong>Ranked issues:</strong> {metadata.RankedCount}</li>");
        if (!string.IsNullOrWhiteSpace(metadata.FixesZipFile))
        {
            builder.AppendLine($"    <li><strong>Fixes archive:</strong> {BuildFixesZipLinkHtml(metadata.OutputDirectory, metadata.FixesZipFile)}</li>");
        }
        builder.AppendLine("  </ul>");

        builder.AppendLine("  <h2>Ranked Issues</h2>");
        if (rankedIssues.Count == 0)
        {
            builder.AppendLine("  <p>No issues were identified in the input log.</p>");
        }
        else
        {
            builder.AppendLine("  <div class=\"table-wrap\">");
            builder.AppendLine("    <table>");
            builder.AppendLine("      <thead>");
            builder.AppendLine("        <tr><th>Rank</th><th>Severity</th><th>Frequency</th><th>Details</th></tr>");
            builder.AppendLine("      </thead>");
            builder.AppendLine("      <tbody>");

            foreach (var issue in rankedIssues)
            {
                var filesDetails = issue.Files.Count == 0
                    ? "<details><summary><strong>Files</strong></summary><div>(no-file)</div></details>"
                    : BuildFilesDetailsHtml(issue.Files);

                var details = $"<strong>Error Message</strong><br/>{HtmlEncode(Truncate(issue.Message, 140))}<br/>{filesDetails}";
                if (ShouldIncludeFixDetails(metadata, issue.Rank, fixScriptFileNamesByRank))
                {
                    var fixDetails = BuildFixDetailsHtml(issue.Rank, metadata.OutputDirectory, fixScriptFileNamesByRank);
                    details += $"<br/>{fixDetails}";
                }

                builder.AppendLine("        <tr>");
                builder.AppendLine($"          <td class=\"num\">{issue.Rank}</td>");
                builder.AppendLine($"          <td>{HtmlEncode(issue.Severity)}</td>");
                builder.AppendLine($"          <td class=\"num\">{issue.Frequency}</td>");
                builder.AppendLine($"          <td class=\"files\">{details}</td>");
                builder.AppendLine("        </tr>");
            }

            builder.AppendLine("      </tbody>");
            builder.AppendLine("    </table>");
            builder.AppendLine("  </div>");
        }

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string HtmlEncode(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }

    private static string BuildFileLinkMarkdown(string path)
    {
        var uri = BuildFileUri(path);
        var text = HtmlEncode(path);

        if (string.IsNullOrEmpty(uri))
        {
            return text;
        }

        return $"<a href=\"{HtmlEncode(uri)}\" target=\"_blank\" rel=\"noopener noreferrer\">{text}</a>";
    }

    private static string BuildFileLinkHtml(string path)
    {
        var uri = BuildFileUri(path);
        var text = HtmlEncode(path);

        if (string.IsNullOrEmpty(uri))
        {
            return text;
        }

        return $"<a href=\"{HtmlEncode(uri)}\" target=\"_blank\" rel=\"noopener noreferrer\">{text}</a>";
    }

    private static string BuildFileUri(string path)
    {
        try
        {
            var normalized = NormalizePath(path);
            var (filePath, line, column) = SplitLocation(normalized);

            if (!Path.IsPathRooted(filePath))
            {
                return string.Empty;
            }

            var encodedPath = string.Join(
                "/",
                filePath
                    .Split('/')
                    .Select(Uri.EscapeDataString));

            var uri = $"vscode://file/{encodedPath}";
            if (line.HasValue)
            {
                uri += $":{line.Value}";
                if (column.HasValue)
                {
                    uri += $":{column.Value}";
                }
            }

            return uri;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static (string filePath, int? line, int? column) SplitLocation(string location)
    {
        var match = FileLocationWithLineRegex.Match(location);
        if (!match.Success)
        {
            return (location, null, null);
        }

        var filePath = match.Groups["path"].Value;
        var line = int.Parse(match.Groups["line"].Value);
        int? column = match.Groups["column"].Success
            ? int.Parse(match.Groups["column"].Value)
            : null;

        return (filePath, line, column);
    }

    private static string BuildFixDetailsMarkdown(
        int rank,
        string outputDirectory,
        IReadOnlyDictionary<int, string>? fixScriptFileNamesByRank)
    {
        if (fixScriptFileNamesByRank is null || !fixScriptFileNamesByRank.TryGetValue(rank, out var fileName))
        {
            return "<strong>Fix</strong><br/>(not generated)";
        }

        var fullPath = Path.Combine(outputDirectory, fileName);
        var uri = BuildFileUri(fullPath);
        var encodedName = HtmlEncode(fileName);

        if (string.IsNullOrEmpty(uri))
        {
            return $"<strong>Fix</strong><br/>{encodedName}";
        }

        return $"<strong>Fix</strong><br/><a href=\"{HtmlEncode(uri)}\" target=\"_blank\" rel=\"noopener noreferrer\">{encodedName}</a>";
    }

    private static string BuildFixesZipLinkMarkdown(string outputDirectory, string fixesZipFile)
    {
        var fullPath = Path.Combine(outputDirectory, fixesZipFile);
        var uri = BuildFileUri(fullPath);
        var encodedName = HtmlEncode(fixesZipFile);

        if (string.IsNullOrEmpty(uri))
        {
            return encodedName;
        }

        return $"<a href=\"{HtmlEncode(uri)}\" target=\"_blank\" rel=\"noopener noreferrer\">{encodedName}</a>";
    }

    private static string BuildDetectedToolsText(IReadOnlyList<string>? detectedTools)
    {
        if (detectedTools is null || detectedTools.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", detectedTools);
    }

    private static bool ShouldIncludeFixDetails(
        ReportMetadata metadata,
        int rank,
        IReadOnlyDictionary<int, string>? fixScriptFileNamesByRank)
    {
        if (metadata.AnalyseOnly || !string.IsNullOrWhiteSpace(metadata.FixesZipFile))
        {
            return false;
        }

        return fixScriptFileNamesByRank is not null && fixScriptFileNamesByRank.ContainsKey(rank);
    }

    private static string BuildFixDetailsHtml(
        int rank,
        string outputDirectory,
        IReadOnlyDictionary<int, string>? fixScriptFileNamesByRank)
    {
        if (fixScriptFileNamesByRank is null || !fixScriptFileNamesByRank.TryGetValue(rank, out var fileName))
        {
            return "<strong>Fix</strong><br/>(not generated)";
        }

        var fullPath = Path.Combine(outputDirectory, fileName);
        var uri = BuildFileUri(fullPath);
        var encodedName = HtmlEncode(fileName);

        if (string.IsNullOrEmpty(uri))
        {
            return $"<strong>Fix</strong><br/>{encodedName}";
        }

        return $"<strong>Fix</strong><br/><a href=\"{HtmlEncode(uri)}\" target=\"_blank\" rel=\"noopener noreferrer\">{encodedName}</a>";
    }

    private static string BuildFixesZipLinkHtml(string outputDirectory, string fixesZipFile)
    {
        return BuildFixesZipLinkMarkdown(outputDirectory, fixesZipFile);
    }
}
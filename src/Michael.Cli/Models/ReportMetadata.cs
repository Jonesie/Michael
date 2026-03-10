namespace Michael.Cli.Models;

public sealed record ReportMetadata(
    DateTimeOffset GeneratedAtUtc,
    string Version,
    string InputSource,
    string OutputDirectory,
    bool AnalyseOnly,
    int? Limit,
    int ParsedIssueCount,
    int SummaryCount,
    int RankedCount,
    IReadOnlyList<string>? DetectedTools = null,
    string? FixesZipFile = null);
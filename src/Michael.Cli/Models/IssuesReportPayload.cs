using Michael.Analysis.Models;

namespace Michael.Cli.Models;

public sealed record IssuesReportPayload(
    ReportMetadata metadata,
    IReadOnlyList<RankedIssue> issues);

using Michael.Analysis.Models;
using Michael.Cli.Models;

namespace Michael.Cli.Abstractions;

public interface IReportWriter
{
    void Write(string outputDirectory, ReportMetadata metadata, IReadOnlyList<RankedIssue> rankedIssues);
}

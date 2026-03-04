using Michael.Analysis.Models;

namespace Michael.Cli.Abstractions;

public interface IReportWriter
{
    void Write(string outputDirectory, IReadOnlyList<RankedIssue> rankedIssues);
}

using Michael.Analysis.Models;
using Michael.Parsing.Models;

namespace Michael.Analysis.Abstractions;

public interface IAnalyzer
{
    IReadOnlyList<IssueSummary> Summarize(IReadOnlyList<ParsedIssue> issues);
}

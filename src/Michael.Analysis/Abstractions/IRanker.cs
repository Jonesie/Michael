using Michael.Analysis.Models;

namespace Michael.Analysis.Abstractions;

public interface IRanker
{
    IReadOnlyList<RankedIssue> Rank(IReadOnlyList<IssueSummary> summaries, int? limit = null);
}

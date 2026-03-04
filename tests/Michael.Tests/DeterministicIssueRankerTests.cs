using Michael.Analysis;
using Michael.Analysis.Models;

namespace Michael.Tests;

public class DeterministicIssueRankerTests
{
    [Fact]
    public void Rank_OrdersByScoreAndTieBreakers()
    {
        var ranker = new DeterministicIssueRanker();
        var summaries = new[]
        {
            new IssueSummary("B", "warning", "warning issue", 4, 0.90),
            new IssueSummary("A", "error", "error issue", 1, 0.95),
            new IssueSummary("C", "warning", "warning issue 2", 4, 0.80)
        };

        var ranked = ranker.Rank(summaries);

        Assert.Equal(3, ranked.Count);
        Assert.Equal("A", ranked[0].Key);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal("B", ranked[1].Key);
        Assert.Equal("C", ranked[2].Key);
    }

    [Fact]
    public void Rank_AppliesLimit_WhenProvided()
    {
        var ranker = new DeterministicIssueRanker();
        var summaries = new[]
        {
            new IssueSummary("A", "error", "a", 10, 0.99),
            new IssueSummary("B", "warning", "b", 9, 0.90),
            new IssueSummary("C", "warning", "c", 8, 0.90)
        };

        var ranked = ranker.Rank(summaries, limit: 2);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[1].Rank);
    }

    [Fact]
    public void Rank_WithNonPositiveLimit_ReturnsEmpty()
    {
        var ranker = new DeterministicIssueRanker();
        var summaries = new[]
        {
            new IssueSummary("A", "error", "a", 1, 0.99)
        };

        Assert.Empty(ranker.Rank(summaries, 0));
        Assert.Empty(ranker.Rank(summaries, -3));
    }
}

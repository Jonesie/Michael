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
            new IssueSummary("B", "Warning B", new[] { "/tmp/B.cs" }, "warning", "warning issue", 4, 0.90),
            new IssueSummary("A", "Error A", new[] { "/tmp/A.cs" }, "error", "error issue", 1, 0.95),
            new IssueSummary("C", "Warning C", new[] { "/tmp/C.cs" }, "warning", "warning issue 2", 4, 0.80)
        };

        var ranked = ranker.Rank(summaries);

        Assert.Equal(3, ranked.Count);
        Assert.Equal("A", ranked[0].Key);
        Assert.Equal("Error A", ranked[0].Message);
        Assert.Equal(new[] { "/tmp/A.cs" }, ranked[0].Files);
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
            new IssueSummary("A", "A", new[] { "/tmp/A.cs" }, "error", "a", 10, 0.99),
            new IssueSummary("B", "B", new[] { "/tmp/B.cs" }, "warning", "b", 9, 0.90),
            new IssueSummary("C", "C", new[] { "/tmp/C.cs" }, "warning", "c", 8, 0.90)
        };

        var ranked = ranker.Rank(summaries, limit: 2);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[1].Rank);
    }

    [Fact]
    public void Rank_WithZeroLimit_ReturnsAllItems()
    {
        var ranker = new DeterministicIssueRanker();
        var summaries = new[]
        {
            new IssueSummary("A", "A", new[] { "/tmp/A.cs" }, "error", "a", 1, 0.99),
            new IssueSummary("B", "B", new[] { "/tmp/B.cs" }, "warning", "b", 1, 0.75)
        };

        var ranked = ranker.Rank(summaries, 0);

        Assert.Equal(2, ranked.Count);
    }

    [Fact]
    public void Rank_WithNegativeLimit_ReturnsEmpty()
    {
        var ranker = new DeterministicIssueRanker();
        var summaries = new[]
        {
            new IssueSummary("A", "A", new[] { "/tmp/A.cs" }, "error", "a", 1, 0.99)
        };

        Assert.Empty(ranker.Rank(summaries, -3));
    }
}

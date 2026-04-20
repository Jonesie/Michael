using Michael.Analysis;
using Michael.Parsing;

namespace Michael.Tests;

public class AnalysisOnlyIntegrationTests
{
    [Fact]
    public void AnalysisPipeline_ProducesRankedIssues_FromFixtureLog()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");

        var parser = new DotnetBuildLogParser();
        var analyzer = new DeterministicIssueAnalyzer();
        var ranker = new DeterministicIssueRanker();

        using var stream = File.OpenRead(logPath);
        using var reader = new StreamReader(stream);

        var parsed = parser.Parse(reader);
        var summaries = analyzer.Summarize(parsed);
        var ranked = ranker.Rank(summaries, limit: 3);

        Assert.Equal(3, parsed.Count);
        Assert.Equal(3, summaries.Count);
        Assert.Equal(3, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Contains(ranked, issue => issue.Severity == "error");
        Assert.All(ranked, issue => Assert.True(issue.Score > 0));
    }

    [Fact]
    public void AnalysisPipeline_ReturnsNoIssues_ForCleanLog()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-clean.log");

        var parser = new DotnetBuildLogParser();
        var analyzer = new DeterministicIssueAnalyzer();
        var ranker = new DeterministicIssueRanker();

        using var stream = File.OpenRead(logPath);
        using var reader = new StreamReader(stream);

        var parsed = parser.Parse(reader);
        var summaries = analyzer.Summarize(parsed);
        var ranked = ranker.Rank(summaries);

        Assert.Empty(parsed);
        Assert.Empty(summaries);
        Assert.Empty(ranked);
    }
}

using Michael.Analysis;
using Michael.Parsing.Models;

namespace Michael.Tests;

public class DeterministicIssueAnalyzerTests
{
    [Fact]
    public void Summarize_GroupsDuplicateIssues_AndAggregatesFrequency()
    {
        var analyzer = new DeterministicIssueAnalyzer();
        var issues = new[]
        {
            new ParsedIssue("dotnet", "NU1902: Vulnerability found", "warning", "/tmp/A.csproj", 2),
            new ParsedIssue("dotnet", "NU1902: Vulnerability found", "warning", "/tmp/A.csproj", 1),
            new ParsedIssue("dotnet", "MSB4019: Imported project was not found", "error", "/tmp/B.csproj", 1)
        };

        var summaries = analyzer.Summarize(issues);

        Assert.Equal(2, summaries.Count);

        var nuSummary = Assert.Single(summaries, summary => summary.Key == "/tmp/A.csproj::NU1902: Vulnerability found");
        Assert.Equal("warning", nuSummary.Severity);
        Assert.Equal(3, nuSummary.Frequency);
        Assert.Contains("NU1902", nuSummary.Explanation, StringComparison.Ordinal);

        var msbSummary = Assert.Single(summaries, summary => summary.Key == "/tmp/B.csproj::MSB4019: Imported project was not found");
        Assert.Equal("error", msbSummary.Severity);
        Assert.Equal(1, msbSummary.Frequency);
        Assert.True(msbSummary.Confidence >= 0.95);
    }

    [Fact]
    public void Summarize_NormalizesUnknownSeverity_ToWarning()
    {
        var analyzer = new DeterministicIssueAnalyzer();
        var issues = new[]
        {
            new ParsedIssue("dotnet", "CS0168: Variable declared but never used", "warn", "/tmp/C.cs", 1)
        };

        var summary = Assert.Single(analyzer.Summarize(issues));

        Assert.Equal("warning", summary.Severity);
        Assert.Equal(1, summary.Frequency);
    }

    [Fact]
    public void Summarize_WithNoIssues_ReturnsEmpty()
    {
        var analyzer = new DeterministicIssueAnalyzer();

        var summaries = analyzer.Summarize(Array.Empty<ParsedIssue>());

        Assert.Empty(summaries);
    }
}

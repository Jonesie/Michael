using Michael.Analysis.Abstractions;
using Michael.Analysis.Models;

namespace Michael.Analysis;

public sealed class DeterministicIssueRanker : IRanker
{
    public IReadOnlyList<RankedIssue> Rank(IReadOnlyList<IssueSummary> summaries, int? limit = null)
    {
        if (summaries.Count == 0)
        {
            return Array.Empty<RankedIssue>();
        }

        var maxItems = limit.GetValueOrDefault(int.MaxValue);
        if (maxItems <= 0)
        {
            return Array.Empty<RankedIssue>();
        }

        return summaries
            .Select(summary => new
            {
                Summary = summary,
                Score = CalculateScore(summary)
            })
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Summary.Frequency)
            .ThenBy(item => SeverityOrder(item.Summary.Severity))
            .ThenByDescending(item => item.Summary.Confidence)
            .ThenBy(item => item.Summary.Key, StringComparer.Ordinal)
            .Take(maxItems)
            .Select((item, index) => new RankedIssue(
                index + 1,
                item.Summary.Key,
                item.Summary.Message,
                item.Summary.Files,
                item.Summary.Severity,
                item.Summary.Frequency,
                item.Summary.Confidence,
                item.Score,
                item.Summary.Explanation))
            .ToArray();
    }

    private static double CalculateScore(IssueSummary summary)
    {
        var severityWeight = summary.Severity switch
        {
            "error" => 100,
            "warning" => 60,
            "info" => 30,
            _ => 40
        };

        var score = severityWeight
            + (summary.Frequency * 10)
            + (summary.Confidence * 100);

        return Math.Round(score, 2, MidpointRounding.AwayFromZero);
    }

    private static int SeverityOrder(string severity) => severity switch
    {
        "error" => 0,
        "warning" => 1,
        "info" => 2,
        _ => 3
    };
}
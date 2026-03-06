namespace Michael.Analysis.Models;

public sealed record RankedIssue(
    int Rank,
    string Key,
    string Message,
    IReadOnlyList<string> Files,
    string Severity,
    int Frequency,
    double Confidence,
    double Score,
    string Explanation);

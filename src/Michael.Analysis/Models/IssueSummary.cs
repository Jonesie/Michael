namespace Michael.Analysis.Models;

public sealed record IssueSummary(
    string Key,
    string Message,
    IReadOnlyList<string> Files,
    string Severity,
    string Explanation,
    int Frequency,
    double Confidence);

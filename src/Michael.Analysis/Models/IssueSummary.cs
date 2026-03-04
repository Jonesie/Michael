namespace Michael.Analysis.Models;

public sealed record IssueSummary(
    string Key,
    string Severity,
    string Explanation,
    int Frequency,
    double Confidence);

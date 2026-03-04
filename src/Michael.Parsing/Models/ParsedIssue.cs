namespace Michael.Parsing.Models;

public sealed record ParsedIssue(
    string Source,
    string Message,
    string Severity,
    string? FilePath,
    int Count);

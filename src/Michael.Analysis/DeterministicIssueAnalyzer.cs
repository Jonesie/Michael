using Michael.Analysis.Abstractions;
using Michael.Analysis.Models;
using Michael.Parsing.Models;

namespace Michael.Analysis;

public sealed class DeterministicIssueAnalyzer : IAnalyzer
{
	public IReadOnlyList<IssueSummary> Summarize(IReadOnlyList<ParsedIssue> issues)
	{
		if (issues.Count == 0)
		{
			return Array.Empty<IssueSummary>();
		}

		var grouped = issues
			.Select(issue => new
			{
				Issue = issue,
				NormalizedSeverity = NormalizeSeverity(issue.Severity)
			})
			.GroupBy(entry => new
			{
				Message = entry.Issue.Message.Trim(),
				entry.NormalizedSeverity
			});

		return grouped
			.Select(group =>
			{
				var sample = group.First().Issue;
				var frequency = group.Sum(entry => Math.Max(1, entry.Issue.Count));
				var message = group.Key.Message;
				var files = group
					.Select(entry => string.IsNullOrWhiteSpace(entry.Issue.FilePath) ? "(no-file)" : entry.Issue.FilePath!)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(path => path, StringComparer.Ordinal)
					.ToArray();
				var key = BuildKey(message);
				var confidence = CalculateConfidence(group.Key.NormalizedSeverity, frequency, sample.Message);

				return new IssueSummary(
					key,
					message,
					files,
					group.Key.NormalizedSeverity,
					BuildExplanation(sample.Message, group.Key.NormalizedSeverity, frequency),
					frequency,
					confidence);
			})
			.OrderByDescending(summary => summary.Frequency)
			.ThenBy(summary => SeverityOrder(summary.Severity))
			.ThenBy(summary => summary.Key, StringComparer.Ordinal)
			.ToArray();
	}

	private static string BuildKey(string message)
	{
		return message;
	}

	private static string NormalizeSeverity(string severity)
	{
		if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
		{
			return "error";
		}

		if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
		{
			return "warning";
		}

		if (severity.Equals("info", StringComparison.OrdinalIgnoreCase) ||
			severity.Equals("information", StringComparison.OrdinalIgnoreCase))
		{
			return "info";
		}

		return "warning";
	}

	private static string BuildExplanation(string message, string severity, int frequency)
	{
		var code = ExtractDiagnosticCode(message);
		var impact = severity == "error"
			? "This blocks a successful build"
			: "This does not block compilation but should be addressed";

		var codePrefix = code is null ? "Issue" : $"{code}";

		return $"{codePrefix} appears {frequency} time(s). {impact}.";
	}

	private static string? ExtractDiagnosticCode(string message)
	{
		var separatorIndex = message.IndexOf(':');
		if (separatorIndex <= 0)
		{
			return null;
		}

		var candidate = message[..separatorIndex].Trim();
		return candidate.Length is >= 4 and <= 10
			? candidate
			: null;
	}

	private static double CalculateConfidence(string severity, int frequency, string message)
	{
		var baseline = severity == "error" ? 0.95 : 0.85;
		var frequencyBoost = Math.Min(0.10, Math.Max(0, frequency - 1) * 0.01);
		var hasCodeBoost = ExtractDiagnosticCode(message) is null ? 0.0 : 0.03;
		var score = baseline + frequencyBoost + hasCodeBoost;

		return Math.Round(Math.Min(0.99, score), 2, MidpointRounding.AwayFromZero);
	}

	private static int SeverityOrder(string severity) => severity switch
	{
		"error" => 0,
		"warning" => 1,
		"info" => 2,
		_ => 3
	};
}

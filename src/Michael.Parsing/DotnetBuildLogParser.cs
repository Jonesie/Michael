using System.Text.RegularExpressions;
using Michael.Parsing.Abstractions;
using Michael.Parsing.Models;

namespace Michael.Parsing;

public sealed partial class DotnetBuildLogParser : IParser
{
	private const string Source = "dotnet";

	public IReadOnlyList<ParsedIssue> Parse(string logContent)
	{
		using var reader = new StringReader(logContent ?? string.Empty);
		return Parse(reader);
	}

	public IReadOnlyList<ParsedIssue> Parse(TextReader reader)
	{
		var issueCounts = new Dictionary<(string Source, string Message, string Severity, string? FilePath), int>();

		while (reader.ReadLine() is { } line)
		{
			var match = IssueLineRegex().Match(line);
			if (!match.Success)
			{
				continue;
			}

			var severity = match.Groups["severity"].Value.ToLowerInvariant();
			var code = match.Groups["code"].Value.Trim();
			var message = CleanMessage(match.Groups["message"].Value);
			var normalizedMessage = $"{code}: {message}";
			var filePath = NormalizeFilePath(match.Groups["location"].Value);

			var key = (Source, normalizedMessage, severity, filePath);
			issueCounts.TryGetValue(key, out var current);
			issueCounts[key] = current + 1;
		}

		return issueCounts
			.Select(pair => new ParsedIssue(
				pair.Key.Source,
				pair.Key.Message,
				pair.Key.Severity,
				pair.Key.FilePath,
				pair.Value))
			.OrderByDescending(issue => issue.Count)
			.ThenByDescending(issue => issue.Severity, StringComparer.Ordinal)
			.ThenBy(issue => issue.FilePath, StringComparer.Ordinal)
			.ThenBy(issue => issue.Message, StringComparer.Ordinal)
			.ToArray();
	}

	private static string NormalizeFilePath(string location)
	{
		return location.Trim();
	}

	private static string CleanMessage(string message)
	{
		var trimmed = message.Trim();
		var projectSuffixIndex = trimmed.LastIndexOf(" [", StringComparison.Ordinal);
		if (projectSuffixIndex > 0 && trimmed.EndsWith(']'))
		{
			return trimmed[..projectSuffixIndex].TrimEnd();
		}

		return trimmed;
	}

	[GeneratedRegex(
		@"^\s*(?<location>.+?)\s*:\s*(?<severity>warning|error)\s+(?<code>[A-Za-z]+\d+)\s*:\s*(?<message>.+)$",
		RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex IssueLineRegex();
}

using Michael.Parsing.Models;

namespace Michael.Parsing.Abstractions;

public interface IParser
{
    IReadOnlyList<ParsedIssue> Parse(TextReader reader);

    IReadOnlyList<ParsedIssue> Parse(string logContent);
}

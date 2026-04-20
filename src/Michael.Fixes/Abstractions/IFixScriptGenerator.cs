using Michael.Analysis.Models;

namespace Michael.Fixes.Abstractions;

public interface IFixScriptGenerator
{
    IReadOnlyDictionary<int, string> Generate(
        string outputDirectory,
        IReadOnlyList<RankedIssue> rankedIssues,
    string? scriptTemplateText = null,
    string? scriptFileExtension = null,
    int? limitFixFiles = null);
}

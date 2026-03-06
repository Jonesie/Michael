using Michael.Analysis.Models;
using Michael.Fixes;

namespace Michael.Tests;

public class CopilotCliFixScriptGeneratorTests
{
    [Fact]
    public void Generate_WritesPowerShellFiles_PerRank_WithPromptContext()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var sourceFile = Path.Combine(tempDir, "Sample.cs");
        var secondSourceFile = Path.Combine(tempDir, "Sample2.cs");
        File.WriteAllLines(sourceFile, new[]
        {
            "namespace Demo;",
            "",
            "public class Sample",
            "{",
            "    public void Run()",
            "    {",
            "        var value = 1;",
            "    }",
            "}"
        });

        File.WriteAllLines(secondSourceFile, new[]
        {
            "namespace Demo;",
            "public class Sample2",
            "{",
            "    public void Run()",
            "    {",
            "        var value = 2;",
            "    }",
            "}"
        });

        try
        {
            var generator = new CopilotCliFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "CS0219: assigned but never used",
                    Message: "CS0219: The variable 'value' is assigned but its value is never used",
                    Files: new[] { $"{sourceFile}(7,9)", $"{secondSourceFile}(6,9)" },
                    Severity: "warning",
                    Frequency: 1,
                    Confidence: 0.90,
                    Score: 180,
                    Explanation: "Sample"),
                new RankedIssue(
                    Rank: 2,
                    Key: "CS0168: declared but never used",
                    Message: "CS0168: The variable 'x' is declared but never used",
                    Files: new[] { sourceFile },
                    Severity: "warning",
                    Frequency: 1,
                    Confidence: 0.90,
                    Score: 170,
                    Explanation: "Sample")
            };

            var filesByRank = generator.Generate(tempDir, rankedIssues);

            Assert.Equal(2, filesByRank.Count);
            Assert.Equal("fix-rank-1.ps1", filesByRank[1]);
            Assert.Equal("fix-rank-2.ps1", filesByRank[2]);

            var firstScriptPath = Path.Combine(tempDir, filesByRank[1]);
            Assert.True(File.Exists(firstScriptPath));

            var firstScript = File.ReadAllText(firstScriptPath);
            Assert.Contains("# Michael generated fix script", firstScript, StringComparison.Ordinal);
            Assert.Contains("# Rank: 1", firstScript, StringComparison.Ordinal);
            Assert.Contains("# Target files: 2", firstScript, StringComparison.Ordinal);
            Assert.Contains("copilot -i \"agent --prompt $Prompt\"", firstScript, StringComparison.Ordinal);
            Assert.Contains("Rank: 1", firstScript, StringComparison.Ordinal);
            Assert.Contains("Error: CS0219: The variable 'value' is assigned but its value is never used", firstScript, StringComparison.Ordinal);
            Assert.Contains("Files that need fixing (apply to all listed files if confirmed):", firstScript, StringComparison.Ordinal);
            Assert.Contains($"- {sourceFile}(7,9)", firstScript, StringComparison.Ordinal);
            Assert.Contains($"- {secondSourceFile}(6,9)", firstScript, StringComparison.Ordinal);
            Assert.Contains("Ask for confirmation before making edits", firstScript, StringComparison.Ordinal);
            Assert.Contains("Apply the fix across all listed target files only if the user confirms", firstScript, StringComparison.Ordinal);
            Assert.Contains($"File: {sourceFile}", firstScript, StringComparison.Ordinal);
            Assert.Contains("L7:         var value = 1;", firstScript, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
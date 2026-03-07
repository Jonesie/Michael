using Michael.Analysis.Models;
using Michael.Fixes;

namespace Michael.Tests;

public class TemplateFixScriptGeneratorTests
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
            var generator = new TemplateFixScriptGenerator();
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
            Assert.Contains($"- {sourceFile}", firstScript, StringComparison.Ordinal);
            Assert.Contains($"- {secondSourceFile}", firstScript, StringComparison.Ordinal);
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

    [Fact]
    public void Generate_WithNoConcreteTargets_ProducesCountZeroAndNoSampleMessage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "NOFILE",
                    Message: "CS0000: No target files available",
                    Files: new[] { "(no-file)", "   ", "relative/path/File.cs(5,1)" },
                    Severity: "warning",
                    Frequency: 1,
                    Confidence: 0.80,
                    Score: 120,
                    Explanation: "No files")
            };

            var filesByRank = generator.Generate(tempDir, rankedIssues);
            var scriptPath = Path.Combine(tempDir, filesByRank[1]);
            var script = File.ReadAllText(scriptPath);

            Assert.Contains("# Target files: 1", script, StringComparison.Ordinal);
            Assert.Contains("Files that need fixing (apply to all listed files if confirmed):", script, StringComparison.Ordinal);
            Assert.Contains("- relative/path/File.cs", script, StringComparison.Ordinal);
            Assert.Contains("- No source file sample was available.", script, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_HandlesMissingSourceFiles_WithoutFailing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-missing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var missingPath = Path.Combine(tempDir, "DoesNotExist.cs");
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "MISSING",
                    Message: "CS1001: Identifier expected",
                    Files: new[] { $"{missingPath}(10,1)" },
                    Severity: "error",
                    Frequency: 1,
                    Confidence: 0.95,
                    Score: 220,
                    Explanation: "Missing source")
            };

            var filesByRank = generator.Generate(tempDir, rankedIssues);
            var scriptPath = Path.Combine(tempDir, filesByRank[1]);
            var script = File.ReadAllText(scriptPath);

            Assert.Contains("# Target files: 1", script, StringComparison.Ordinal);
            Assert.Contains($"- {missingPath}", script, StringComparison.Ordinal);
            Assert.Contains("- No source file sample was available.", script, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_AppliesCustomTemplatePlaceholders()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-template-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "CS1002",
                    Message: "CS1002: ; expected",
                    Files: new[] { "C:/repo/src/File.cs(10,5)" },
                    Severity: "error",
                    Frequency: 2,
                    Confidence: 0.95,
                    Score: 300,
                    Explanation: "Template check")
            };

            var template = """
rank=[[rank]]
targets=[[targetFileCount]]
details:
[[issueDetails]]
files:
[[fileList]]
samples:
[[samples]]
cmd:
copilot -i "agent --prompt $Prompt"
""";

            var filesByRank = generator.Generate(tempDir, rankedIssues, template);
            var script = File.ReadAllText(Path.Combine(tempDir, filesByRank[1]));

            Assert.Contains("rank=1", script, StringComparison.Ordinal);
            Assert.Contains("targets=1", script, StringComparison.Ordinal);
            Assert.Contains("- Rank: 1", script, StringComparison.Ordinal);
            Assert.Contains("- C:/repo/src/File.cs", script, StringComparison.Ordinal);
            Assert.Contains("- No source file sample was available.", script, StringComparison.Ordinal);
            Assert.Contains("copilot -i \"agent --prompt $Prompt\"", script, StringComparison.Ordinal);
            Assert.DoesNotContain("[[issueDetails]]", script, StringComparison.Ordinal);
            Assert.DoesNotContain("[[fileList]]", script, StringComparison.Ordinal);
            Assert.DoesNotContain("[[samples]]", script, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_WhenTemplateContainsUnknownPlaceholder_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-unresolved-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "CS0001",
                    Message: "Sample issue",
                    Files: new[] { "(no-file)" },
                    Severity: "error",
                    Frequency: 1,
                    Confidence: 0.90,
                    Score: 100,
                    Explanation: "Sample")
            };

            var template = "rank=[[rank]] unknown=[[unknownPlaceholder]]";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                generator.Generate(tempDir, rankedIssues, template));

            Assert.Contains("Unresolved template placeholders for rank 1", exception.Message, StringComparison.Ordinal);
            Assert.Contains("[[unknownPlaceholder]]", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_WhenTemplateHasWhitespaceBetweenBrackets_ReplacesTokens()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-spaced-brackets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "CS0003",
                    Message: "Whitespace token sample",
                    Files: new[] { "(no-file)" },
                    Severity: "warning",
                    Frequency: 1,
                    Confidence: 0.90,
                    Score: 90,
                    Explanation: "Sample")
            };

            var template = """
    rank: [
      [rank
      ]
    ]
    files:
    [
      [fileList
      ]
    ]
    """;

            var filesByRank = generator.Generate(tempDir, rankedIssues, template);
            var script = File.ReadAllText(Path.Combine(tempDir, filesByRank[1]));

            Assert.Contains("rank: 1", script, StringComparison.Ordinal);
            Assert.DoesNotContain("[\n  [rank", script, StringComparison.Ordinal);
            Assert.Contains("files:", script, StringComparison.Ordinal);
            Assert.Contains("- No concrete target files were identified.", script, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_WithScriptFileExtension_UsesConfiguredExtension()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-extension-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "EXT",
                    Message: "Extension sample",
                    Files: new[] { "(no-file)" },
                    Severity: "warning",
                    Frequency: 1,
                    Confidence: 0.90,
                    Score: 100,
                    Explanation: "Sample")
            };

            var filesByRank = generator.Generate(tempDir, rankedIssues, scriptTemplateText: null, scriptFileExtension: ".sh");

            Assert.Equal("fix-rank-1.sh", filesByRank[1]);
            Assert.True(File.Exists(Path.Combine(tempDir, "fix-rank-1.sh")));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Generate_WithDuplicateLocationsInSameFile_EmitsSingleSampleBlock()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-fixes-sample-dedupe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var sourceFile = Path.Combine(tempDir, "Sample.cs");
        File.WriteAllLines(sourceFile, new[]
        {
            "namespace Demo;",
            "",
            "public class Sample",
            "{",
            "    public void Run()",
            "    {",
            "        var value1 = 1;",
            "        var value2 = 2;",
            "    }",
            "}"
        });

        try
        {
            var generator = new TemplateFixScriptGenerator();
            var rankedIssues = new[]
            {
                new RankedIssue(
                    Rank: 1,
                    Key: "DEDUPE",
                    Message: "Duplicate locations in same file",
                    Files: new[] { $"{sourceFile}(8,9)", $"{sourceFile}(7,9)" },
                    Severity: "warning",
                    Frequency: 1,
                    Confidence: 0.90,
                    Score: 100,
                    Explanation: "Sample dedupe")
            };

            var filesByRank = generator.Generate(tempDir, rankedIssues);
            var script = File.ReadAllText(Path.Combine(tempDir, filesByRank[1]));

            Assert.Equal(1, CountOccurrences(script, $"- File: {sourceFile}"));
            Assert.Contains("L5:     public void Run()", script, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
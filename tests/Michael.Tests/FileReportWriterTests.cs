using Michael.Analysis.Models;
using Michael.Cli;
using Michael.Cli.Models;

namespace Michael.Tests;

public class FileReportWriterTests
{
    [Fact]
    public void Write_BuildsMarkdownTable_WithAllRankedRows_AndFileLists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"michael-report-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var writer = new FileReportWriter();
            var metadata = new ReportMetadata(
                DateTimeOffset.UtcNow,
                "1.0.0-test",
                "input.log",
                tempDir,
                true,
                2,
                10,
                2,
                2,
                new[] { ".NET SDK 10.0.100", ".NET", "C#" });

            var ranked = new[]
            {
                new RankedIssue(1, "MSB4019: Missing targets", "MSB4019: Missing targets", new[] {"/tmp/a.csproj", "/tmp/b.csproj"}, "error", 5, 0.99, 249.0, "err"),
                new RankedIssue(2, "CS0168: Unused variable", "CS0168: Unused variable", new[] {"/tmp/c.cs"}, "warning", 3, 0.90, 180.0, "warn")
            };

            var fixFileNamesByRank = new Dictionary<int, string>
            {
                [1] = "fix-rank-1.ps1",
                [2] = "fix-rank-2.ps1"
            };

            writer.Write(tempDir, metadata, ranked, fixFileNamesByRank);

            var summaryPath = Path.Combine(tempDir, "summary.md");
            var htmlPath = Path.Combine(tempDir, "summary.html");
            Assert.True(File.Exists(summaryPath));
            Assert.True(File.Exists(htmlPath));

            var markdown = File.ReadAllText(summaryPath);
            var html = File.ReadAllText(htmlPath);
            var expectedFixUri = $"vscode://file/{string.Join('/', Path.Combine(tempDir, "fix-rank-1.ps1").Replace('\\', '/').Split('/').Select(Uri.EscapeDataString))}";
            Assert.Contains("| Rank | Severity | Frequency | Details |", markdown, StringComparison.Ordinal);
            Assert.Contains("- Detected tools/frameworks: .NET SDK 10.0.100, .NET, C#", markdown, StringComparison.Ordinal);
            Assert.Contains("<strong>Error Message</strong><br/>MSB4019: Missing targets", markdown, StringComparison.Ordinal);
            Assert.Contains("<summary><strong>Files</strong> (2)</summary>", markdown, StringComparison.Ordinal);
            Assert.Contains("<a href=\"vscode://file//tmp/a.csproj\" target=\"_blank\" rel=\"noopener noreferrer\">/tmp/a.csproj</a>", markdown, StringComparison.Ordinal);
            Assert.Contains("<a href=\"vscode://file//tmp/b.csproj\" target=\"_blank\" rel=\"noopener noreferrer\">/tmp/b.csproj</a>", markdown, StringComparison.Ordinal);
            Assert.Contains("<summary><strong>Files</strong> (1)</summary>", markdown, StringComparison.Ordinal);
            Assert.Contains("<a href=\"vscode://file//tmp/c.cs\" target=\"_blank\" rel=\"noopener noreferrer\">/tmp/c.cs</a>", markdown, StringComparison.Ordinal);
            Assert.Contains($"<strong>Fix</strong><br/><a href=\"{expectedFixUri}\" target=\"_blank\" rel=\"noopener noreferrer\">fix-rank-1.ps1</a>", markdown, StringComparison.Ordinal);
            Assert.Contains("<title>Michael Analysis Summary</title>", html, StringComparison.Ordinal);
            Assert.Contains("<li><strong>Detected tools/frameworks:</strong> .NET SDK 10.0.100, .NET, C#</li>", html, StringComparison.Ordinal);
            Assert.Contains("<th>Details</th>", html, StringComparison.Ordinal);
            Assert.Contains("<details><summary><strong>Files</strong> (2)</summary><ul>", html, StringComparison.Ordinal);
            Assert.Contains("<li><a href=\"vscode://file//tmp/a.csproj\" target=\"_blank\" rel=\"noopener noreferrer\">/tmp/a.csproj</a></li>", html, StringComparison.Ordinal);
            Assert.Contains($"<strong>Fix</strong><br/><a href=\"{expectedFixUri}\" target=\"_blank\" rel=\"noopener noreferrer\">fix-rank-1.ps1</a>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<details open", markdown, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<details open", html, StringComparison.OrdinalIgnoreCase);

            var zipMetadata = metadata with { FixesZipFile = "fixes.zip" };
            writer.Write(tempDir, zipMetadata, ranked, fixFileNamesByRank);

            markdown = File.ReadAllText(summaryPath);
            html = File.ReadAllText(htmlPath);
            var expectedZipUri = $"vscode://file/{string.Join('/', Path.Combine(tempDir, "fixes.zip").Replace('\\', '/').Split('/').Select(Uri.EscapeDataString))}";
            Assert.Contains($"- Fixes archive: <a href=\"{expectedZipUri}\" target=\"_blank\" rel=\"noopener noreferrer\">fixes.zip</a>", markdown, StringComparison.Ordinal);
            Assert.Contains($"<li><strong>Fixes archive:</strong> <a href=\"{expectedZipUri}\" target=\"_blank\" rel=\"noopener noreferrer\">fixes.zip</a></li>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<strong>Fix</strong>", markdown, StringComparison.Ordinal);
            Assert.DoesNotContain("<strong>Fix</strong>", html, StringComparison.Ordinal);

            writer.Write(tempDir, metadata with { RankedCount = 1 },
                new[]
                {
                    new RankedIssue(1, "LINE", "Has line info", new[] { "/tmp/WithLine.cs(42,13)" }, "warning", 1, 0.90, 160.0, "warn")
                });

            markdown = File.ReadAllText(summaryPath);
            html = File.ReadAllText(htmlPath);
            Assert.Contains("href=\"vscode://file//tmp/WithLine.cs:42:13\" target=\"_blank\"", markdown, StringComparison.Ordinal);
            Assert.Contains("href=\"vscode://file//tmp/WithLine.cs:42:13\" target=\"_blank\"", html, StringComparison.Ordinal);

            var longMessage = new string('X', 200);
            var longIssue = new[]
            {
                new RankedIssue(1, "LONG", longMessage, new[] {"/a/b/c/d/e/File.cs"}, "warning", 1, 0.90, 160.0, "warn")
            };

            writer.Write(tempDir, metadata with { RankedCount = 1 }, longIssue);
            markdown = File.ReadAllText(summaryPath);

            Assert.Contains("<a href=\"vscode://file//a/b/c/d/e/File.cs\" target=\"_blank\" rel=\"noopener noreferrer\">/a/b/c/d/e/File.cs</a>", markdown, StringComparison.Ordinal);
            Assert.DoesNotContain(longMessage, markdown, StringComparison.Ordinal);

            var manyFiles = Enumerable.Range(1, 9)
                .Select(index => $"/root/project/src/Feature/File{index}.cs")
                .ToArray();

            writer.Write(tempDir, metadata with { RankedCount = 1 },
                new[]
                {
                    new RankedIssue(1, "MANY", "Many files", manyFiles, "warning", 9, 0.90, 190.0, "warn")
                });

            markdown = File.ReadAllText(summaryPath);
            Assert.Contains("<details><summary><strong>Files</strong> (9)</summary>", markdown, StringComparison.Ordinal);
            Assert.Contains("<a href=\"vscode://file//root/project/src/Feature/File9.cs\" target=\"_blank\" rel=\"noopener noreferrer\">/root/project/src/Feature/File9.cs</a>", markdown, StringComparison.Ordinal);
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

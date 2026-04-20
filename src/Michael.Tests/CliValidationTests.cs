using System.Diagnostics;
using System.IO.Compression;

namespace Michael.Tests;

public class CliValidationTests
{
    [Fact]
    public void Cli_RejectsRemovedApplyFixesOption()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");

        var result = RunCli(repoRoot, $"--input \"{logPath}\" --output out-test --apply-fixes");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--apply-fixes", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cli_AllowsZeroLimit_AsUnlimited()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-limit-zero-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only --limit 0");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Limit    : unlimited", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_AllowsNegativeLimit_AsUnlimited()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-limit-negative-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only --limit -1");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Limit    : unlimited", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_GeneratesFixFiles_ByDefault_WhenNotAnalyseOnly()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\"");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDir, "fix-rank-1.ps1")));
            Assert.True(File.Exists(Path.Combine(outputDir, "fix-rank-2.ps1")));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_DoesNotGenerateFixes_InAnalyseOnlyMode()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-analysis-only-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only");

            Assert.Equal(0, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(outputDir, "fix-rank-1.ps1")));
            Assert.Contains("Fixes    : 0", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Detected : .NET SDK 9.0.114, .NET, C#", result.Output, StringComparison.OrdinalIgnoreCase);

            var summaryPath = Path.Combine(outputDir, "summary.md");
            var summary = File.ReadAllText(summaryPath);
            Assert.Contains("- Detected tools/frameworks: .NET SDK 9.0.114, .NET, C#", summary, StringComparison.Ordinal);
            Assert.DoesNotContain("<strong>Fix</strong>", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_WithZipOption_GeneratesFixesZip_WhenFixesAreGenerated()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-zip-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --zip");

            Assert.Equal(0, result.ExitCode);
            var zipPath = Path.Combine(outputDir, "fixes.zip");
            Assert.True(File.Exists(zipPath));
            Assert.False(File.Exists(Path.Combine(outputDir, "fix-rank-1.ps1")));
            Assert.False(File.Exists(Path.Combine(outputDir, "fix-rank-2.ps1")));

            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Contains(archive.Entries, entry => string.Equals(entry.FullName, "fix-rank-1.ps1", StringComparison.Ordinal));
            Assert.Contains(archive.Entries, entry => string.Equals(entry.FullName, "fix-rank-2.ps1", StringComparison.Ordinal));
            Assert.Contains("fixes.zip", result.Output, StringComparison.OrdinalIgnoreCase);

            var summaryPath = Path.Combine(outputDir, "summary.md");
            var summary = File.ReadAllText(summaryPath);
            Assert.Contains("- Fixes archive:", summary, StringComparison.Ordinal);
            Assert.DoesNotContain("<strong>Fix</strong>", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_WithZipOption_InAnalyseOnlyMode_DoesNotCreateFixesZip()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-zip-analysis-only-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only --zip");

            Assert.Equal(0, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(outputDir, "fixes.zip")));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_SupportsAnalysisOnlyAlias()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-analysis-alias-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --analysis-only");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Fixes    : 0", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_UsesConfiguredScriptTemplate_ForGeneratedFixScripts()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-template-{Guid.NewGuid():N}");
        var configDir = Path.Combine(Path.GetTempPath(), $"michael-config-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(configDir);

        var templatePath = Path.Combine(configDir, "custom-template.ps1.template");
        File.WriteAllText(templatePath, """
custom-rank=[[rank]]
custom-files:
[[fileList]]
command:
copilot -i "agent --prompt $Prompt"
""");

        var configPath = Path.Combine(configDir, "michael.config.json");
        File.WriteAllText(configPath, """
        {
          "fixes": {
            "scriptTemplateFile": "custom-template.ps1.template"
          }
        }
        """);

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --config \"{configPath}\"");

            Assert.Equal(0, result.ExitCode);
            var script = File.ReadAllText(Path.Combine(outputDir, "fix-rank-1.ps1"));
            Assert.Contains("custom-rank=1", script, StringComparison.Ordinal);
            Assert.Contains("custom-files:", script, StringComparison.Ordinal);
            Assert.DoesNotContain("[[fileList]]", script, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }

            if (Directory.Exists(configDir))
            {
                Directory.Delete(configDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_UsesBashTemplateExtension_ForGeneratedFixScripts()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
                var bashTemplatePath = Path.Combine(repoRoot, "src", "Michael.Cli", "templates", "fix-script.sh.template");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-bash-template-{Guid.NewGuid():N}");
        var configPath = Path.Combine(Path.GetTempPath(), $"michael-bash-config-{Guid.NewGuid():N}.json");

                File.WriteAllText(configPath, $$"""
                {
                    "fixes": {
                        "scriptTemplateFile": "{{bashTemplatePath.Replace("\\", "\\\\")}}"
                    }
                }
                """);

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --config \"{configPath}\"");

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(outputDir, "fix-rank-1.sh")));
            Assert.False(File.Exists(Path.Combine(outputDir, "fix-rank-1.ps1")));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }

            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    [Fact]
    public void Cli_WithExistingOutputFiles_DeclineConfirmation_AbortsWithoutClearing()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-existing-decline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDir);
        var staleFile = Path.Combine(outputDir, "stale.txt");
        File.WriteAllText(staleFile, "stale");

        try
        {
            var result = RunCli(
                repoRoot,
                $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only",
                standardInput: "n" + Environment.NewLine);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("contains existing files", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("run cancelled", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(staleFile));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_WithClearExistingOutputFlag_ClearsExistingFilesAndContinues()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-existing-clear-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDir);
        var staleFile = Path.Combine(outputDir, "stale.txt");
        File.WriteAllText(staleFile, "stale");

        try
        {
            var result = RunCli(
                repoRoot,
                $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only --clear-existing-output");

            Assert.Equal(0, result.ExitCode);
            Assert.False(File.Exists(staleFile));
            Assert.True(File.Exists(Path.Combine(outputDir, "issues.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "summary.md")));
            Assert.True(File.Exists(Path.Combine(outputDir, "summary.html")));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_CiMode_SkipsAsciiBanner_And_SuppressesFileListsInSummary()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-ci-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --ci");

            Assert.Equal(0, result.ExitCode);
            // ASCII art should be skipped in CI mode
            Assert.DoesNotContain("███", result.Output, StringComparison.Ordinal);

            var summaryPath = Path.Combine(outputDir, "summary.md");
            Assert.True(File.Exists(summaryPath));
            var summary = File.ReadAllText(summaryPath);

            // File lists should be suppressed: summary contains the count but not file links
            Assert.Matches(@"<strong>Files</strong> \([0-9]+\)", summary);
            Assert.DoesNotContain("vscode://file", summary, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_VersionOption_PrintsRawVersionAndExits()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-version-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, "--version --input dummy --output out-nop");

            Assert.Equal(0, result.ExitCode);

            var expectedVersion = typeof(Michael.Cli.FileReportWriter).Assembly
                .GetCustomAttributes(false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()
                ?.InformationalVersion
                ?? "0.0.0";

            Assert.Equal(expectedVersion, result.Output.Trim());

            // Ensure no output directory was created
            Assert.False(Directory.Exists(outputDir));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Cli_DisplaysLimitFixFiles_InBanner()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");
        var outputDir = Path.Combine(Path.GetTempPath(), $"michael-cli-limitfixfiles-{Guid.NewGuid():N}");

        try
        {
            var result = RunCli(repoRoot, $"--input \"{logPath}\" --output \"{outputDir}\" --analyse-only --limitfixfiles 1");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("Fix file limit: 1", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    private static (int ExitCode, string Output) RunCli(string repoRoot, string arguments, string? standardInput = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project src/Michael.Cli/Michael.Cli.csproj -- {arguments}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");

        if (standardInput is not null)
        {
            process.StandardInput.Write(standardInput);
        }

        process.StandardInput.Close();

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(30000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // no-op
            }

            throw new TimeoutException("Timed out waiting for CLI process to complete.");
        }

        return (process.ExitCode, standardOutput + Environment.NewLine + standardError);
    }
}

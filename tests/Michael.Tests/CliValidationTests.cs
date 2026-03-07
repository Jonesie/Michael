using System.Diagnostics;

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
    public void Cli_RejectsNonPositiveLimit()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");

        var result = RunCli(repoRoot, $"--input \"{logPath}\" --output out-test --analyse-only --limit 0");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--limit must be greater than 0", result.Output, StringComparison.OrdinalIgnoreCase);
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

        var result = RunCli(repoRoot, $"--input \"{logPath}\" --output out-test --analysis-only");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Fixes    : 0", result.Output, StringComparison.OrdinalIgnoreCase);
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

    private static (int ExitCode, string Output) RunCli(string repoRoot, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project src/Michael.Cli/Michael.Cli.csproj -- {arguments}",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");

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

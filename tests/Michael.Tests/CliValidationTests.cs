using System.Diagnostics;

namespace Michael.Tests;

public class CliValidationTests
{
    [Fact]
    public void Cli_BlocksApplyFixes_InMvp()
    {
        var repoRoot = TestWorkspace.RepoRoot();
        var logPath = Path.Combine(repoRoot, "data", "sample-dotnet-small.log");

        var result = RunCli(repoRoot, $"--input \"{logPath}\" --output out-test --apply-fixes");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--apply-fixes is not available", result.Output, StringComparison.OrdinalIgnoreCase);
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

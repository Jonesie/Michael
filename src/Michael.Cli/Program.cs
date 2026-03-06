using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using Michael.Analysis;
using Michael.Cli;
using Michael.Cli.Models;
using Michael.Parsing;

var inputOption = new Option<FileInfo?>(
    name: "--input",
    description: "Path to the build log file to analyse.");

var outputOption = new Option<DirectoryInfo?>(
    name: "--output",
    description: "Directory to write report files (issues.json, summary.md, summary.html). Defaults to 'out'.");

var analyseOnlyOption = new Option<bool>(
    name: "--analyse-only",
    description: "Parse and analyse the log without applying any fixes.");

var applyFixesOption = new Option<bool>(
    name: "--apply-fixes",
    description: "Apply generated fixes (post-MVP, currently blocked).");

var limitOption = new Option<int?>(
    name: "--limit",
    description: "Maximum number of issues to include in the report.");

var gitBranchOption = new Option<string?>(
    name: "--git-branch",
    description: "Git branch name to include in report metadata.");

var aiToolOption = new Option<string?>(
    name: "--ai-tool",
    description: "AI tool to use for fix generation (e.g. copilot).");

var aiModelOption = new Option<string?>(
    name: "--ai-model",
    description: "AI model to use for fix generation.");

var rootCommand = new RootCommand("Michael – build log analyser and issue reporter.")
{
    inputOption,
    outputOption,
    analyseOnlyOption,
    applyFixesOption,
    limitOption,
    gitBranchOption,
    aiToolOption,
    aiModelOption,
};

rootCommand.SetHandler((InvocationContext context) =>
{
    var input       = context.ParseResult.GetValueForOption(inputOption);
    var output      = context.ParseResult.GetValueForOption(outputOption);
    var analyseOnly = context.ParseResult.GetValueForOption(analyseOnlyOption);
    var applyFixes  = context.ParseResult.GetValueForOption(applyFixesOption);
    var limit       = context.ParseResult.GetValueForOption(limitOption);
    var gitBranch   = context.ParseResult.GetValueForOption(gitBranchOption);
    var aiTool      = context.ParseResult.GetValueForOption(aiToolOption);
    var aiModel     = context.ParseResult.GetValueForOption(aiModelOption);

    if (applyFixes)
    {
        Console.Error.WriteLine(
            "--apply-fixes is not available in the current MVP release. " +
            "Fix generation and application is planned for a future version.");
        context.ExitCode = 1;
        return;
    }

    if (input is null)
    {
        Console.Error.WriteLine("Error: --input is required.");
        context.ExitCode = 1;
        return;
    }

    if (!input.Exists)
    {
        Console.Error.WriteLine($"Error: input file not found: {input.FullName}");
        context.ExitCode = 1;
        return;
    }

    if (limit is <= 0)
    {
        Console.Error.WriteLine("Error: --limit must be greater than 0 when provided.");
        context.ExitCode = 1;
        return;
    }

    output ??= new DirectoryInfo("out");

    Console.WriteLine($"Michael {GetVersion()} – analysing {input.Name}");
    Console.WriteLine($"  Output   : {output.FullName}");
    if (limit.HasValue)   Console.WriteLine($"  Limit    : {limit}");
    if (gitBranch is not null) Console.WriteLine($"  Branch   : {gitBranch}");
    if (aiTool is not null)    Console.WriteLine($"  AI tool  : {aiTool}");
    if (aiModel is not null)   Console.WriteLine($"  AI model : {aiModel}");

    using var stream = input.OpenRead();
    using var reader = new StreamReader(stream);

    var parser = new DotnetBuildLogParser();
    var analyzer = new DeterministicIssueAnalyzer();
    var ranker = new DeterministicIssueRanker();
    var writer = new FileReportWriter();

    var parsedIssues = parser.Parse(reader);
    var summaries = analyzer.Summarize(parsedIssues);
    var rankedIssues = ranker.Rank(summaries, limit);

    var metadata = new ReportMetadata(
        GeneratedAtUtc: DateTimeOffset.UtcNow,
        Version: GetVersion(),
        InputSource: input.FullName,
        OutputDirectory: output.FullName,
        AnalyseOnly: analyseOnly,
        Limit: limit,
        GitBranch: gitBranch,
        AiTool: aiTool,
        AiModel: aiModel,
        ParsedIssueCount: parsedIssues.Count,
        SummaryCount: summaries.Count,
        RankedCount: rankedIssues.Count);

    writer.Write(output.FullName, metadata, rankedIssues);

    Console.WriteLine($"  Parsed   : {parsedIssues.Count}");
    Console.WriteLine($"  Summaries: {summaries.Count}");
    Console.WriteLine($"  Ranked   : {rankedIssues.Count}");
    Console.WriteLine($"  Wrote    : {Path.Combine(output.FullName, "issues.json")}");
    Console.WriteLine($"  Wrote    : {Path.Combine(output.FullName, "summary.md")}");
    Console.WriteLine($"  Wrote    : {Path.Combine(output.FullName, "summary.html")}");
});

return await rootCommand.InvokeAsync(args);

static string GetVersion() =>
    Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "0.0.0";

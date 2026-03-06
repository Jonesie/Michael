using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using Michael.Analysis;
using Michael.Cli;
using Michael.Cli.Models;
using Michael.Fixes;
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
analyseOnlyOption.AddAlias("--analysis-only");

var limitOption = new Option<int?>(
    name: "--limit",
    description: "Maximum number of issues to include in the report.");

var rootCommand = new RootCommand("Michael – build log analyser and issue reporter.")
{
    inputOption,
    outputOption,
    analyseOnlyOption,
    limitOption,
};

rootCommand.SetHandler((InvocationContext context) =>
{
    var input       = context.ParseResult.GetValueForOption(inputOption);
    var output      = context.ParseResult.GetValueForOption(outputOption);
    var analyseOnly = context.ParseResult.GetValueForOption(analyseOnlyOption);
    var limit       = context.ParseResult.GetValueForOption(limitOption);

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
    var generateFixes = !analyseOnly;

    PrintBanner(
        GetVersion(),
        input.Name,
        output.FullName,
        limit,
        generateFixes);

    using var stream = input.OpenRead();
    using var reader = new StreamReader(stream);

    var parser = new DotnetBuildLogParser();
    var analyzer = new DeterministicIssueAnalyzer();
    var ranker = new DeterministicIssueRanker();
    var writer = new FileReportWriter();

    var parsedIssues = parser.Parse(reader);
    var summaries = analyzer.Summarize(parsedIssues);
    var rankedIssues = ranker.Rank(summaries, limit);

    IReadOnlyDictionary<int, string> fixScriptFileNamesByRank = new Dictionary<int, string>();
    if (generateFixes)
    {
        var fixScriptGenerator = new CopilotCliFixScriptGenerator();
        fixScriptFileNamesByRank = fixScriptGenerator.Generate(output.FullName, rankedIssues);
    }

    var metadata = new ReportMetadata(
        GeneratedAtUtc: DateTimeOffset.UtcNow,
        Version: GetVersion(),
        InputSource: input.FullName,
        OutputDirectory: output.FullName,
        AnalyseOnly: analyseOnly,
        Limit: limit,
        ParsedIssueCount: parsedIssues.Count,
        SummaryCount: summaries.Count,
        RankedCount: rankedIssues.Count);

    writer.Write(output.FullName, metadata, rankedIssues, fixScriptFileNamesByRank);

    Console.WriteLine($"  Parsed   : {parsedIssues.Count}");
    Console.WriteLine($"  Summaries: {summaries.Count}");
    Console.WriteLine($"  Ranked   : {rankedIssues.Count}");
    Console.WriteLine($"  Fixes    : {fixScriptFileNamesByRank.Count}");
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

static void PrintBanner(
    string version,
    string inputName,
    string outputDirectory,
    int? limit,
    bool generateFixes)
{
    Console.WriteLine("  ███╗   ███╗██╗ ██████╗██╗  ██╗ █████╗ ███████╗██╗     ");
    Console.WriteLine("  ████╗ ████║██║██╔════╝██║  ██║██╔══██╗██╔════╝██║     ");
    Console.WriteLine("  ██╔████╔██║██║██║     ███████║███████║█████╗  ██║     ");
    Console.WriteLine("  ██║╚██╔╝██║██║██║     ██╔══██║██╔══██║██╔══╝  ██║     ");
    Console.WriteLine("  ██║ ╚═╝ ██║██║╚██████╗██║  ██║██║  ██║███████╗███████╗");
    Console.WriteLine("  ╚═╝     ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝");
    Console.WriteLine("  GitHub: https://github.com/Jonesie/Michael");
    Console.WriteLine();
    Console.WriteLine($"  Michael {version}");
    Console.WriteLine($"  Analysing: {inputName}");
    Console.WriteLine($"  Output   : {outputDirectory}");
    if (limit.HasValue) Console.WriteLine($"  Limit    : {limit}");
    Console.WriteLine($"  Generate fixes: {generateFixes}");
    Console.WriteLine();
}

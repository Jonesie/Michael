using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Text.Json;
using Michael.Analysis;
using Michael.Cli;
using Michael.Cli.Models;
using Michael.Cli.Serialization;
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
    getDefaultValue: static () => 10,
    description: "Maximum number of issues to include in the report. Values less than 1 are treated as unlimited.");

var configOption = new Option<FileInfo?>(
    name: "--config",
    description: "Path to CLI JSON config file. Defaults to 'michael.config.json' next to the executable.");

var clearExistingOutputOption = new Option<bool>(
    name: "--clear-existing-output",
    description: "Automatically clear existing files in the output directory before writing reports.");

var rootCommand = new RootCommand("Michael – build log analyser and issue reporter.")
{
    inputOption,
    outputOption,
    analyseOnlyOption,
    limitOption,
    configOption,
    clearExistingOutputOption,
};

rootCommand.SetHandler((InvocationContext context) =>
{
    var input       = context.ParseResult.GetValueForOption(inputOption);
    var output      = context.ParseResult.GetValueForOption(outputOption);
    var analyseOnly = context.ParseResult.GetValueForOption(analyseOnlyOption);
    var limit       = context.ParseResult.GetValueForOption(limitOption);
    var configPath  = context.ParseResult.GetValueForOption(configOption);
    var clearExistingOutput = context.ParseResult.GetValueForOption(clearExistingOutputOption);

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

    output ??= new DirectoryInfo("out");

    if (!PrepareOutputDirectory(output, clearExistingOutput, out var outputPrepareError))
    {
        Console.Error.WriteLine($"Error: {outputPrepareError}");
        context.ExitCode = 1;
        return;
    }

    var generateFixes = !analyseOnly;
    var effectiveConfigPath = GetEffectiveConfigPath(configPath);
    var appConfig = LoadAppConfig(configPath, out var configError);

    if (configError is not null)
    {
        Console.Error.WriteLine($"Error: {configError}");
        context.ExitCode = 1;
        return;
    }

    var fixScriptTemplatePath = "(not used in --analyse-only mode)";
    var fixScriptFileExtension = ".ps1";
    string? fixScriptTemplateText = null;

    if (generateFixes)
    {
        var resolvedTemplatePath = ResolveFixScriptTemplatePath(
            appConfig,
            effectiveConfigPath,
            out var templateResolveError);

        if (templateResolveError is not null)
        {
            Console.Error.WriteLine($"Error: {templateResolveError}");
            context.ExitCode = 1;
            return;
        }

        fixScriptTemplatePath = resolvedTemplatePath!;
        fixScriptFileExtension = DetermineFixScriptExtension(fixScriptTemplatePath);
        fixScriptTemplateText = LoadFixScriptTemplate(fixScriptTemplatePath, out var templateLoadError);
        if (templateLoadError is not null)
        {
            Console.Error.WriteLine($"Error: {templateLoadError}");
            context.ExitCode = 1;
            return;
        }
    }

    PrintBanner(
        GetVersion(),
        input.Name,
        output.FullName,
        limit,
        generateFixes,
        fixScriptTemplatePath!);

    var logContent = File.ReadAllText(input.FullName);

    var parser = new DotnetBuildLogParser();
    var toolDetector = new BuildToolDetector();
    var analyzer = new DeterministicIssueAnalyzer();
    var ranker = new DeterministicIssueRanker();
    var writer = new FileReportWriter();

    var parsedIssues = parser.Parse(logContent);
    var detectedTools = toolDetector.Detect(logContent);
    var summaries = analyzer.Summarize(parsedIssues);
    var rankedIssues = ranker.Rank(summaries, limit);

    IReadOnlyDictionary<int, string> fixScriptFileNamesByRank = new Dictionary<int, string>();
    if (generateFixes)
    {
        var fixScriptGenerator = new TemplateFixScriptGenerator();
        fixScriptFileNamesByRank = fixScriptGenerator.Generate(
            output.FullName,
            rankedIssues,
            fixScriptTemplateText!,
            fixScriptFileExtension);
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
        RankedCount: rankedIssues.Count,
        DetectedTools: detectedTools);

    writer.Write(output.FullName, metadata, rankedIssues, fixScriptFileNamesByRank);

    Console.WriteLine($"  Detected : {(detectedTools.Count == 0 ? "(none)" : string.Join(", ", detectedTools))}");
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
    bool generateFixes,
    string fixScriptTemplatePath)
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
    if (limit.HasValue)
    {
        var limitDisplay = limit.Value < 1 ? "unlimited" : limit.Value.ToString();
        Console.WriteLine($"  Limit    : {limitDisplay}");
    }
    Console.WriteLine($"  Generate fixes: {generateFixes}");
    Console.WriteLine($"  Fix template  : {fixScriptTemplatePath}");
    Console.WriteLine();
}

static string GetEffectiveConfigPath(FileInfo? configuredPath)
{
    return configuredPath?.FullName
        ?? Path.Combine(AppContext.BaseDirectory, "michael.config.json");
}

static AppConfig LoadAppConfig(FileInfo? configuredPath, out string? error)
{
    error = null;

    var fullPath = GetEffectiveConfigPath(configuredPath);

    if (!File.Exists(fullPath))
    {
        return AppConfig.Default;
    }

    try
    {
        using var stream = File.OpenRead(fullPath);
        var parsed = JsonSerializer.Deserialize(stream, MichaelCliJsonContext.Default.AppConfig);

        return NormalizeConfig(parsed);
    }
    catch (Exception exception)
    {
        error = $"failed to read config '{fullPath}': {exception.Message}";
        return AppConfig.Default;
    }
}

static AppConfig NormalizeConfig(AppConfig? raw)
{
    if (raw is null)
    {
        return AppConfig.Default;
    }

    var scriptTemplateFile = string.IsNullOrWhiteSpace(raw.Fixes?.ScriptTemplateFile)
        ? AppConfig.DefaultFixScriptTemplateFile
        : raw.Fixes.ScriptTemplateFile.Trim();

    return new AppConfig(new FixGenerationConfig(scriptTemplateFile));
}

static string? ResolveFixScriptTemplatePath(AppConfig appConfig, string effectiveConfigPath, out string? error)
{
    error = null;

    var configuredTemplatePath = string.IsNullOrWhiteSpace(appConfig.Fixes.ScriptTemplateFile)
        ? AppConfig.DefaultFixScriptTemplateFile
        : appConfig.Fixes.ScriptTemplateFile.Trim();

    string resolvedPath;
    if (Path.IsPathRooted(configuredTemplatePath))
    {
        resolvedPath = configuredTemplatePath;
    }
    else if (string.Equals(configuredTemplatePath, AppConfig.DefaultFixScriptTemplateFile, StringComparison.Ordinal))
    {
        resolvedPath = Path.Combine(AppContext.BaseDirectory, configuredTemplatePath);
    }
    else
    {
        resolvedPath = Path.Combine(Path.GetDirectoryName(effectiveConfigPath) ?? AppContext.BaseDirectory, configuredTemplatePath);
    }

    resolvedPath = Path.GetFullPath(resolvedPath);

    if (!File.Exists(resolvedPath))
    {
        error = $"fix script template file not found: {resolvedPath}";
        return null;
    }

    return resolvedPath;
}

static string? LoadFixScriptTemplate(string path, out string? error)
{
    error = null;

    try
    {
        return File.ReadAllText(path);
    }
    catch (Exception exception)
    {
        error = $"failed to read fix script template '{path}': {exception.Message}";
        return null;
    }
}

static string DetermineFixScriptExtension(string templatePath)
{
    var fileName = Path.GetFileName(templatePath);
    const string templateSuffix = ".template";

    if (fileName.EndsWith(templateSuffix, StringComparison.OrdinalIgnoreCase))
    {
        var withoutTemplateSuffix = fileName[..^templateSuffix.Length];
        var extension = Path.GetExtension(withoutTemplateSuffix);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }
    }

    return ".ps1";
}

static bool PrepareOutputDirectory(DirectoryInfo outputDirectory, bool clearExistingOutput, out string? error)
{
    error = null;

    try
    {
        if (!outputDirectory.Exists)
        {
            outputDirectory.Create();
            return true;
        }

        var hasExistingEntries = outputDirectory.EnumerateFileSystemInfos().Any();
        if (!hasExistingEntries)
        {
            return true;
        }

        if (clearExistingOutput)
        {
            ClearDirectoryContents(outputDirectory);
            return true;
        }

        Console.Write($"Output directory '{outputDirectory.FullName}' contains existing files. Clear it before continuing? [y/N]: ");
        var response = Console.ReadLine();
        if (!IsAffirmative(response))
        {
            error = "run cancelled because output directory has existing files. Re-run with --clear-existing-output to clear automatically.";
            return false;
        }

        ClearDirectoryContents(outputDirectory);
        return true;
    }
    catch (Exception exception)
    {
        error = $"failed to prepare output directory '{outputDirectory.FullName}': {exception.Message}";
        return false;
    }
}

static void ClearDirectoryContents(DirectoryInfo directory)
{
    foreach (var entry in directory.EnumerateFileSystemInfos())
    {
        if (entry is DirectoryInfo childDirectory)
        {
            childDirectory.Delete(recursive: true);
            continue;
        }

        entry.Delete();
    }
}

static bool IsAffirmative(string? input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        return false;
    }

    var normalized = input.Trim();
    return string.Equals(normalized, "y", StringComparison.OrdinalIgnoreCase)
        || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase);
}

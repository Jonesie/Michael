using System.CommandLine;
using System.Linq;
using System.CommandLine.Invocation;
using System.IO.Compression;
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

var limitFixFilesOption = new Option<int?>(
    name: "--limitfixfiles",
    getDefaultValue: static () => null,
    description: "Maximum number of distinct files to include in generated fix prompts. Values less than 1 are treated as unlimited.");

var configOption = new Option<FileInfo?>(
    name: "--config",
    description: "Path to CLI JSON config file. Defaults to 'michael.config.json' next to the executable.");

var templateFileOption = new Option<FileInfo?>(
    name: "--template-file",
    description: "Path to a fix-script template file. When provided this overrides the configured template.");

var clearExistingOutputOption = new Option<bool>(
    name: "--clear-existing-output",
    description: "Automatically clear existing files in the output directory before writing reports.");

var zipOption = new Option<bool>(
    name: "--zip",
    description: "Create fixes.zip in the output directory containing generated fix files.");

var ciOption = new Option<bool>(
    name: "--ci",
    description: "Run in CI-friendly mode: skip ASCII banner and reduce summary verbosity.");

// Support raw `--version` early to avoid System.CommandLine duplicate-option edge cases
if (args.Contains("--version") || args.Contains("-v"))
{
    Console.WriteLine(GetVersion());
    Environment.Exit(0);
}

var rootCommand = new RootCommand("Michael – build log analyser and issue reporter.")
{
    inputOption,
    outputOption,
    analyseOnlyOption,
    limitOption,
    limitFixFilesOption,
    templateFileOption,
    configOption,
    clearExistingOutputOption,
    zipOption,
    ciOption,
};

rootCommand.SetHandler((InvocationContext context) =>
{
    var input       = context.ParseResult.GetValueForOption(inputOption);
    var output      = context.ParseResult.GetValueForOption(outputOption);
    var analyseOnly = context.ParseResult.GetValueForOption(analyseOnlyOption);
    var limit       = context.ParseResult.GetValueForOption(limitOption);
    var limitFixFiles = context.ParseResult.GetValueForOption(limitFixFilesOption);
    var templateFile = context.ParseResult.GetValueForOption(templateFileOption);
    var configPath  = context.ParseResult.GetValueForOption(configOption);
    var clearExistingOutput = context.ParseResult.GetValueForOption(clearExistingOutputOption);
    var createZip = context.ParseResult.GetValueForOption(zipOption);
    var ci = context.ParseResult.GetValueForOption(ciOption);

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
        if (templateFile is not null)
        {
            if (!templateFile.Exists)
            {
                Console.Error.WriteLine($"Error: template file not found: {templateFile.FullName}");
                context.ExitCode = 1;
                return;
            }

            fixScriptTemplatePath = templateFile.FullName;
            fixScriptFileExtension = DetermineFixScriptExtension(fixScriptTemplatePath);
            fixScriptTemplateText = LoadFixScriptTemplate(fixScriptTemplatePath, out var templateLoadError2);
            if (templateLoadError2 is not null)
            {
                Console.Error.WriteLine($"Error: {templateLoadError2}");
                context.ExitCode = 1;
                return;
            }
        }
        else
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
    }

    PrintBanner(
        GetVersion(),
        input.Name,
        output.FullName,
        limit,
        limitFixFiles,
        generateFixes,
        fixScriptTemplatePath!,
        ci);

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
    string? zipFilePath = null;
    string? tempFixOutputDirectory = null;

    try
    {
        if (generateFixes)
        {
            var fixScriptGenerator = new TemplateFixScriptGenerator();
            var fixOutputDirectory = output.FullName;

            if (createZip)
            {
                tempFixOutputDirectory = Path.Combine(Path.GetTempPath(), $"michael-fixes-{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempFixOutputDirectory);
                fixOutputDirectory = tempFixOutputDirectory;
            }

            fixScriptFileNamesByRank = fixScriptGenerator.Generate(
                fixOutputDirectory,
                rankedIssues,
                fixScriptTemplateText!,
                fixScriptFileExtension,
                limitFixFiles);
        }

        if (createZip && generateFixes)
        {
            var candidateZipFilePath = Path.Combine(output.FullName, "fixes.zip");

            if (!TryCreateFixesZip(tempFixOutputDirectory ?? output.FullName, candidateZipFilePath, out var zipError))
            {
                Console.Error.WriteLine($"Error: {zipError}");
                context.ExitCode = 1;
                return;
            }

            zipFilePath = File.Exists(candidateZipFilePath) ? candidateZipFilePath : null;
        }
    }
    finally
    {
        if (tempFixOutputDirectory is not null && Directory.Exists(tempFixOutputDirectory))
        {
            Directory.Delete(tempFixOutputDirectory, recursive: true);
        }
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
        DetectedTools: detectedTools,
        FixesZipFile: zipFilePath is null ? null : Path.GetFileName(zipFilePath),
        Ci: ci);

    writer.Write(output.FullName, metadata, rankedIssues, fixScriptFileNamesByRank);

    Console.WriteLine($"  Detected : {(detectedTools.Count == 0 ? "(none)" : string.Join(", ", detectedTools))}");
    Console.WriteLine($"  Parsed   : {parsedIssues.Count}");
    Console.WriteLine($"  Summaries: {summaries.Count}");
    Console.WriteLine($"  Ranked   : {rankedIssues.Count}");
    Console.WriteLine($"  Fixes    : {fixScriptFileNamesByRank.Count}");
    Console.WriteLine($"  Wrote    : {Path.Combine(output.FullName, "issues.json")}");
    Console.WriteLine($"  Wrote    : {Path.Combine(output.FullName, "summary.md")}");
    Console.WriteLine($"  Wrote    : {Path.Combine(output.FullName, "summary.html")}");
    if (zipFilePath is not null)
    {
        Console.WriteLine($"  Wrote    : {zipFilePath}");
    }
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
    int? limitFixFiles,
    bool generateFixes,
    string fixScriptTemplatePath,
    bool ci)
{
    if (!ci)
    {
        Console.WriteLine("  ███╗   ███╗██╗ ██████╗██╗  ██╗ █████╗ ███████╗██╗     ");
        Console.WriteLine("  ████╗ ████║██║██╔════╝██║  ██║██╔══██╗██╔════╝██║     ");
        Console.WriteLine("  ██╔████╔██║██║██║     ███████║███████║█████╗  ██║     ");
        Console.WriteLine("  ██║╚██╔╝██║██║██║     ██╔══██║██╔══██║██╔══╝  ██║     ");
        Console.WriteLine("  ██║ ╚═╝ ██║██║╚██████╗██║  ██║██║  ██║███████╗███████╗");
        Console.WriteLine("  ╚═╝     ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝");
        Console.Write("  ");
    }

    Console.WriteLine("GitHub: https://github.com/Jonesie/Michael");
    Console.WriteLine($"  Michael {version}");
    Console.WriteLine($"  Analysing: {inputName}");
    Console.WriteLine($"  Output   : {outputDirectory}");
    if (limit.HasValue)
    {
        var limitDisplay = limit.Value < 1 ? "unlimited" : limit.Value.ToString();
        Console.WriteLine($"  Limit    : {limitDisplay}");
    }
    if (limitFixFiles.HasValue)
    {
        var limitDisplay = limitFixFiles.Value < 1 ? "unlimited" : limitFixFiles.Value.ToString();
        Console.WriteLine($"  Fix file limit: {limitDisplay}");
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

static bool TryCreateFixesZip(string fixesDirectory, string zipFilePath, out string? error)
{
    error = null;

    try
    {
        var fixFiles = Directory
            .EnumerateFiles(fixesDirectory, "fix-rank-*")
            .Where(file => !string.Equals(Path.GetExtension(file), ".zip", StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToList();

        if (fixFiles.Count == 0)
        {
            return true;
        }

        if (File.Exists(zipFilePath))
        {
            File.Delete(zipFilePath);
        }

        using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
        foreach (var fixFile in fixFiles)
        {
            archive.CreateEntryFromFile(fixFile, Path.GetFileName(fixFile), CompressionLevel.Optimal);
        }

        return true;
    }
    catch (Exception exception)
    {
        error = $"failed to create fixes.zip: {exception.Message}";
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

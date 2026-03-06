
```text
	███╗   ███╗██╗ ██████╗██╗  ██╗ █████╗ ███████╗██╗     
	████╗ ████║██║██╔════╝██║  ██║██╔══██╗██╔════╝██║     
	██╔████╔██║██║██║     ███████║███████║█████╗  ██║     
	██║╚██╔╝██║██║██║     ██╔══██║██╔══██║██╔══╝  ██║     
	██║ ╚═╝ ██║██║╚██████╗██║  ██║██║  ██║███████╗███████╗
	╚═╝     ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝
```

CLI-first .NET tool to parse build logs, summarize issues, rank them, and write local reports.

Supports .NET build logs only and uses Copilot CLI only for generated fix scripts.

## Prerequisites

- .NET SDK 10.0+
- Linux/macOS/Windows shell

## Build and Test

- Restore dependencies:
	- `dotnet restore src/Michael.sln`
- Build solution:
	- `dotnet build src/Michael.sln`
- Run tests:
	- `dotnet test src/Michael.sln`

## CLI Usage

- Help:
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --help`
- Version:
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --version`

### Analyse a build log

- Example with provided fixture:
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out --analyse-only`
- Example with result limit:
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out --analyse-only --limit 5`
- Example generating fix scripts (default behavior):
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out`

### Output files

After a successful run, the output directory contains:

- `issues.json` – machine-readable metadata and ranked issues.
- `summary.md` - Markdown summary with a ranked table and a single `Details` column per row.
- `summary.html` - preview-friendly interactive report with the same ranked data.
- `fix-rank-<n>.ps1` - one PowerShell script per ranked issue by default (not generated when using `--analyse-only`) that calls `copilot` with issue context.

### Report details behavior

- `Details` column format:
	- `Error Message` heading + truncated issue message.
	- expandable `Files` section (collapsed by default).
	- `Fix` section with generated fix script file name (or `(not generated)` in `--analyse-only` mode).
- File entries are clickable links using VS Code URI schema (`vscode://file/...`).
- When line/column data exists in logs, links include location suffixes (for example `:127:23`).
- `summary.html` is recommended when you want stable expand/collapse behavior while opening links.

## CLI Options

- `--input <file>`: required path to build log.
- `--output <dir>`: output directory (default: `out`).
- `--analyse-only` / `--analysis-only`: run parse/analyze/rank/report flow without generating fix scripts.
- `--limit <n>`: maximum number of ranked issues written (`n > 0`).

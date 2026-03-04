
```text
 __  __ _      _                _
|  \/  (_) ___| |__   __ _  ___| |
| |\/| | |/ __| '_ \ / _` |/ _ \ |
| |  | | | (__| | | | (_| |  __/ |
|_|  |_|_|\___|_| |_|\__,_|\___|_|
```

CLI-first .NET tool to parse build logs, summarize issues, rank them, and write local reports.

## MVP Status

- Implemented: parse (.NET logs), summarize, rank, and write reports (`issues.json`, `summary.md`).
- Implemented: CLI options `--help`, `--version`, `--input`, `--output`, `--analyse-only`, `--limit`, `--git-branch`, `--ai-tool`, `--ai-model`.
- MVP limitation: `--apply-fixes` is intentionally blocked and returns a post-MVP message.
- Current parsing scope: .NET build logs only (Angular/React parsing is out of MVP scope).

## Prerequisites

- .NET SDK 9.0+
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

### Analyze a build log

- Example with provided fixture:
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out --analyse-only`
- Example with result limit:
	- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out --analyse-only --limit 5`

### Output files

After a successful run, the output directory contains:

- `issues.json` – machine-readable metadata and ranked issues.
- `summary.md` – human-readable report with metadata and ranked table.

## CLI Options

- `--input <file>`: required path to build log.
- `--output <dir>`: output directory (default: `out`).
- `--analyse-only`: run parse/analyze/rank/report flow without fixes.
- `--apply-fixes`: blocked in MVP; exits with message and code `1`.
- `--limit <n>`: max number of ranked issues written (`n > 0`).
- `--git-branch <name>`: included in report metadata.
- `--ai-tool <name>`: included in report metadata.
- `--ai-model <name>`: included in report metadata.

## Post-MVP Fix Flow (Planned)

Planned later phases will add safe fix generation and apply flow:

1. Generate fix candidates for selected ranked issues.
2. Preview changes before apply.
3. Optionally apply fixes in bulk (branch-aware).

Until then, use reports to prioritize manual remediation.

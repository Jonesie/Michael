
```text
	███╗   ███╗██╗ ██████╗██╗  ██╗ █████╗ ███████╗██╗     
	████╗ ████║██║██╔════╝██║  ██║██╔══██╗██╔════╝██║     
	██╔████╔██║██║██║     ███████║███████║█████╗  ██║     
	██║╚██╔╝██║██║██║     ██╔══██║██╔══██║██╔══╝  ██║     
	██║ ╚═╝ ██║██║╚██████╗██║  ██║██║  ██║███████╗███████╗
	╚═╝     ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝
```

Michael is a CLI-first .NET build diagnostics assistant for large or noisy build outputs.

Releases include prebuilt platform binaries (Linux, macOS, and Windows) so you can download and run Michael directly without building from source.

It ingests .NET build logs, groups repeated warnings and errors into deterministic issue summaries, ranks issues by impact, and writes machine-readable plus human-readable reports you can review quickly.

When fix generation is enabled, Michael also creates one script per ranked issue that sends structured context to your chosen AI CLI command (configured in `michael.config.json` shipped with the CLI binary) so you can apply focused fixes in a controlled, scriptable workflow.

Current analysis scope is .NET build logs only.

Default fix script templates use the GitHub Copilot CLI, but you can customize the command and template format to work with any AI CLI tool that accepts structured input.

## Author

Peter G. Jones (New Zealand)

## Install

- Download latest release: [https://github.com/Jonesie/Michael/releases/latest](https://github.com/Jonesie/Michael/releases/latest)
- Download the latest release asset for your platform.
	- Linux x64: `michael-<tag>-linux-x64.tar.gz`
	- macOS arm64: `michael-<tag>-osx-arm64.tar.gz`
	- Windows x64: `michael-<tag>-win-x64.zip`
	- (`<tag>` is the GitHub release tag, for example `v1.2.3`)
- Extract the archive.
- Run the `michael` binary (or `michael.exe` on Windows).

## Usage

- Help:
	- `michael --help`
- Version:
	- `michael --version`

### Analyse a build log

- Example with provided fixture:
	- `michael --input data/build.log --output out --analyse-only`
- Example with automatic output cleanup:
	- `michael --input data/build.log --output out --analyse-only --clear-existing-output`
- Example with result limit:
	- `michael --input data/build.log --output out --analyse-only --limit 5`
- Example generating fix scripts (default behavior):
	- `michael --input data/build.log --output out`

### Output files

After a successful run, the output directory contains:

- `issues.json` – machine-readable metadata and ranked issues.
- `summary.md` - Markdown summary with a ranked table and a single `Details` column per row.
- `summary.html` - preview-friendly interactive report with the same ranked data.
- `fix-rank-<n>.<ext>` - one script per ranked issue (not generated when using `--analyse-only`), where `<ext>` is derived from the template filename (for example `.ps1` or `.sh`).

Metadata now includes detected build tools/frameworks (for example `.NET SDK 10.0.100`, `.NET`, `C#`) inferred from the input build log, and this is shown both in CLI console output and in summary reports.

If the output directory already contains files, Michael asks for confirmation before clearing them. Use `--clear-existing-output` to skip the prompt and clear automatically.

### Fix script template configuration

- Default config file path: `michael.config.json` next to the executable (included in release packages).
- Override config path with `--config <file>`.
- Configure the fix script template path at `fixes.scriptTemplateFile`.
- Built-in templates:
	- PowerShell: `templates/fix-script.ps1.template`
	- Bash: `templates/fix-script.sh.template`
- Script templates support placeholders: `[[issueDetails]]`, `[[fileList]]`, and `[[samples]]` (plus `[[rank]]`, `[[targetFileCount]]`).
- The Copilot command line is hardcoded in each template (`$Prompt` in PowerShell, `$prompt` in Bash).

Example `michael.config.json`:

```json
{
	"fixes": {
		"scriptTemplateFile": "templates/fix-script.sh.template"
	}
}
```

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
- `--config <file>`: optional path to a CLI JSON config file.
- `--clear-existing-output`: automatically clear existing files in the output directory before writing new results.

## Development

### Prerequisites

- .NET SDK 10.0+
- Linux/macOS/Windows shell

### Build and Test from Source

- Restore dependencies:
	- `dotnet restore src/Michael.sln`
- Build solution:
	- `dotnet build src/Michael.sln`
- Run tests:
	- `dotnet test src/Michael.sln`

### Run from Source

- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --help`

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2026 Peter G. Jones, New Zealand.

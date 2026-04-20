
```text
	███╗   ███╗██╗ ██████╗██╗  ██╗ █████╗ ███████╗██╗     
	████╗ ████║██║██╔════╝██║  ██║██╔══██╗██╔════╝██║     
	██╔████╔██║██║██║     ███████║███████║█████╗  ██║     
	██║╚██╔╝██║██║██║     ██╔══██║██╔══██║██╔══╝  ██║     
	██║ ╚═╝ ██║██║╚██████╗██║  ██║██║  ██║███████╗███████╗
	╚═╝     ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝
```

Michael turns noisy .NET build logs into a focused, ranked action plan in seconds.

Key features:

- Parses .NET build logs and groups repeated warnings and errors deterministically.
- Ranks issues by impact so you can fix the most important problems first.
- Generates clear artifacts: `issues.json`, `summary.md`, and `summary.html`.
- Optionally creates AI-ready fix scripts per ranked issue for controlled remediation.
- Runs as a CLI and as a GitHub Action for local and CI workflows.
- Ships prebuilt binaries for Linux, macOS, and Windows.

Current analysis scope is .NET build logs only. Default fix script templates use the GitHub Copilot CLI, but you can customize templates and command format to work with other AI CLIs.

## Quick Start

Follow this flow for a first run in a few minutes.

### 1) Download and install

- Download latest release: [https://github.com/Jonesie/Michael/releases/latest](https://github.com/Jonesie/Michael/releases/latest)
	- Linux x64: `michael-<tag>-linux-x64.tar.gz`
	- macOS arm64: `michael-<tag>-osx-arm64.tar.gz`
	- Windows x64: `michael-<tag>-win-x64.zip`
	- (`<tag>` is the GitHub release tag, for example `v1.2.3`)
- Extract the archive.
- Run the `Michael` binary (or `Michael.exe` on Windows).

### 2) Run a build and capture a log

- macOS/Linux:
	- `dotnet build | tee build.log`
- PowerShell:
	- `dotnet build *>&1 | Tee-Object -FilePath build.log`

### 3) Run Michael

- Analyse only:
	- `Michael --input build.log --output out --analyse-only`
- Analyse and clear existing output automatically:
	- `Michael --input build.log --output out --analyse-only --clear-existing-output`
- Analyse with a result limit:
	- `Michael --input build.log --output out --analyse-only --limit 5`
- Generate fix scripts (default mode):
	- `Michael --input build.log --output out`
- Generate fix scripts and bundle them:
	- `Michael --input build.log --output out --zip`

## Usage

- Help:
	- `Michael --help`
	- Version:
		- `Michael --version` (or `-v`) — prints the raw CLI version string and exits.

### Analyse a build log

- Example with provided fixture:
	- `Michael --input data/build.log --output out --analyse-only`
- Example with automatic output cleanup:
	- `Michael --input data/build.log --output out --analyse-only --clear-existing-output`
- Example with result limit:
	- `Michael --input data/build.log --output out --analyse-only --limit 5`
- Example generating fix scripts (default behavior):
	- `Michael --input data/build.log --output out`
- Example generating fix scripts and bundling them:
	- `Michael --input data/build.log --output out --zip`

### Output files

After a successful run, the output directory contains:

- `issues.json` – machine-readable metadata and ranked issues.
- `summary.md` - Markdown summary with a ranked table and a single `Details` column per row.
- `summary.html` - preview-friendly interactive report with the same ranked data.
- `fix-rank-<n>.<ext>` - one script per ranked issue when fix generation is enabled without `--zip`, where `<ext>` is derived from the template filename (for example `.ps1` or `.sh`).
- `fixes.zip` - optional archive created with `--zip`, containing generated `fix-rank-*` files; when this mode is used, individual `fix-rank-*` files are not written to the output directory.

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
- When a ranked issue has more than `20` target files, `[[fileList]]` is replaced with a reference to `fix-rank-<n>-files.txt`, and the full list is written to that file.

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
	- `Fix` section with generated fix script file name (or `(not generated)` in `--analyse-only` mode) when not using `--zip`.
- In `--zip` mode, the `Fix` section is omitted from each row and metadata includes a `Fixes archive` link to `fixes.zip`.
- File entries are clickable links using VS Code URI schema (`vscode://file/...`).
- When line/column data exists in logs, links include location suffixes (for example `:127:23`).
- `summary.html` is recommended when you want stable expand/collapse behavior while opening links.
- In CI mode (`--ci`), file lists in `summary.md` are suppressed and shown as counts only to keep reports compact and machine-friendly.

## CLI Options

- `--input <file>`: required path to build log.
- `--output <dir>`: output directory (default: `out`).
- `--analyse-only` / `--analysis-only`: run parse/analyze/rank/report flow without generating fix scripts.
- `--limit <n>`: maximum number of ranked issues written (default: `10`; values less than `1` are treated as unlimited).
- `--config <file>`: optional path to a CLI JSON config file.
- `--clear-existing-output`: automatically clear existing files in the output directory before writing new results.
- `--zip`: create `fixes.zip` in the output directory containing generated fix files.
- `--ci`: run in CI-friendly mode — skip the ASCII banner and reduce summary verbosity (suppress file links, show counts instead).
- `--version`: print the raw CLI version string and exit (also available as `-v`).

## GitHub Action

Michael is also available as a composite GitHub Action you can use directly in your workflows.


### Run sample in GitHub Actions

Use the dispatch workflow at `.github/workflows/sample-action-dispatch.yml` to build the sample and run Michael end-to-end.

Copy this workflow:

```yaml
name: Sample Action Dispatch

on:
  workflow_dispatch:

permissions:
  contents: read

jobs:
  run-sample-and-michael:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v5

      - name: Setup .NET
        uses: actions/setup-dotnet@v5
        with:
          dotnet-version: '10.0.x'

      - name: Build sample app and capture log
        run: dotnet build samples/sample-warning-app/SampleWarningApp.csproj > sample-build.log 2>&1

      - name: Run Michael action (local)
        id: michael
        uses: ./
        with:
          input: sample-build.log
          output: michael-output
          analyse-only: 'true'
          clear-existing-output: 'true'

      - name: Upload Michael results archive
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: michael-sample-results
          path: ${{ steps.michael.outputs.archive }}
          if-no-files-found: warn
```

Sample run screenshot:

![Sample workflow run](https://github.com/user-attachments/assets/b5976671-a172-4540-b473-f2bd64bef795)


### Action inputs

| Input | Required | Default | Description |
|---|---|---|---|
| `input` | **yes** | | Path to the build log file. |
| `output` | no | `michael-output` | Directory for report files. |
| `analyse-only` | no | `false` | Set to `true` to skip fix-script generation. |
| `limit` | no | | Maximum number of ranked issues (CLI default is `10`; use `0` for unlimited). |
| `config` | no | | Path to a `michael.config.json` file. |
| `template-file` | no | | Path to a fix-script template file (overrides config). |
| `clear-existing-output` | no | `true` | Clear existing output directory before writing. |
| `version` | no | `latest` | Michael release tag to install (e.g. `v1.0.0`). |

### Action outputs

| Output | Description |
|---|---|
| `archive` | Path to a `tar.gz` archive of the output directory. |
| `output-dir` | Path to the output directory. |
| `issues-json` | Path to the `issues.json` file. |

### Workflow summary

The action automatically writes `summary.md` to `$GITHUB_STEP_SUMMARY` so the Michael analysis report appears in the workflow run summary page.

### Fix scripts archive

When fix-script generation is enabled (the default), the action runs Michael with `--zip`, which creates `fixes.zip` in the output directory containing generated fix files. The action also bundles the full output directory into `michael-results.tar.gz`. Use the `archive` output with `actions/upload-artifact` to publish it.

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


## Author

Peter G. Jones (New Zealand)

If Michael helps your team, you can support ongoing development by buying me a coffee:

<a href="https://buymeacoffee.com/jonesie"><img src="buymecoffee.png" alt="Buy me a coffee" width="100"></a>

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2026 Peter G. Jones, New Zealand.

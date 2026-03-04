## Plan: MVP in Small Steps

Build a CLI-first .NET MVP in one month that ingests build logs, parses issues deterministically, summarizes and ranks them, and writes reports to local files. Scope is analysis-only for MVP (parse/analyze/rank/report). `--apply-fixes` remains explicitly blocked as post-MVP. AI support is Copilot-only now, but behind abstractions so other tools/models can be added later. The CLI application/executable name is `Michael`. This follows [docs/prompt.md](docs/prompt.md) as source of truth and includes minimal CI (restore/build/test).

**Steps**
_Status legend: [x] complete, [~] in progress, [ ] not started._

- [x] 1. Bootstrap solution structure and projects under [src](src), [tests](tests), with solution file [src/michael.sln](src/michael.sln) and projects in subfolders such as [src/Michael.Cli](src/Michael.Cli), [src/Michael.Parsing](src/Michael.Parsing), [src/Michael.Analysis](src/Michael.Analysis), [src/Michael.Fixes](src/Michael.Fixes); create `Main` and core interfaces (`IParser`, `IAnalyzer`, `IRanker`, `IReportWriter`).
	- Completed: solution + projects scaffolded, core interfaces added, project references wired, build/test passing.
- [x] 2. Implement CLI contract in [src/Michael.Cli](src/Michael.Cli): `--help`, `--version`, `--input`, `--output`, `--analyse-only`, `--apply-fixes`, `--limit`, `--git-branch`, `--ai-tool`, `--ai-model`; block `--apply-fixes` with a clear post-MVP message.
	- Completed: `System.CommandLine` wired up, all options registered, `--apply-fixes` exits 1 with post-MVP message, `--version` and `--help` work, assembly named `Michael`, build passing.
- [x] 3. Build deterministic parser pipeline in [src/Michael.Parsing](src/Michael.Parsing) for dotnet build logs; normalize to `ParsedIssue` (message, source, optional file path, severity, count); process large logs via streaming.
	- Completed: implemented streaming parser (`TextReader`) for .NET warning/error lines, normalized and deduplicated issues with counts, and added parser unit tests.
- [x] 4. Implement summarization/classification in [src/Michael.Analysis](src/Michael.Analysis) via `IAnalyzer`; group duplicates, map severities, produce concise explanations without requiring AI.
	- Completed: added `DeterministicIssueAnalyzer` that groups duplicate issues, normalizes severities (`error`/`warning`/`info`), computes deterministic confidence, and generates concise non-AI explanations; added analyzer unit tests.
- [ ] 5. Implement ranking service `IRanker` in [src/Michael.Analysis](src/Michael.Analysis); score by severity/frequency/confidence; support `--limit`; define deterministic tie-breakers.
- [ ] 6. Implement outputs: write `issues.json` and `summary.md` to `--output`; include metadata (timestamp, input source, version, options).
- [ ] 7. Add tests in [tests](tests): parser edge cases, classification, ranking stability, CLI validation; add fixture logs in [data](data); add analysis-only integration tests.
- [ ] 8. Update [README.md](README.md) with build/run/test instructions and example analysis workflows; document MVP limits and post-MVP fix flow.
- [ ] 9. Add minimal CI (build/test only) under [.github/workflows](.github/workflows) once solution exists; keep pipeline aligned to local `dotnet restore/build/test`.
- [ ] 10. Post-MVP: implement `IFixGenerator`/`IFixApplier` in [src/Michael.Fixes](src/Michael.Fixes), enable safe preview/apply flow, then activate real `--apply-fixes`.

**Naming Convention**
- Root support folders remain lowercase (for example: [tests](tests), [docs](docs), [data](data), [scripts](scripts)).
- Solution file name is [src/michael.sln](src/michael.sln).
- Project folders follow `Michael.*` convention under [src](src) (for example: [src/Michael.Cli](src/Michael.Cli), [src/Michael.Analysis](src/Michael.Analysis)).
- CLI project file: [src/Michael.Cli/Michael.Cli.csproj](src/Michael.Cli/Michael.Cli.csproj).
- Other assemblies follow the same pattern (for example: `Michael.Parsing`, `Michael.Analysis`, `Michael.Fixes`).
- Assembly and executable name: `Michael`.

**Verification**
- `dotnet restore`
- `dotnet build`
- `dotnet test`
- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --help`
- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --version`
- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out --analyse-only`
- `dotnet run --project src/Michael.Cli/Michael.Cli.csproj -- --input data/build.log --output out --apply-fixes` (expected post-MVP message)

**Decisions**
- MVP excludes automated fix generation/application (deferred).
- Copilot-only now, with provider abstraction (`IAiProvider`) retained.
- Minimal CI included in MVP scope.
- Deterministic analysis is prioritized before AI-assisted behavior.
- Where [README.md](README.md) implies bulk apply, [docs/prompt.md](docs/prompt.md) and your decisions govern MVP scope.

# Project Prompt

## 1) Project Overview
- **Project name:** Michael
- **One-sentence summary:** Analyse .NET build logs, summarize/rank repeated issues, and optionally generate fix scripts for AI-assisted remediation.
- **Who is this for?** Software developers and DevOps engineers working with noisy build output.
- **Primary problem being solved:** Reduce manual effort when triaging large build logs with repeated warnings/errors.

## 2) Goals and Success Criteria
- **Top 3 goals:**
    1. Parse build logs and extract deterministic issue summaries.
    2. Rank issues by impact so users can focus on high-value fixes first.
    3. Produce clear reports and optional fix scripts for controlled follow-up.
- **Success metrics (how we know it works):**
    - Consistent issue grouping and ranking for identical input logs.
    - Report artifacts (`issues.json`, `summary.md`, `summary.html`) generated successfully.
    - Users can run `--analyse-only` for triage-only workflows, or default mode for fix-script generation.

## 3) Scope
### In scope (current)
- .NET build log parsing and analysis.
- Deterministic issue summarization and ranking.
- Optional fix script generation from templates.
- Composite GitHub Action wrapper for CI usage.

### Out of scope (for now)
- Parsing non-.NET build logs.
- Automatically applying fixes to source code.
- Native support for git branch management inside the CLI.

## 4) Core Features
| Feature | Description | Priority |
|---|---|---|
| Read Build Logs | Parse .NET build output into issue records. | P0 |
| Summarise Issues | Group repeated issues and keep representative context. | P0 |
| Rank Issues | Prioritize by severity/frequency/confidence score. | P0 |
| Write Reports | Emit `issues.json`, `summary.md`, and `summary.html`. | P0 |
| Generate Fix Scripts | Create one script per ranked issue using templates. | P1 |

## 5) Users and Workflows
- **User types / personas:** Software developers, CI maintainers, DevOps engineers.
- **Main user journey (step-by-step):**
    1. Provide input build log path (`--input`).
    2. Run analysis with optional controls (`--output`, `--limit`, `--analyse-only`).
    3. Review generated summaries and ranked issues.
    4. If fix scripts were generated, run/edit them manually in a controlled process.
- **Edge cases to handle:**
    - Large logs and repeated issue noise.
    - Existing output directory content (prompt to clear, or `--clear-existing-output`).
    - Invalid option values (for example, negative limit).

## 6) Technical Preferences
- **Preferred language(s):** C#
- **Framework(s):** .NET
- **Storage:** Local file system + JSON artifacts; in-memory runtime state.
- **Hosting/deployment target:** Console app for local runs and GitHub Action for CI.
- **Authentication needs:** None.
- **API integrations (if any):** None required in core runtime.

## 7) Project Constraints
- **Budget:** $0 (open source).
- **Performance target:** Handle logs up to ~100MB in practical local/CI time.
- **Security/compliance:** No special compliance targets; use standard secure coding practices.

## 8) CLI Contract (Current)
- `--help` / `-h`: display usage.
- `--version` / `-v`: display version.
- `--input <file>`: required input build log path.
- `--output <dir>`: output directory (default: `out`).
- `--analyse-only` / `--analysis-only`: analyze and report only, skip fix scripts.
- `--limit <n>`: max ranked issues (default: `10`; `0` means unlimited; negative values invalid).
- `--config <file>`: optional JSON config path for template settings.
- `--clear-existing-output`: clear existing output files without interactive prompt.

## 9) Deliverables From AI
- [X] Folder structure
- [X] Starter/production code
- [X] Tests
- [X] CI/CD wiring
- [X] Documentation

## 10) Definition of Done
- Behavior matches CLI contract and documented options.
- Tests pass successfully.
- Documentation reflects current implemented behavior.
- Output artifacts are generated correctly for both analyse-only and default modes.

## 11) Open Questions
- Should future versions add direct fix application (`--apply-fixes`) or keep script generation only?
- Should non-.NET log formats be added after current stability milestones?

## 12) Next Request Template
"Given the project prompt above, generate the next phase with:
1) architecture updates,
2) dependency changes,
3) exact file tree deltas,
4) implementation code,
5) run/test instructions."
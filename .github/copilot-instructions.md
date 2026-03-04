# Copilot Instructions for `Michael`

## Current project state
- This repository is an early scaffold with directories only: `src/`, `tests/`, `scripts/`, `docs/`, and `data/`.
- There is no application code yet in `src/` and no established build/test pipeline.
- Treat this as a greenfield codebase and keep initial implementations simple and incremental.

## Source of truth
- Use `README.md` for the project headline and intent.
- Use `docs/prompt.md` as the primary product/technical requirements document.
- If `README.md` and `docs/prompt.md` conflict, prefer `docs/prompt.md` and note the mismatch in your response.

## Intended architecture (from requirements)
- Target runtime is a C#/.NET console application.
- Core workflow is: ingest build logs → analyze and summarize issues → rank issues → optionally generate/apply fixes.
- Current scope mentions .NET backend and frontend logs (Angular/React) as input sources.
- Storage is file-based (local files + JSON), with in-memory state during execution.

## Agent workflow expectations
- Before coding, read `docs/prompt.md` and explicitly map requested changes to the MVP scope.
- When creating new code, keep boundaries clear (for example: `src/Parsing`, `src/Analysis`, `src/Fixes`, `src/Cli`).
- Prefer deterministic parsing/analysis steps before invoking AI-assisted fix generation.
- Keep AI provider/model usage behind abstractions so tool/model choices can be swapped.

## CLI and UX conventions to preserve
- This is CLI-first; no web UI unless explicitly requested.
- Preserve/help implement options documented in `docs/prompt.md` (examples: `--help`, `--version`, `--input`, `--output`, `--analyse-only`, `--apply-fixes`, `--limit`, `--git-branch`, `--ai-tool`, `--ai-model`).
- Keep command behavior scriptable and suitable for local automation.

## Validation and deliverables
- Add tests alongside new functionality in `tests/`.
- Prefer small, testable units for parsing and issue classification logic.
- When introducing commands or scripts, document exact usage in `README.md`.
- Do not invent CI/build commands that are not present; add them only when also adding the corresponding config/files.

## What to avoid
- Do not assume an existing solution/project file layout that is not in the repo.
- Do not add heavy infrastructure or services; keep implementation local and file-based unless requirements change.
- Do not silently diverge from `docs/prompt.md` scope; call out deliberate tradeoffs.

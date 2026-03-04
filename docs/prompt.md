# Project Prompt (Fill This In)

## 1) Project Overview
- **Project name:** Michael
- **One-sentence summary:** Analyse software build output to create fixes for common issues and bulk apply these.
- **Who is this for?** Software developers.
- **Primary problem being solved:** Fix legacy systems that are riddled with build warnings and errors.

## 2) Goals and Success Criteria
- **Top 3 goals:**
	1. Analyse build output to identify common issues.
	2. Generate automated fixes for identified issues.
	3. Apply fixes in bulk to improve build stability.
- **Success metrics (how we know it works):**
    - Reduction in build warnings/errors by 80%.
    - Positive feedback from users on the effectiveness of generated fixes.
    - Adoption rate of the tool among target users.

## 3) Scope
### In scope (MVP)
- Build output analysis for dotnet backends and angular, react front ends.
- Only support CoPilot CLI for AI analysis and fix generation.

### Out of scope (for now)
- Support for other languages/frameworks.

## 4) Core Features
List each feature with priority:

| Feature | Description | Priority (P0/P1/P2) |
|---|---|---|
|Read Build Logs | Analyse build output to identify common issues. | P1 |
|Summarise Issues | Provide a summary of identified issues with explanations. | P1 |
|Rank Issues | Prioritise issues based on severity and frequency. | P1 |
|Generate Fixes | Generate automated fixes for identified issues. | P2 |
|Apply Fixes | Apply fixes in bulk to improve build stability. | P3 |

## 5) Users and Workflows
- **User types / personas:** Software developers, DevOps engineers.
- **Main user journey (step-by-step):**
    1. User uploads build logs to the tool.
    2. Tool analyses logs and identifies common issues.
    3. User reviews the summary of issues and their explanations.
    4. User prioritises which issues to fix based on severity and frequency.
    5. User generates automated fixes for selected issues.
    6. User applies fixes in bulk to improve build stability.
- **Edge cases to handle:**
    - Handling large build logs that may exceed upload limits.
    - Dealing with ambiguous or incomplete build output that may lead to incorrect issue identification.
    - Providing options for users to manually review and edit generated fixes before applying them.

## 6) Technical Preferences
- **Preferred language(s):** C#
- **Framework(s):** .Net
- **Database/storage:** Local file system for storing build logs and generated fixes.  Any state management can be done in memory and persisted to disk as needed in JSON format.
- **Hosting/deployment target:** Console application for local use.
- **Authentication needs:** None
- **API integrations (if any):** 
    - Integration with code repositories (e.g., GitHub, GitLab) for applying fixes directly to codebases.

## 7) Project Constraints
- **Timeline / deadline:** 1 month for MVP.
- **Budget limits:** $0 (open source project).
- **Compliance/security requirements:** None specific, but should follow best practices for handling user data and ensuring the security of the application.
- **Performance requirements:** The tool should be able to process build logs of up to 100MB in size within a reasonable time frame (e.g., under 1 minute).

## 8) UI/UX Direction
- **Style references:** Simple and functional, similar to existing build analysis tools.
- **Splash** Splash screen using ascii art of tool name.
- **Accessibility requirements:** Ensure the tool is accessible to users with disabilities, following WCAG guidelines.
- **Must-have CLI Options:** 
    - display help page: -h or --help to display usage instructions and available options.
    - display version: -v or --version to display current version of the tool.
    - specify build log input and output folder for summary & fixes: 
        - -i or --input to specify path to build log file.
        - -o or --output to specify path to output folder for summary and generated fixes.
    - specify which issues to generate fixes for and apply.
        - analyse only mode to identify and summarise issues without generating fixes: --analyse-only
        - apply fixes mode to apply previously generated fixes in bulk : --apply-fixes
        - limit number of fixes to apply - ie, top 3 issues: --apply-fixes --limit 3
    - git branch name - will create new branch for fixes if specified, otherwise will apply fixes to current branch: --git-branch fix-build-issues
    - AI tools options
        - allow users to specify which AI tools to use for analysis and fix generation (e.g., CoPilot-CLI, Claude, custom tool): --ai-tool copilot
        - allow users to specify which AI model to use for analysis and fix generation (e.g., GPT-3, custom model): --ai-model gpt-3

- **User feedback mechanisms:** Provide clear feedback on the analysis process, identified issues, and the
 
## 9) Deliverables You Want From AI
Check or list what you want generated:
- [X] Folder structure
- [X] Boilerplate code
- [X] Tests
- [X] CI/CD setup
- [X] Documentation

## 10) Definition of Done
- Code is complete and meets the specified requirements.
- Code is reviewed and approved by at least one other developer.
- All tests pass successfully.
- Documentation is complete and provides clear instructions for usage.
- The tool is successfully deployed and can be used by target users.

## 11) Open Questions
-

## 12) Next Request for AI
Use this exact format when asking AI to proceed:

"Given the project prompt above, generate phase 1 with:
1) architecture,
2) dependency list,
3) exact file tree,
4) starter code,
5) run instructions."

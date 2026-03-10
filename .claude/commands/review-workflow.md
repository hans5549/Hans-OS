# Review Workflow - Full Review Without Commit

CRITICAL RULE: Each agent step below MUST be executed using the Agent tool.
You are FORBIDDEN from substituting any agent step with your own text summary or analysis.
The post-agent-verify hook will check -- steps only count as complete when Agent tool is actually called.

## Preparation
1. Read `.claude/workflow/state.json` to get the `modifiedFiles` list
2. If no tracked files exist, inform user: "No tracked files. Edit code files first."
3. Run `git diff --name-only` to confirm changed files
4. Prepare a summary of what changed (1-2 sentences per file)

## Step 1: Code Simplifier
Use the Agent tool with these EXACT parameters:
- subagent_type: "code-simplifier:code-simplifier"
- prompt: "Review and simplify the following modified files: [list files]. Focus on: primary constructors, guard clauses, expression bodies, method length (max 20 lines), nesting depth (max 3). Apply changes directly."
- DO NOT do this yourself. You MUST call the Agent tool.
- Wait for the agent to complete before proceeding.

## Step 1.5: Spec Check
This step YOU do directly (no agent needed):
1. Re-read the original user request or plan
2. Compare: Are all requirements implemented? Nothing extra (YAGNI)?
3. Report findings, then say "spec check done"

## Step 2: Code Review
Use the Agent tool with these EXACT parameters:
- subagent_type: "code-review-specialist"
- prompt: "Review these modified files: [list files]. Changes summary: [summary]. Check: code quality, architecture compliance, naming conventions, error handling, potential bugs."
- DO NOT do a review yourself. You MUST call the Agent tool.
- Wait for the agent to complete before proceeding.

## Step 3: Security Review
Use the Agent tool with these EXACT parameters:
- subagent_type: "security-vuln-scanner"
- prompt: "Scan these modified files for security vulnerabilities: [list files]. Check: OWASP Top 10, injection, XSS, auth bypass, sensitive data exposure, insecure configuration."
- DO NOT do a security check yourself. You MUST call the Agent tool.
- Wait for the agent to complete before proceeding.

## Step 4: Linus Review
This step YOU do directly (no agent needed):
1. Read `.claude/LINUS_MODE.md`
2. Apply each criterion to the modified code
3. If all pass, say "Linus Green"
4. If any fail, fix the issues and re-check

## Step 5: Build Verification
1. Build backend: `dotnet build backend/CGMSportFinance.sln`
2. If any `.vue`, `.ts`, or `.tsx` files were modified, also run: `cd frontend && pnpm check:type`
3. Run tests: `dotnet test backend/CGMSportFinance.sln`
4. Report whether build and tests succeeded or failed. Do NOT create a git commit.
5. Use `commit this` or `/commit-this` when ready to commit.

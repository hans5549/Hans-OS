# Commit This - Full Workflow

CRITICAL RULE: Each agent step (Steps 1-5) MUST be executed using the Agent tool.
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
Use the Agent tool with these EXACT parameters:
- subagent_type: "linus-reviewer"
- prompt: "Review these modified files applying Linus Torvalds criteria: [list files]. Check: Good Taste (edge case elimination), Never Break Userspace (backward compatibility), Pragmatism, Simplicity (nesting <= 3, methods <= 20 lines). Hans-OS domain: API endpoints, JWT auth, menu system, user management."
- DO NOT do a Linus review yourself. You MUST call the Agent tool.
- Wait for the agent to complete before proceeding.

## Step 5: gstack Review
Use the Agent tool with these EXACT parameters:
- subagent_type: "gstack-reviewer"
- prompt: "Pre-landing structural review for these modified files: [list files]. Two-pass analysis: CRITICAL (SQL safety, race conditions, trust boundary, async void, shared DbContext) then INFORMATIONAL (magic numbers, dead code, test gaps). Hans-OS domain: API endpoints, JWT auth, menu system, user management. Analyze diff against origin/main."
- DO NOT do this review yourself. You MUST call the Agent tool.
- Wait for the agent to complete before proceeding.

## Step 6: Build & Commit
1. Run: `dotnet build backend/HansOS.slnx`
2. If frontend files (.vue, .ts, .tsx) were modified, also run: `cd frontend && pnpm check:type`
3. Run: `dotnet test backend/HansOS.slnx`
4. If build/test fails, fix and retry
5. If all pass, create a git commit:
   - Stage only modified files (specific paths, never `git add .`)
   - Use Conventional Commits format in Traditional Chinese
   - Example: `feat(auth): 新增 JWT refresh token 自動續期機制`

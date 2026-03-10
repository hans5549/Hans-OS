# Anti-Pattern Check (C# Adapted)

Scan specified files (or recently modified .cs files) for common C# anti-patterns.

## Detection Rules

### Critical (must fix)
- **Empty catch blocks**: `catch { }` or `catch (Exception) { }` with no handling
- **Methods > 20 lines**: Count executable lines (exclude blank lines and comments)
- **Nesting > 3 levels**: Nested if/for/foreach/while beyond 3 levels

### Important (should fix)
- **`== null` instead of `is null`**: Use pattern matching for null checks
- **`""` instead of `string.Empty`**: Use the constant for clarity
- **Missing `Async` suffix**: Async methods not ending in `Async` (except event handlers `On*`)
- **`namespace X {` instead of `namespace X;`**: Use file-scoped namespaces

### Style (nice to have)
- **Unused `using` statements**: Using directives with no references in file
- **Magic numbers**: Numeric literals without named constants (except 0, 1, -1)
- **`var` with non-obvious types**: `var x = GetThing()` where type isn't clear

## Workflow

1. Identify target files (argument or recently modified .cs files from `git diff --name-only`)
2. Read each file and scan for patterns above
3. Classify findings by severity: Critical / Important / Style
4. Report findings with file:line references
5. Offer to fix issues one-by-one with user confirmation
6. Track progress: `[x/total] fixed`

## Usage

```
/anti-pattern-check                    # Scan recently modified .cs files
/anti-pattern-check path/to/File.cs    # Scan specific file
/anti-pattern-check --all              # Scan all .cs files in solution
```

## Output Format

```
## Anti-Pattern Report: FileName.cs

### Critical
- line 42: Empty catch block — add logging or specific handling
- line 87-120: Method `ProcessTicket` is 34 lines (max 20)

### Important
- line 15: `== null` → use `is null`
- line 67: `""` → use `string.Empty`

### Style
- line 3: Unused using `System.Linq`

Summary: 2 critical, 2 important, 1 style
```

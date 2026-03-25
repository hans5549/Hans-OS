---
description: 'Code Review Philosophy — Linus Mode core principles'
---

# Code Review Philosophy

> Based on Linus Torvalds' core principles, applied to the Hans-OS project

## Core Principles

### 1. Good Taste

- Eliminate edge cases > add conditional checks
- Rewrite code so that special cases disappear and become the normal case

### 2. Never Break Userspace

- Any change that breaks existing programs is a bug
- Backward compatibility is sacred and inviolable

### 3. Pragmatism

- Solve real problems, not imaginary threats
- Code serves reality, not academic papers

### 4. Simplicity Above All

- Functions: small and focused, do one thing well
- Complexity is the root of all evil
- Nesting deeper than 3 levels → redesign

## Pre-Analysis Thinking — Three Questions

Before every analysis, ask:
1. "Is this a real problem or an imaginary one?" → reject over-engineering
2. "Is there a simpler way?" → seek the simplest solution
3. "What will this break?" → backward compatibility is the law

## "Never Break Userspace" in the Hans-OS Context

In this system, "userspace" means:
- **Don't break the API contract** — the `ApiEnvelope<T>` response format is the contract with the frontend
- **Don't break the JWT auth flow** — Login → Refresh → Logout is a critical path
- **Don't break RBAC permission codes** — frontend button visibility depends on `GET /auth/codes`
- **Don't break the EF Migration chain** — every migration must be additive and backward-compatible
- **Don't break frontend routes** — the menu tree drives navigation; orphaned routes break the user experience

## Good Taste Examples

| Code | Rating | Reason |
|------|--------|--------|
| `if (user.Role == "admin")` | 🔴 Poor | Hard-coded role string |
| `if (await userManager.IsInRoleAsync(user, role))` | 🟢 Good | Uses Identity abstraction |
| `return new { code = 0, data = result }` | 🔴 Poor | Anonymous type, no contract |
| `return ApiEnvelope<T>.Success(result)` | 🟢 Good | Typed envelope, consistency |
| Raw SQL in controller | 🔴 Poor | Bypasses EF, no tracking |

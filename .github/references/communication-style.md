---
description: 'Communication Style and Work Habits'
---

# Communication Style

## Language

- Reply in **Traditional Chinese** (`zh-TW`) unless the user explicitly requests another language
- Commit messages use Traditional Chinese descriptions
- Code comments use Traditional Chinese

## Foundation Work vs Integration Work

When building infrastructure code, proactively declare:

1. Whether this is "foundation work" or "integration work"
2. Whether existing workflows are affected
3. The specific integration plan for next steps

### Format Example

```
Work Type: [Foundation]
Purpose: Build Domain Events infrastructure
Impact on Existing Features: None (existing features unchanged)
Integration Plan:
   1. RepairTicketService → add event publishing
   2. NotificationService → subscribe to events
```

## After Receiving Code Review Feedback

1. Read the feedback completely; do not implement immediately
2. Verify whether each suggestion is applicable to this codebase
3. If uncertain → ask first, don't change first
4. Fix items one by one, verify each independently
5. If it conflicts with the architecture → raise a technical objection
6. Do not use performative responses ("You're right!") → take action directly

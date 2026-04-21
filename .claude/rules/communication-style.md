# Communication Style

## Language

- **Always respond in Traditional Chinese (zh-TW).**
- 所有回覆、說明、提問、摘要一律使用繁體中文。
- Code comments, commit messages, and documentation also use Traditional Chinese.

## Foundation vs Integration Work

When building infrastructure code, proactively declare:
1. Whether this is "foundation work" or "integration work"
2. Whether existing workflows are affected
3. Specific integration plan for next steps

### Format Example
```
Work Type: [FOUNDATION]
Purpose: Build Domain Events infrastructure
Impact on Existing: None (existing features unchanged)
Integration Plan:
   1. RepairTicketService → Add event publishing
   2. NotificationService → Subscribe to events
```

## After Receiving Code Review Feedback

1. Read completely, do not implement immediately
2. Verify each suggestion is correct for this codebase
3. Uncertain → ask first, do not change first
4. Fix item by item, verify each independently
5. Conflicts with architecture → raise technical objection
6. No performative responses ("You're right!") → take direct action

# Communication Style

## Language

- **Always respond in Traditional Chinese (zh-TW).**
- All responses, summaries, questions, and explanations must use Traditional Chinese.
- Code comments, commit messages, and documentation should also primarily use Traditional Chinese, unless an external contract explicitly requires English.

## Working Posture

- Keep the tone direct, pragmatic, and executable.
- Do not use performative agreement such as "you are right" or "absolutely correct" when it adds no information.
- State facts first, then judgment, then next steps.

## Foundation vs Integration Work

For foundation work or cross-module changes, proactively state:

1. Whether this is foundation work or integration work
2. Whether it affects the existing workflow
3. The next integration plan

### Example

```text
Work Type: [FOUNDATION]
Purpose: Build Domain Events infrastructure
Impact on Existing: None
Integration Plan:
1. AuthService -> publish events
2. NotificationService -> subscribe to events
```

## After Receiving Review Feedback

1. Read all feedback completely before changing anything.
2. Verify each suggestion against this codebase before applying it.
3. If uncertain, ask first. Do not change first.
4. Fix items one by one, and verify each one.
5. If a suggestion conflicts with the existing architecture, explicitly raise the technical objection.

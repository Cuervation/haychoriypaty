---
name: targeted-testing
description: Select proportional validation after a game, visual, instruction or configuration change; do not trigger global Unity tests or APK by default.
---

Map the change to its acceptance criteria and use the smallest meaningful check:

| Change | Check |
|---|---|
| Documentation / agent / skill config | TOML/frontmatter syntax, paths, references and scope diff; no Unity/PlayMode |
| Text, style, color | Inspect the changed value/output; one affected visual check when appearance matters |
| Layout / routes | Geometry checks plus visual review at affected resolution/safe area, not every device |
| Gameplay logic | Focused EditMode domain/regression tests |
| Interaction / input | Focused PlayMode pointer/integration tests |
| Architecture / cross-system behavior | Relevant integration checks; expand only for the affected boundary |

Prefer the existing Unity editor. Do not rerun passing checks without relevant changes, aggregate overlapping runs as unique totals, or run full suites for minor edits. APK and device installation require an explicit request or authorization within the objective.

For each acceptance criterion, report exactly one status: `PASS` (direct evidence supports it), `FAIL` (executed check contradicts it), `BLOCKED` (environment/dependency prevented the check), `NOT_RUN` (not attempted), or `NOT_APPLICABLE` (explain why). Include the smallest useful evidence (test name/result, assertion/output, relevant code location or capture). Timeouts/import/initialization failures are not passes. Do not claim full validation if a mandatory criterion remains `FAIL`, `BLOCKED` or `NOT_RUN`; route demonstrated failures through the bounded correction loop in `token-optimization`, then rerun only affected checks.

For visual tasks, identify evidence type precisely: full Game View, PlayMode/projection-only capture, or physical-device capture. A cropped view or isolated renderer proves only what it shows; do not infer full-screen layout, resolution coverage or device behavior. APK and device claims require their own build/install evidence.

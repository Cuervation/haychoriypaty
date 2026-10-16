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

Prefer the existing Unity editor. Do not rerun passing checks without relevant changes, aggregate overlapping runs as unique totals, or run full suites for minor edits. APK and device installation require an explicit request or authorization within the objective. Report executed/passed, failed and not run separately; timeout/initialization failures are not test passes. Visual/device/performance claims require their own evidence.

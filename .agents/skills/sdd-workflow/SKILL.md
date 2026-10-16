---
name: sdd-workflow
description: Implement or revise a Hay Chori y Paty feature contract or SDD context; avoid documentation churn for purely cosmetic edits.
---

1. Use `specs/README.md` to locate only the owning contract; consult `continuity.md` for current checkpoint/known gaps, not as a second specification. Open architecture only when its boundaries matter.
2. Scope the smallest acceptance slice. For a behavior conflict, compare recent contracts, implementation and serialized configuration; surface unresolved differences rather than restoring an obsolete rule.
3. Implement only authorized behavior and validate proportionally with `targeted-testing`. Documentation/configuration-only work stays static; no Unity runs or APK by default.
4. Update the owning contract when behavior changes; update acceptance/status when completion or evidence changes. Do not create spec copies, paste full requirements into handoffs or log cosmetic-only edits without contractual impact. Record only a non-obvious decision in `specs/decisions.md`.
5. Keep historical validation/decisions with clear dates and supersession links. Pending features remain pending until implementation and relevant validation are evidenced.

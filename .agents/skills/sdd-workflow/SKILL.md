---
name: sdd-workflow
description: Use for implementing or changing a Hay Chori y Paty feature; consult only its authoritative spec and update acceptance/status when outcomes change.
---

1. Start at `specs/README.md`, then read only the relevant feature and architecture spec.
2. Bound the request to the smallest acceptance-criteria slice; do not copy spec text into prompts or duplicate requirements.
3. Implement the slice, validate proportionally, and report what was run versus not verified.
4. Update the owning spec's acceptance/validation and `specs/status.md` only when behavior or completion changed; put concise tradeoffs in `specs/decisions.md`.

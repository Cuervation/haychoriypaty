---
name: token-optimization
description: Use for choosing context, model effort, delegation, or validation scope on a project task where extra work could outweigh its value.
---

- Search for relevant paths and use `specs/README.md`; do not read the whole repository by default.
- Solve simple edits directly. Delegate only independent work whose context isolation or parallelism clearly saves effort; one agent owns each writable file set.
- Project agents use only the Luna/Sol roles in `.codex/agents/`. Pick the smallest supported effort; the parent must not claim it changed its own model automatically.
- Avoid duplicate scans, repeated full-doc output, broad test runs after small changes, and long handoffs. Prefer concise changed-file and validation summaries.

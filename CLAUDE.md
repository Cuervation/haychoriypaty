@AGENTS.md

<!--
Claude Code entry point. AGENTS.md (imported above) is the single source of project rules for
Codex and Claude; do not add rules here. Shared architecture: .agents/README.md.
-->

## Claude Code mapping (no rules here; only where each shared piece lives)
- Roles: `.claude/agents/{architecture,implementation,validation}.md` are thin adapters; each reads its instructions from `.codex/agents/<role>.toml`.
- Skills: `.claude/skills` is a symlink to `.agents/skills` (same files Codex uses).
- Concurrency: at most two subagents at a time plus the main session, as in `.codex/config.toml`.
- Unity MCP: project `.mcp.json` (local, untracked) points to the already-running Unity MCP server; do not start another.

---
name: unity-2d
description: Use when creating or modifying Unity 2D scenes, scripts, prefabs, sprites, animation, or project settings in this repository.
---

- Check `ProjectSettings/ProjectVersion.txt` and the relevant feature spec first.
- Keep scenes, serialized references, and `.meta` files consistent. Prefer simple built-in 2D components; avoid installing packages unless the spec requires them.
- Keep simulation data separate from replaceable art/UI. Placeholder graphics must remain clearly provisional.
- Use the editor or a focused Unity batch command only when no Unity editor is already open. If Unity cannot run, preserve files and mark editor/import/play validation unverified.

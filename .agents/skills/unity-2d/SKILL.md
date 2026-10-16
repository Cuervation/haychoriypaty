---
name: unity-2d
description: Modify Unity 2D scripts, scenes, prefabs, sprites, animation or settings in Hay Chori y Paty; not needed for documentation-only edits.
---

- Confirm the active checkout and `ProjectSettings/ProjectVersion.txt`; read only the affected feature contract. Preserve scenes, serialized references, GUIDs, original assets and every `.meta`.
- Current runtime: `StreetGame` lifecycle/save, plain-C# `StreetSimulation`/`StreetKitchenProduction` domain, `StreetView` IMGUI/input. `StreetKitchenRenderer`, worker projection/animation and SpriteRenderer prefabs provide live transparent renders; visuals must not create stock or revenue. Do not introduce parallel uGUI or a second simulation.
- `StreetSceneLayout` owns the root scene; `StreetKitchenLayout` owns modular station geometry/anchors/routes. Racing+ uses the active-cook/Mozo contract; levels 1–5 retain their mode. `StreetWorkstationLayout` is historical compatibility, not active route authority. Consult `specs/visual-reference.md` only for visual work.
- Prefer the already-open editor through Unity MCP. Start read-only, select the authoritative project if multiple instances exist, and check actual results/compilation. Never launch a second editor against its lock. Batch is a fallback only when the relevant editor is not open; unavailable checks remain unverified.
- Preserve actual saves during validation and restore any temporary QA state. Reuse current cameras, art, fonts and packages; no dependencies/services installed by this skill. Choose only affected checks with `targeted-testing`; build/install only when authorized.

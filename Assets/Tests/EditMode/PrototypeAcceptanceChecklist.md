# Prototype acceptance check

## Automated (executed through Unity MCP, 2026-10-03)
- FlorestaRoundTests: 13 EditMode cases passed (actual simulation step, costs/rates/stages, outcomes and lifecycle).
- FlorestaPointerTests: 2 mouse PlayMode + 1 primary-touch PlayMode case passed via queued InputSystem device events; no direct gameplay command invocation in pointer tests.
- Exact jobs/results in specs/status.md. No batch/second editor was launched.

## Visual/editor (reviewed)
1. Main opens and runs. Reference composition reviewed in actual 540×960 Game-view PNGs, with player reference 1080×1920.
2. Live supporter sprites align with actual order/patience indicators; 6-order default and up to12 tuning slots rendered.
3. Original vendor frames/movement, street iron parrilla/embers/chorizo, cooler, side condiments and bottom actions are separate from the empty Floresta background; provisional, not final art.
4. Real round reached12-sales victory; earned funds purchased staff2 and speed1 in another run.

## Remaining device/manual pass
5. Build/install on an Android phone; verify physical taps, aspect ratios/safe areas, frame pacing and readability.
6. Review all vendor transitions and source sprite edge padding at target phone size; polish animation/hand-off variants and sound.
7. Tune starting funds, arrival/cook/patience and target after actual phone play. Keep chori/Coca only.

Capture Game view via MCP screenshot with include_image=false, then inspect the saved PNG. Inline capture can omit IMGUI and pause the editor. Do not mark the remaining device pass complete without execution.

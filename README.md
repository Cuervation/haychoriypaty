# Hay Chori y Paty

Unity `6000.6.3f1`, 2D portrait Android-first street-stall game. Open this repository in the recorded Unity editor and run `Assets/Scenes/Main.unity`.

Start with the [SDD index](specs/README.md); use [current state](specs/current-state.md) for the compact checkpoint; use dated status only for scoped evidence. The active game has eleven levels, fixed per-product sale prices, unlimited affordable hiring, capped attempt-local speed, persistent Mercado/Heladera and Racing+ active cooks/Mozos. The earlier two-product prototype remains historical, disabled in Main.

No additional packages are needed beyond the checked-in manifest. Use the existing Unity MCP connection and focused tests; do not launch another editor. Android APKs and private video/test evidence belong in ignored `Builds/` and `Logs/`, not Git.

## Working safely and validating

Open the existing checkout in Unity 6000.6.3f1 and use the Test Runner for EditMode and PlayMode tests. Preserve player saves and existing local changes; do not clear progress to run checks. See [current state](specs/current-state.md) and the relevant feature contract for known acceptance gaps. A passing test or build is not a substitute for visual and device testing.

**Do not run `Hay Chori y Paty/Regenerate Main Prototype Scene` on the current game.** This historical editor utility creates the old prototype and overwrites `Assets/Scenes/Main.unity`, Build Settings and player settings. The current runtime uses the existing Main scene instead.

The development Android build menu `Hay Chori y Paty/Build Firebase Android Development` requires the Android target, a clean active scene and an idle editor. Its build launcher explicitly includes only `Assets/Scenes/Main.unity`; the general Build Settings currently also list `SampleScene`. The launcher writes a development APK under `Builds/Android/`; review signing and platform requirements before distribution. See [Android delivery](specs/features/android-prototype.md) for current build and acceptance details.

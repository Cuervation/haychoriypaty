# River Plate / Núñez — visual reference mapping (2026-10-07)

Initial scope: prepared visual identity for official level8 / internal index7, without activating progression during that art-only request. **Activation follow-up2026-10-07:** River is now integrated into the eleven-level playable catalog, with300seconds/goal146 and the seven existing products; see `specs/features/street-automation.md` for the authorized configuration. Existing All Boys StreetSceneLayout, counter/clipping/worker/station geometry remain unchanged. One neutral clear-day scenery theme. Clothing is drawn into complete bodies, never overlaid photos.

## Specific mural references

- A: user-supplied `codex-clipboard-4aef9214-9404-4045-bd47-0729ef58ca54.png`. Left wall panel preserves the silver Libertadores trophy surrounded by historical white/red-sash figures, raised arm, smiling coach, dark blue field and red goalkeeper below.
- B: user-supplied `codex-clipboard-afc7363c-c506-4ffc-ad2b-b06b1c3348c6.png`. Right wall panel preserves the dense cheering supporter composition, raised hands, shield-bearing white caps, red/white flags and the sky-blue/white jersey at its left. No photo watermarks/signatures, no pasted photography.
- Output: `Resources/street-mural-river-master-v1.png`; clean inked painted wall style matching current scenery. Source composition is reinterpreted, not claimed to be a pixel-exact or photorealistic reproduction. Full source is fitted uniformly under the HUD; artwork does not dictate gameplay geometry.

## Apparel provenance

Browsed [Kits de juego](https://www.tiendariver.com/kits-de-juego), [Entrenamiento / Buzos](https://www.tiendariver.com/entrenamiento/abrigos/buzos), [Indumentaria](https://www.tiendariver.com/indumentaria), [Remeras y chombas](https://www.tiendariver.com/indumentaria/hombre/remeras-y-chombas) and actual product photographs. Source photos cached locally in ignored `Logs/Acceptance/RiverIdentity-20261007/References/`; they are not runtime textures and are not committed.

Exactly12 outfits,24 paired cheer/walk full-body cells and24 matched angry/swing cells. Grid6columns×4rows; variants run top-to-bottom, three per row, with even/odd columns their pose pair. Runtime customerId modulo12 remains stable in front/walk/riot. Sponsor/logo microdetails are simplified for legibility; these are retail-derived cartoon reinterpretations, not claims of licensed exact garment replicas. Pants/shoes/character identities are style-compatible choices; cited SKU pertains to the upper garment.

| Variant (ID%12) | Row / columns (1-based) | Real source SKU/product | Recognizable interpretation |
|---|---|---|---|
| 0 | 1 / 1–2 | [JI7071 Titular 25/26](https://www.tiendariver.com/ji7071-camiseta-titular-river-plate-25-26/p) | Blanca, banda roja diagonal, manga corta y anillos rojos; short oscuro. |
| 1 | 1 / 3–4 | [JN0785 Titular manga larga 25/26](https://www.tiendariver.com/jn0785-camiseta-titular-river-plate-25-26-manga-larga/p) | Blanca, banda roja diagonal, manga larga/anillos rojos y jeans. |
| 2 | 1 / 5–6 | [KD1519 Alternativa 26/27](https://www.tiendariver.com/kd1519-camiseta-alternativa-river-plate-26-27/p) | Paneles verticales rojos/blancos con separadores negros, mangas rojas; short oscuro. |
| 3 | 2 / 1–2 | [KG9670 Camiseta EQT](https://www.tiendariver.com/camiseta-eqt-river-plate-kg9670/p) | Coral, bloques blancos/negros de hombro, insignia circular central; jeans. |
| 4 | 2 / 3–4 | [KG9672 Campera EQT](https://www.tiendariver.com/campera-deportiva-eqt-river-plate-kg9672/p) | Campera retro con bloques blancos/negros, mitad inferior roja y cierre; pantalón oscuro. |
| 5 | 2 / 5–6 | [KB8867 Buzo Tiro26 blanco](https://www.tiendariver.com/buzo-de-entrenamiento-tiro26-competition-river-plate-26-27-kb8867/p) | Entrenamiento blanco, hombros negros curvos y antebrazos rojos; pantalón oscuro. |
| 6 | 3 / 1–2 | [KB8868 Buzo Tiro26 rojo](https://www.tiendariver.com/buzo-de-entrenamiento-tiro26-competition-river-plate-26-27-kb8868/p) | Entrenamiento rojo, hombros negros curvos, antebrazos rojo oscuro; pantalón oscuro. |
| 7 | 3 / 3–4 | [KB8875 Buzo Tiro26 con capucha](https://www.tiendariver.com/buzo-de-entrenamiento-con-capucha-river-plate-26-27-kb8875/p) | Buzo rojo con capucha, hombros negros curvos, mangas largas y puños; pantalón oscuro. |
| 8 | 3 / 5–6 | [KB8850 Remera algodón roja](https://www.tiendariver.com/remera-de-algodon-river-plate-26-27-kb8850/p) | Remera roja, paneles negros curvos y mangas cortas; jeans. |
| 9 | 4 / 1–2 | [KB8856 Chomba Tiro26 blanca](https://www.tiendariver.com/chomba-tiro-26-competition-river-plate-26-27-kb8856/p) | Chomba blanca, cuello/bordes de manga rojos y hombros negros curvos; pantalón oscuro. |
| 10 | 4 / 3–4 | [KB8857 Chomba Tiro26 roja](https://www.tiendariver.com/chomba-tiro-26-competition-river-plate-26-27-kb8857/p) | Chomba roja, hombros negros curvos, cuello y mangas contrastantes; jeans. |
| 11 | 4 / 5–6 | [JZ9296 Remera Seasonal Graphic](https://www.tiendariver.com/jz9296-remera-seasonal-graphic-river-plate/p) | Remera blanca, CARP negro sobre rectángulo diagonal rojo y escudo inferior; jeans. |

## Assets / implementation

- `Resources/street-river-fans-paired-v1.png`: complete bodies with12 cheer/right-walk pose pairs.
- `Resources/street-river-riot-fans-v1.png`: same12 people/outfits with existing angry/swing logic.
- `StreetView` reuses the established6×4 pose helpers and flip/queue/cheer/riot logic; a row adapter makes the documented top-origin provenance map exact.
- `ClubVisualTheme.ForLevel(7)` resolves River. Prepared-art indices shifted only where required to preserve official Boca8/Camioneros9/Redondos10 slots; live levels0–4 are unchanged.
- The initial art-only check used an editor-only nonserialized visual override. Activation removes that override: rendering now reads the actual simulation level; the subsequent playable-level review is recorded in `specs/status.md`.
- Built-in imagegen used for all three new PNGs; no external/paid API, asset overlays or edits to other club art. Prompts summarized in GenerationPrompts.md.

## Initial art-only validation

Unity6000.6.3f1:8/8 focused River/palette EditMode cases passed (`99d8d8e4fe7b45eb98859269beaf12ef`);2/2 atlas cases passed again after the final transparent-background cleanup (`5cc1709e85614504b4dcabe507b646e9`). Pixel checks require >40% genuinely clear atlas pixels, not merely an alpha channel.

Reviewed River-only1080×1920 Game-view captures of normal orders, all12front outfits, left/right walks and matching riot poses in ignored `Logs/Acceptance/RiverIdentity-20261007/river-playing.png`, `river-front.png`, `river-walking.png`, `river-riot.png`. The two-panel wall fills the viewport width with uniform proportions and stays above the unchanged master counter; no squeezed portraits or blank side panels. Existing queue/worker/station coordinates remain untouched.

This is an editor-only visual preview using the unchanged level5 simulation, **not a playable level8 or native Android validation**. Catalog/progression remain five entries; no APK or Git push requested.

Final focused console query:0 errors /0 warnings. Play stopped; original PlayerPrefs JSON, time scale, run-in-background flag and Game-view selection/maximization restored. Main scene remains clean.

## Activation validation follow-up

Actual River level8/index7 reviewed in Unity at1080×1920 (`Logs/Acceptance/Levels6-11-20261007/level8.png`) with its own146-unit goal/300-second configuration, real orders and both specialty workers. All six new actual-goal simulations finish using earned-income upgrades; focused catalog/progression/UI checks33/33 EditMode +5/5 PlayMode passed, final selector/theme recheck6/6 passed. No editor-only visual override remains; no native-device check/APK/push.

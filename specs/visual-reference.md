# Visual reference and art direction

## Source of truth

The supplied 30-second Food Fever video is the primary audiovisual reference for this milestone, with the user's detailed prompt specifying dynamics not visible in sampled frames. The user's attached gameplay screenshot, stored at [../Assets/Art/Reference/gameplay-layout-reference.png](../Assets/Art/Reference/gameplay-layout-reference.png), is the retained scene composition/style reference. Consult it before designing or changing any screen, character, object, sprite, or animation. Use it for its casual 2D cartoon language and readable vertical layout only; never reproduce its exact characters, illustrations, interface artwork, or assets.

![Primary gameplay style and layout reference](../Assets/Art/Reference/gameplay-layout-reference.png)

## Audiovisual evidence and composition

The supplied MP4 is VP9, 1080×1920, 60 fps and 29.9667 seconds. The parent decoded all 1,798 samples: unique frame indices 0–1797 with none missing and 1,785 distinct frame hashes. The first browser seek/canvas capture incorrectly repeated the initial frame; it was a capture defect, not a static video. Visual review used a contact sheet at one-second intervals 0–29 plus full frames at 8 and 20 seconds; decoding every frame is not a claim that each was visually inspected separately.

| Time | Verified visual observation |
|---|---|
| 0–3.5 s | White price panel, blue header, Burger Price slider and green Start. Displayed prices include $0 initially, $52 around 1 s, $35 around 2 s and $5 around 3 s; these do not establish the slider's maximum. |
| ~3.60–3.817 s | Price panel closes with an animated transition. |
| 4–7 s | Customers enter and group: roughly twelve moving customers at 5 s, seven front-row 999 orders at 6 s, then 21 visible orders in seven columns/three rows at 7 s. |
| ~4–8 s | One chef starts on the right, walks down-left toward the product station (~6.6 s), then toward the left counter (~7.5 s). At 8 s the first order reads 998 with a $5 coin indicator; Speed is available, Cook still unavailable. |
| 10–29 s | First counter reads 996 at 10 s, 987 at 20 s and 978 at 29 s. Both upgrade buttons are available by 10 s; prices remain $5/$15. Chef walking/carrying is visible, including burger carrying during later frames. |

Only one chef appears; no purchase, finished customer departure, mixed-product order or level transition was demonstrated. Do not infer a complete physical round trip for every decrement from the roughly once-per-second counter changes: the user explicitly requests handoff-gated decrements/revenue, coordinated extra workers, the seven products and progression as extensions. Prices/demand correlation is requested, not experimentally established by this clip.

Normalize the observed 1080×1920 composition to a 540×960 logical safe-area canvas: signs occupy left/right top bands; customer feet approximately y310/240/170 in three rows; horizontal counter top y314/bottom y384; handoff worker feet around y400; worker floor y400–585; station pickup y578 with worker feet capped near y585; large management controls y675–855. The video shows signs at normalized y.01–.14, bubbles/crowd y.066–.325, counter y.325–.40, worker floor y.40–.60, stations y.59–.65 and stage foot near y.715. Speed spans x.16–.47 and Cook x.53–.84, y.695–.89. Preserve this functional hierarchy and scale without copying illustrations. Camera is fixed, frontal and slightly elevated, showing ground/object tops: not pure top-down, isometric or 3D. Fit other aspect ratios to safe area using the same draw/input transform.

## Required direction

- 100% 2D mobile cartoon: small expressive supporters with oversized heads, rounded shapes, vivid colors, clean dark contours, consistent proportions and fluid simple animation.
- Fixed portrait camera. Keep signs at top, dense standing supporters with product/quantity/patience indicators above a horizontal separating counter, moving workers in the open lower playfield, product stations below and large hire/upgrade controls at bottom; retain automatic side condiments.
- Floresta public sidewalk, in front of All Boys: neighborhood facades and a generic stadium silhouette. Do not use club logos or imply indoor stadium service.
- Grill must read as an Argentine iron street parrilla with embers, chorizo and bread, not an industrial kitchen. Condiments are an automatic stop; there is no seating.

## Do not

Use photorealism, 3D characters, generic indoor-restaurant styling, seated diners, unapproved menu items, or literal copies of the reference's protected visual assets.

## Retained legacy assets and new milestone

Historical prototype assets: `Assets/Art/Placeholder/Resources/floresta-background-v2.png` is an empty original cartoon Floresta sidewalk/stadium-perimeter backdrop; `floresta-atlas-v2.png` supplies distinct black-and-white supporters, Argentine iron parrilla/embers/chorizos, cooler, condiment table, chori/Coca icons and a red-apron vendor. The atlas has seven vendor poses: idle, two walk, two cook and two serve frames. Simple 7 fps frame animation and view-only movement respond to actual round work/deliveries; hired sellers have separate working positions. Live customers, order icons and patience bars replace the old baked crowd, with up to twelve visible orders in two rows.

The atlas generator did not produce an exact geometric grid: PrototypeView uses reviewed individual pixel bounds at authored 1402×1122, not equal cells or imported texture dimensions. Import with NPOT resize/mipmaps off, alpha transparency and uncompressed max2048. Keep all art **provisional and swappable**, not a final-art or device-performance claim. Earlier plate/seller/atlas files and their metas are preserved but no longer consumed by the active view.

Editor screenshots reviewed at 540×960 (9:16), with player reference settings 1080×1920. For the historical0.1.0 prototype, Android safe-area fitting and native touch buttons were reviewed on the API36 emulator at1080×2400 (see features/android-prototype.md); physical hardware/other cutouts, sound, animation polish and final balancing remain later checks. Use async MCP screenshots (`include_image=false`) and inspect the resulting file: the v10 inline capture path can fall back to camera-only imagery (omitting IMGUI) and leave Play paused.

The new milestone requires original commercial-style backdrop, characters, all seven catalog products/stations, order/UI/coin effects and directional movement/carry/pickup/handoff states. Generated files and cropped bounds must be documented only after they actually exist and are reviewed/imported. No final-art, animation or performance completion claim follows from a generation request. Keep art swappable and preserve existing files/metas. See [street automation acceptance](features/street-automation.md).

Historical generation prompts/method: see `Assets/Art/Placeholder/GenerationPrompts.md`.

### Full-sequence count measurement

All 1798 frames were decoded without missing indices; source is 60 fps / 29.9667s (not exactly 1800 encoded frames). After crowd settlement, a binary digit-template comparison checked every frame in the first customer counter, using visually verified templates and five stable frames per change. The displayed order decreases from999 to977 by the last frame:22 unit decrements, approximately1.02s between changes. Approximate animation-onset events:7.95s→998,8.95→997,9.983→996,11.017→995,20.217→986,28.383→978,29.383→977. These are image measurements, not access to Food Fever internal logic. The worker visibly carries a burger away from the counter later while the count keeps changing; do not invent a new full station roundtrip for each observed decrement. The requested game must nevertheless gate each unit/revenue by its real handoff.

Private analysis evidence (ignored, not gameplay art or GitHub content): `Logs/ReferenceAnalysis/{status.json,metrics.jsonl,index.json,quantity-analysis.json,contact-00.png,contact-10.png,contact-20.png,transitions.png}`. Decoder used installed browser WebCodecs VP9 and a temporary loopback server; no packages installed or video sent externally.

## Current Street art — imported and reviewed

Active `Assets/Art/Street/Resources/street-background-v2.png` (940×1672) places the All Boys perimeter, original stall signage, counter and Argentine sidewalk in the measured functional bands. `street-characters.png` (1254×1254 RGBA) contains16 original worker/fan poses; `street-items.png` (1402×1122 RGBA) contains the seven products, parrilla/cooler/condiments and management/coin/order sprites. StreetView uses individually reviewed16/20 pixel rectangles, not assumed grid cells. NPOT resizing/mipmaps off; uncompressed max2048 and Android RGBA32 overrides.

Actual reviewed Game-view evidence: ignored `Logs/Acceptance/street-layout-final.png`. Generated method/prompts: `Assets/Art/Street/GenerationPrompts.md`. Directional frame walking and state-driven pickup/carry/handoff, moving/receiving fans and sale effects are active, but additional frame polish/audio and distinct four later-club illustrations remain pending. Original background v1 and all existing metas are retained.

## Parrillero appearance — 2026-10-03

User photo is appearance reference, not graphic style: very overweight adult, dark wavy hair/light stubble, fuller cheeks and double chin, matched face/body skin hue, bare shoulders/arms/back (no shirt), dirty white bib apron with grease/charcoal stains. Visible role is **Parrillero**, not Cocinero. Original cartoon likeness, no copied photo/logo/tattoo. Preserve existing fans, 2D composition and seven products.

New transparent16-pose atlas and hire portrait: Assets/Art/Street/Resources/street-parrillero{,-icon}.png. Reviewed independent bounds in Assets/Art/Street/parrillero-poses.json; exact built-in prompts in ParrilleroGenerationPrompts.md. Real MCP import16namedSprite, original dimensions/alpha/AndroidRGBA32 and foot pivots verified;3/3art and1/1affected pointer checks passed. Actual editor Game-view capture reviewed in Logs/Acceptance/Parrillero/parrillero-final.png: profile walk and matching portrait/Parrillero label. Native visual check pending; no device-performance claim.

Face references: user supplied four photos and explicitly selected the drawn adult-face variant; retain its eyes/nose/toothy smile while making cheeks/jaw fuller and matching face/neck skin palette to body. Do not revert to giant-eyed alternate or photographic head. Selected sheet1315x1197 and portrait1315x1196; previous character files and GUIDs retained.

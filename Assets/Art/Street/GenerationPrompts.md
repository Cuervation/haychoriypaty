# Original street-service art

Generated with the built-in image_gen tool (no external API/packages), 2026-10-03. Original cartoon art, not copied from Food Fever. All sprites are alpha-backed and render independently; text is live UI, not baked labels. Existing art/metas are preserved.

- `Resources/street-background.png`: portrait frontal/slightly elevated Floresta sidewalk outside All Boys; black-white pennants/canopy, empty product signs, horizontal wood/iron counter, open paved worker zone, condiment mesita, blank lower management area. No baked customers/equipment/UI; no industrial kitchen/3D/photorealism.
- `Resources/street-characters.png`: 4×4 sheet; red-apron bandana parrillero front idle/two walks/handoff, rear idle/two walks/profile pickup; four black-white fans idle/waving plus respective side walking poses. Big heads, small bodies, crisp dark outlines, rounded cel-shaded shapes; no club logos. Runtime directional selection, flip, bob and pickup/handoff pulses use actual simulation states.
- `Resources/street-items.png`: 20 individually bounded sprites: seven definitive products (four sandwiches on bread, small cola cup, large fernet cup, beer can), Argentine iron parrilla/embers, cooler, mesita, coin, bubble, upgrade card, Start button, modal panel, speed shoe, bandana worker portrait, lock, star, smoke. Same vivid rounded cartoon language; no extra products.

Sheets are not exact geometric grids. StreetView has reviewed source bounds, preserving alpha and aspect; importer disables NPOT resize/mips/compression. UI cards deliberately stretch their empty backgrounds, icons preserve aspect. Import and runtime checks are recorded in specs/status.md, not inferred from image generation. Source video frames are analysis-only under ignored Logs/ReferenceAnalysis, never gameplay sprites.

- `street-background-v2.png`: targeted built-in imagegen edit moved/resized counter to32.5–40% image height; preserved style, neighborhood, blank worker/UI areas. It is the active backdrop. Tight alpha-derived character rects prevent equal-grid padding from shrinking characters.

## Large street grill — 2026-10-04

Built-in image_gen, transparent background; user street-grill photos supplied only as ambience/scale reference. Original2172x724RGBA single sprite saved as Resources/street-parrilla-large.png, source generated_images/exec-d67ba197-b658-4350-87ac-bb8d1a6d5886.png. No copied photographs/people/logos.

```text
Use case: stylized-concept. Asset type: original transparent 2D sprite for the Argentine mobile game Hay Chori y Paty. Primary request: ONE LARGE street parrilla, much wider than a tiny barbecue, like a busy sidewalk stand outside an Argentine football stadium. Style: polished casual cartoon matching colorful food sprites, warm saturated orange browns, thick clean dark contours, simplified highlights, rounded readable shapes, NOT photorealistic, NOT 3D. Fixed frontal slightly elevated view of the grill top, NOT isometric. Subject: a long black iron rectangular grill with visible straight iron bars, orange glowing charcoal underneath and a sturdy low black iron frame with very short legs. The wide cooking surface is densely covered with 36 plump Argentine chorizos arranged in three neat long rows, visibly individual browned sausages with light grill marks and a few lighter ones. Small cluster of golden bread rolls on the right edge. A subtle curl of translucent smoke, not a giant cloud. Grill itself horizontal, approximately FOUR times as wide as tall, minimal perspective depth and low squat silhouette. Entire isolated object fully visible and fills width, no clipping. Transparent background with real alpha; no floor, no cast shadow outside object, no people, no lettering, logos or watermark, no stainless steel industrial appliance, no kitchen, no UI, no tiled sprite sheet. Mobile readability is essential. Draw exactly one original large grill, not copies of the photographs.
```

## Floresta opposite-wall mural — 2026-10-04

User-supplied mural photo was used only as a location/content reference. Built-in image_gen edited the existing composed background non-destructively to a new versioned asset. The wall art is original, avoids copied portrait arrangement, text and marks, and preserves the stall/road/sidewalk/open playfield/UI-safe area.

```text
Use case: precise-object-edit. Asset type: portrait 2D mobile-game level background for Hay Chori y Paty, Floresta/All Boys level. Image 1 is the exact edit target. Image 2 is reference for neighborhood mural subject/composition only, not for copying. Change ONLY the blank far-background stadium-perimeter facade: paint a long monochrome football-supporter mural on the wall across the street, visible over/around the stall. Original art: expressive simplified supporter portraits, a small group celebrating, player kicking a ball, flags/scarves; hand-drawn black ink/charcoal linework on pale plaster, with bold readable silhouettes. Evoke All Boys / Floresta football supporter culture without copying reference photo's arrangement, exact figures/faces, wording, logos, crests, signatures, watermark, or sponsor marks. No mural text. Preserve everything else in Image 1 exactly: portrait composition, sky, trees, stadium silhouette, pennants, stall canopy, blank hanging signs, “HAY CHORI Y PATY” stall sign, wood/iron counter, sidewalk, open playfield, condiment table, empty lower UI-safe area; do not move, resize, redraw, remove, cover, or add foreground elements. Match the existing polished original 2D cartoon illustration, clean dark contours, rounded readable shapes, warm daylight palette, lightly painted surfaces. Mural is a stylized hand-painted cartoon mural, not realistic people. Perspective-calibrated flush to the far wall and secondary to gameplay. No new people outside mural, no added food/menu objects, no photorealism, no 3D, no UI, no foreground character, no other text, no club trademark/logo, no watermark.
```

Output: `Resources/street-background-mural-v3.png` (940×1673 RGB). It remains a separate file so the previous backdrop and GUID remain available.


## Open street / counter-only stall — 2026-10-04

Mode: built-in ImageGen `imagegen`, precise-object-edit, opaque background. Exact edit target: `Resources/street-background-mural-v3.png` (existing original project art). Generated output is copied unchanged to `Resources/street-background-open-street-v4.png` (940×1673); old v3/GUID retained. No CLI/API fallback, resampling or programmatic image editing.

```text
Use case: precise-object-edit.
Asset type: opaque portrait 2D mobile-game background for Hay Chori y Paty, Floresta street level.
Input image: the referenced file is the exact existing project background to EDIT, not merely a style reference.

Primary request: remove the entire upper stadium/sky portion, remove the stall roof/awning, remove the "HAY CHORI Y PATY" wooden header, remove both hanging black chalkboards and all tall roof-support posts and their feet. The stall must be ONLY the low wood-and-black-iron counter. Enlarge the open STREET BEHIND the counter where the queued customers will stand. The top edge of the image must end at the mural wall itself: NO scenery, sky, stadium facade or floodlights above that mural.

Composition and exact game-layout constraints:
- Keep the FULL portrait canvas at the input aspect ratio, approximately 940 x 1673 (9:16). Do NOT solve this by cropping or rescaling the entire image.
- Preserve the existing counter in its SAME horizontal band and scale: wooden countertop begins approximately y548 (32.7% of image height); black iron front ends approximately y655 (39.2%). Keep the counter spanning the width. Do not move it up or down.
- Recompose ONLY the former upper stadium/canopy/signage area above this fixed counter. Relocate the existing ORIGINAL black-and-white supporter mural wall to the TOP EDGE, occupying roughly the first 12% of image height (y0 to around y200). At the image's upper edge there is mural/plaster, not sky or architecture above it. Keep recognizable original painted supporters/flags/football mural subjects and the same cartoon art language; do not invent lettering, crests or logos.
- Immediately below the mural wall, a subtle far curb and a broad EMPTY gray-warm street extend all the way to the back of the counter. The expanded customer STREET occupies roughly y200 through y548 (12% to 32.7% of the canvas). This band must be open, unobstructed across its entire width and large enough for three rows of standing customers. It is street roadway/paving, not another counter or a restaurant. No live people: customers will be separate game sprites.
- Preserve EVERYTHING from the counter downward in the same positions, proportions, palette and appearance: long counter, existing right-side condiment workbench, open warm sidewalk tiles for workers, cream blank lower HUD area, small bottom black-and-white pennants. Exception: remove the old tall canopy supports/pole bases wherever they extend into this area, neatly inpainting their tiny footprints. Do not change the lower floor, do not enlarge the foreground worker sidewalk instead of the CUSTOMER STREET, do not add objects.
- No awnings, roofs, overhead bunting, title signs, hanging boards, floating icons, stall branding, stadium above the wall, extra structures, seating, people outside the painted mural, food, UI, text, watermark or new logos. Keep only the counter as the stall structure.

Style invariants: match the exact existing polished original casual 2D cartoon, clean dark contours, warm daylight, lightly painted material textures, fixed frontal slightly elevated portrait camera. Not photorealistic, not 3D, not isometric. Preserve existing art; do not redesign the full scene.
Output: exactly one finished opaque edited portrait background, without framing or an explanatory diagram.

```

Static output review: no upper stadium/sky, canopy/header/chalkboards or supports; original mural at top; wide empty road behind unchanged counter band; existing foreground/HUD bands retained. No live crowd/worker/food sprites baked in. Source selection and sign-overlay removal integrated, but no Unity/Game-view/device validation or APK this turn.

SHA256: `4d468772fc8524a331f7d8ac7e1bfcf2f32ad44bad99f51302c36e3b6e9bd8dd`. New TextureImporter meta copies prior NPOT/mip/Android settings with new GUIDs and full image bounds; import not verified.


## Game currency: gold-only choripán/paty coin — 2026-10-04

- Method: built-in ImageGen generate, then scoped color/material edit after user requested100%gold. No API/CLI fallback or Python image edit.
- Draft: `/Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/exec-0e71e8c8-a587-4e09-9b3f-94fd841b1dbb.png` (colored food/Argentina accents, not integrated).
- Selected edit source/output: `/Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/exec-fbcef7af-73af-42b8-928d-6e7266f490ed.png`.
- Project asset: `Resources/street-coin-gold-v2.png` —1254×1254RGBA, original output bytes/alpha preserved, gold-only relief, statically reviewed; import/Game/device review deferred.
- SHA256: `2b65f5d3ee2e637c7fa898a57be8ff172b45bca76a2c07c4faa0222c5831b4a4`. Previous sprites/metas retained; new meta GUID and full bounds, mipmaps/NPOT off, uncompressed AndroidRGBA32.

### Initial generation prompt

```text
Use case: stylized-concept.
Asset type: single original transparent game currency icon for the upper-left HUD of Hay Chori y Paty, an Argentine street-grill 2D mobile cartoon game.
Primary request: one beautiful chunky GOLD COIN with clear original embossed symbols referencing BOTH choripán and paty: a grilled chorizo in a split bread roll and a small beef paty sandwich, arranged together as one compact central food emblem. The choripán must be unmistakable (sausage sticking out of bread, a few simple grill marks), the paty recognizable as a burger bun with beef patty. No other foods.
Style/medium: polished 100% 2D cartoon sprite, rounded cel-shaded shapes, thick clean dark-brown contours, crisp readable silhouette, highlights and raised embossed relief drawn in simple flat cartoon tones; NOT photoreal metal, NOT 3D render. Warm yellow/gold rim with a subtle cream/red center food detail, tiny pale sky-blue/white enamel accent reminiscent of Argentina and the existing title logo palette. Large simple forms, readable at 40 pixels.
Composition/framing: frontal nearly perfectly round coin, very slight illustrated rim thickness, centered on a square canvas, isolated, occupying about 90% of canvas with generous transparent padding. Entire outline visible, no cropped edges. One coin only, no pile, no mockup, no UI panel.
Scene/backdrop: genuinely transparent alpha outside the coin; no opaque background, checkerboard, floor, or cast shadow outside its silhouette.
Text: none. No letters, numbers, dollar signs, watermark, logo copy, club crests or external brand marks.
Output: transparent square PNG asset. This is new original in-game currency artwork, not the app icon or title logo.
```

### Final gold-only edit prompt

```text
Use case: precise-object-edit.
Asset type: final transparent in-game currency coin sprite for Hay Chori y Paty.
Primary request: EDIT THE ATTACHED COIN ONLY TO MAKE IT 100% GOLD, with absolutely no colored enamel or colored food. Preserve the same coin outline, circular framing, rim thickness, placement and exact central choripán and paty/burger food shapes, but turn every detail into embossed relief made from the SAME GOLD METAL as the coin.
Change only material/color treatment. All sausage, bread, grill marks, burger, tomato, patty, garnish and both ribbons must be monochrome gold relief. No red, green, blue, white, silver, brown food, black ink or multicolored accents anywhere on the coin. Use only gold-family tones: warm gold, pale yellow-gold highlights and deeper gold/ochre relief shadows, enough contrast to read the choripán and paty at small HUD size. Make upper/lower ribbons plain gold embossed arcs without a flag. Not painted food, no colored center.
Preserve polished casual 2D cartoon icon style, not photorealistic and not a 3D-render photograph. Entire coin visible with transparent padding, centered square format.
Keep true transparent alpha everywhere outside the coin; no opaque background, checkerboard, floor, external cast shadow, UI layout, letters/numbers/dollar signs, watermark, new objects or extra coins. Do not alter or replace the food-emblem design or introduce the app title logo.
```

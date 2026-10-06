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

## Grill bread correction — 2026-10-04

Built-in image_gen precise edit of the original grill; `Resources/street-parrilla-large-v2.png` is the active version (2172×724 RGBA). Original art and `.meta` are retained; new version has its own importer GUID and uses the copied no-mipmap, uncompressed Android RGBA32 settings. The redundant first-product badge is suppressed in `StreetView.DrawStation` rather than baked into art.


## Parrilla Criolla upgrade cards — 2026-10-04

User selected the second (wood-and-iron **Parrilla Criolla**) treatment from the five-pair upgrade-button concept sheet, and asked to replace the speed shoe with forward chevrons. Both are original transparent assets generated with built-in ImageGen using that supplied board as a style reference; live text, price and coins are rendered by Unity rather than baked into either image. Existing Parrillero portrait is reused.

- `Resources/street-upgrade-wood-v1.png`:1510×1041 RGBA,2039874bytes,SHA256`41c4eb90653ba87c72fe4b4f8bea3485a1d804de4e4a2719f7bc98b503f102b7`. Blank reusable riveted iron/wood frame with a dark-gold circular medallion and wide brass price plaque. Fresh GUID; preserved alpha, no mipmaps, full source resolution, Android RGBA32. Unity draws its alpha-trimmed pixel bound `(13,148,1484,715)` across each card.
- `Resources/street-speed-arrows-v1.png`:1402×1122 RGBA,805780bytes,SHA256`09ec1a9307665929d855947c0a3e0c524745f0eeb76507f7f333b5f3af375ac9`. Three forward/right-facing amber-gold chevrons with dark comic outline; no shoe/text/background. Fresh GUID; preserved alpha, no mipmaps, full source resolution, Android RGBA32. Alpha-trimmed draw bound `(114,224,1174,662)`.

### Image-generation prompts

**Wood card:** Create one clean transparent-background horizontal game UI upgrade card for the Argentine street-stall mobile game Hay Chori y Paty, using the supplied five-design board only as a visual style reference. Select its second “Parrilla Criolla” wood-and-forged-metal look. Make a broad compact rounded rectangular plaque with rich warm horizontal wood boards, dark charcoal riveted iron border/corners and warm gold highlights. Put an empty dark charcoal circular inset outlined in gold on the left for an icon portrait. Reserve a clear horizontal wood title zone across the upper right and a large empty brass/gold rectangular nameplate across the lower right for dynamically rendered coin and variable price. Keep the areas unobstructed and spacious enough for UI text, and the brass plaque large enough to display four digits. Crisp friendly comic mobile-game illustration, strong readable outlines and polished dimensional shading. Output only the standalone plaque with truly transparent alpha around it; no lettering, numbers, currency, coin, person, sneaker, arrow, logo, watermark, shadows outside the silhouette, or background.

**Speed icon:** Create a standalone transparent-background icon for a speed upgrade in the same Argentine Parrilla Criolla comic mobile game. Replace a sneaker with exactly three bold forward/right-pointing speed chevrons in warm golden yellow and amber, with cream highlights, dark brown outline and polished dimensional shading. Keep the icon horizontal, compact and clearly readable at small mobile-button size. No shoe, person, text, numbers, coin, logo, watermark, backdrop, tile, or cast shadow outside the icon; transparent alpha.

```text
Precise edit of the exact referenced in-game asset. Keep the same full canvas, dimensions, transparent background, composition, lighting, style and grill exactly. Edit ONLY the cluster of four short round hamburger buns on the far right of the grate: replace them with four clearly elongated narrow rustic Argentine pan francés rolls for choripán, golden crisp crust with subtle lengthwise scoring, same cluster/location/scale, resting on the grate. No sausage, no sandwich filling, no new food. Ensure bread silhouette is oblong French bread, not round burger buns. Remove any detached floating choripán or food icon from empty space above the grill; that area must remain transparent and empty. Preserve every existing chorizo, their exact three rows and count, grate, fire, metal frame, legs, handles and smoke without any other changes. No text, UI, watermark, people, floor, logos, or extra objects.
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


## Full-screen startup background without old hero — 2026-10-04

- Built-in ImageGen **edit**, not paid CLI/API routing. Image1 edit target: retained`Resources/street-cover.png`; Image2 style reference only: retained exact gameplay`Resources/street-parrillero-icon.png`. Output does not contain the hero; StreetView composites that exact original portrait PNG as a separate foreground layer, so face/identity are never regenerated. Logo/menu also remain separate unchanged layers.
- Final project asset:`Resources/street-cover-no-hero-v2.png`,1024×1536RGB/opaque,2503289bytes, SHA256`240a30b8902e78d5875b0fa0d96accc8f1c26fd14c25931e27255096118f1734`; original generated PNG copied byte-for-byte. New preserved GUID/meta inherits NPOT/mipmap off,max2048/uncompressed/AndroidRGBA32 with correct full1024×1536bounds. Previous cover/portrait/logo assets+metas untouched.
- Generation output:`/Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/exec-4a39f5b7-4487-462c-87af-07bdbc6dd5f2.png`. Reviewed native output and actual composed9:16/tall-phone Unity screenshots; no baked UI/logo or old foreground hero.

### Exact prompt

Use case: precise-object-edit / game-background. Image 1 is the edit target: the existing Hay Chori y Paty startup cover. Image 2 is a style reference only: the EXACT existing game Parrillero portrait which will be composited separately in game code; DO NOT draw this man or any new foreground character in the output. Produce one complete opaque portrait BACKGROUND plate, 1024x1536. Remove the entire large foreground man from Image 1 (head, torso, apron, arms/hands) and naturally reconstruct the vacated center as the continuation of the dim Argentine street-stall / football-stadium approach and distant black-and-white standing supporters. No replacement foreground man, no foreground face or hands, no ghost silhouette; leave the center-upper area compositionally simple behind the separate portrait that will be drawn later. Retain the warm stall canopy/hanging bulbs at upper left, original blue nighttime football stadium lights in distant upper background, modest distant black-white supporters on public street, left charcoal grill with chorizos, wooden counter with the original sandwiches and generic unlabeled cups/can along the lower foreground. Maintain their functional upper/background versus lower/counter bands, straight-on portrait framing, friendly Argentine street-grill atmosphere. Render the whole plate in cohesive clean outlined, warm rounded 2D casual mobile-game cartoon illustration/cel shading compatible with Image 2, not skin/food photographs or 3D. Entire frame filled with scenery edge-to-edge; no margins/bars/border or blank transparent regions. Keep safe central subject area; when side edges are cropped on a tall phone, the food/stall still reads. Never bake UI: no text, title, logo, lettering, buttons, labels or watermarks, no added products or indoor/seated dining. Image 2 is NOT an edit target: preserve that exact portrait file; only generate the empty-background plate.


## Real street mural / stadium-corner overlay — 2026-10-04

- Method: built-in ImageGen edit. First input v4 target; second real main mural photo; third stadium-corner photo. Final correction targets first output with v4 as geometry reference. User explicitly asks recognizable real mural motifs; no external paid CLI/API route.
- Final retained asset: Resources/street-mural-real-v5.png,940×1673RGB/opaque,2105716bytes, SHA256 1f349657ae3e3922ff766841104e487bfaa18c82dc14155739bd10d21cb0c24e. Copied byte-for-byte from /Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/exec-a01541c9-fde3-4870-82be-43b501ee42bc.png. Fresh asset/Sprite GUIDs; inherited NPOT/mips off/max2048/uncompressed/AndroidRGBA32/full bounds. Original v4 unchanged.
- Static review: specific player rows/windows/buildings/justice portraits/inscription and corner oval/shield/shoe memorial; no watermarks, ad phone number or upper stadium/sky. Both full plates drifted counter/cream panel about80px: full new background is NOT consumed. StreetView draws only source(0,0,940,234) into logical(0,0,540,90) over old v4, keeping all lower pixels/anchors unchanged. Follow-up seam correction (2026-10-05): the current renderer uses source height280 and target height118 to include the full bottom edge and cover the old mural band. No Photoshop/Python raster edits/resampling. Unity import/Game-view unverified.
- Draft retained at /Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/exec-dde5bfdd-8763-45c5-bc18-727ddae23473.png; final output path above.

### Initial exact prompt

Use case: precise-object-edit.
Asset type: opaque portrait 2D mobile-game background for Hay Chori y Paty.
Input images: Image1 is the EDIT TARGET, the current complete game background. Image2 is the main real All Boys street mural reference. Image3 is the real stadium corner mural reference. Images2/3 are appearance/composition references, never pasted photographs.
Primary request: Repaint ONLY the upper distant mural wall in image1 so players recognize the ACTUAL street mural in image2, rendered as clear original hand-drawn cartoon game art. The present generic cheering fans and waving flags are NOT sufficiently similar. The result should unmistakably use the specific main mural arrangement, not another invented fan scene.
Preservation: Keep image1's complete portrait canvas and perspective, ideally its native940x1673 dimensions. Keep the curb at approximately y160–180, the large EMPTY crowd street from y180–548, wooden counter and dark front at y548–655, tiled worker foreground to y1155, right condiment bench, plants at counter edges, cream blank bottom management area and lower black/white bunting EXACTLY in their existing positions and proportions. Do not enlarge the wall down into the customer street. No gameplay sprites, supporters in street, grill, HUD, prices, buttons or floating items baked into backdrop.
Upper mural details: Recreate the composition of image2 within the upper wall band, adapting to the game's wide shallow band. Ivory/white painted brick wall; strong horizontal black stripes across it; dark barred horizontal windows in the upper stripe; several rows of individually illustrated black-and-white footballers and historic neighborhood people, NOT generic waving fans; a recognizable group of standing uniformed football players on the left; white/black outlines of Floresta neighborhood buildings along lower middle; three bust portraits and a dark figure with outstretched arms on right half; substantial lettering "FLORESTA POR JUSTICIA" on lower main panel; small score "0-3". Preserve the visual relationships/painted-wall look from photo, with simplified readable cartoon faces and dark outlines rather than photorealism. Main mural must occupy majority of the upper width.
Corner imagery: A modest angled wall return at far left, inspired by image3, with prominent blue/white oval intertwined AB monogram filled with a tiny green pitch at its foot, plus the simple black-outlined shield "C. A. ALL BOYS". Integrate a small black/white sneaker memorial motif and words "SIEMPRE RESISTIR" in a modest far-right wall panel from image3 if space permits. Those corner motifs are secondary to main mural; don't displace its football team rows, buildings and justice portraits. User explicitly requests recognizable real-world murals in this cartoon game.
Remove the decorative front fence/white posts and large tree canopies that currently cover much of image1's mural, ONLY within upper wall band, to make the actual painted composition visible. Small peripheral greenery may frame outer edges but do not obscure the mural. The bottom curb stays aligned.
Style: Match existing background's friendly outlined clean 2D casual mobile-game illustration, warm ambient daytime, hand-painted subtle brick texture, consistent scene lighting. Main wall mostly black and ivory, only the corner oval brings localized sky-blue/green accents. No photo texture pasted into game.
Avoid: No upper stadium facade, upper construction bricks, windows above mural, sky, floodlights, overhead stall roof/canopy/signage, giant poles, dining tables, seated customers or new products. Do not copy any social-media watermark (CAALLBOYS.COM.AR, @handles, icons) or vertical CLUB ATLÉTICO photo watermark; no advertising banner, phone number, sponsor mark, stock watermark or commentary. No changes outside the upper wall band. No cropping or moving the original counter/street/UI bands. Produce the whole edited background, not a standalone mural or montage.

### Corrective exact prompt

Use case: precise-object-edit.
Image1 is EDIT TARGET: new cartoon real-All-Boys mural background. Image2 is geometry/layout reference: previous game background. Keep recognizable newly painted real mural motifs from image1, NOT the generic waving fans in image2.
CRITICAL FIX: Image1 shifted the curb, counter and cream bottom panel down about80pixels. Restore EXACT original gameplay geometry from image2. Both canvases940x1673. Preserve entire lower scenery of IMAGE2 unchanged, starting at its upper road curb y160–180 and continuing down through the whole rest of its canvas, including exact sidewalk cracks, wood counter, plants, condiment bench, tiled foreground and lower cream UI panel/bunting. The original counter top is y548, front ends y655; cream bottom panel starts y1156. These coordinates MUST match IMAGE2, not image1's countery630 and UIy1236. No global shift, reframe, zoom or expanded wall.
ONLY replace image2's old top mural with the NEW specific mural artwork from image1, compressed vertically to fit the original wall height: the far wall/mural occupies y0–155; curb y155–180; empty road y180–495; narrow tilesy495–548; countertop/fronty548–655; foregroundtilesy655–1156; creampanely1156down. Recompose/compress the new mural's characters/black horizontal bands/barred windows/team/building outlines/three portraits/"FLORESTA POR JUSTICIA" within y0–155, with its blue ovalAB and C.A.ALLBOYS shield on left return and sneaker memorial on right. Leave those motifs recognizably the same as image1, not generic cheering-fan replacement. It is a shallow distant mural wall; fitting the band is more important than tall wall proportions.
Top frame ends at mural paint, with absolutely NO sky visible, no construction/stadium above wall, no overhead roof/awning/signposts/fence. Small outer greenery may frame it but no sky gaps.
Maintain friendly outlined cartoon style consistent with both images; no photographs or watermarks, no gameplay characters, grill, buttons or HUD baked in. Complete opaque940x1673 portrait game background. Match image2 bottom scenery/layout; repaint ONLY top wall with image1's newly recognizable mural.


## Existing All Boys crest for level footer — 2026-10-04 (NOT generated)

User explicitly requested All Boys shield beside Floresta / All Boys. Source club page https://caallboys.com.ar/manual-de-marca-allboys/ links header PNG https://caallboys.com.ar/wp-content/uploads/2022/07/logo@2x.png. Retrieved original on2026-10-04, inspected black/white classic shield with diagonal C.A.ALL BOYS band. Resources/street-allboys-crest.png is an untouched byte-for-byte320×320RGBA PNG,72250bytes,SHA2569a8b8543b88401f6186722b3caa3d5aed25e12d8c8563ffdc94e33167e477f29. No ImageGen, raster editing/resampling or reconstructed logo. Fresh meta inherits existing alpha/NPOT/mips-off/uncompressed/AndroidRGBA32 settings, full320×320bounds. This is a third-party club mark, not original game artwork; source attribution is not a claim of a reuse license or club endorsement. Any public-release permissions review remains separate.

Only level0 footer displays it (Ready/Playing); later locations never inherit All Boys crest. Typography reuses already bundled Luckiest Guy font/license. Unity import/Game-view readability not yet verified.


## Used trestle choripán pickup table — 2026-10-04

Built-in ImageGen generated a new transparent 2D prop. Reference image 1 (the user's square photo) guided the wooden A-frame/caballetes construction and perspective only; reference image 2 (the existing `street-parrilla-large-v2.png`) guided only the game's cartoon linework, food lighting and rendering. The final asset is `Resources/street-serving-table-v1.png`,1774×887 RGBA, SHA256`a44bc943193943c549fe792d6dc90141508bdc940c893e0d0cd1c4807c9da4d1`. Original output remains at `/Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/exec-73273c50-6133-4fbc-8e20-5a94cb9ba7c0.png`. New Unity metadata uses fresh GUID`262c70b1a7b14d359409cefc28e1de95`, single full-canvas sprite, mipmaps off, alpha transparency enabled, original resolution and Android RGBA32. Import has not been verified in Unity.

### Exact prompt

Create one new standalone game sprite asset, use case: prop-sprite. Reference image 1 is ONLY for the recognizable construction and perspective of a simple wooden sawhorse/trestle work table: a long thick plank top supported by two separate A-frame caballetes. Reference image 2 is ONLY for the existing Hay Chori y Paty game's warm outlined 2D cartoon food/lighting/render style; do not copy the grill itself. Depict the table in a matching clean, bold-outlined, rich warm casual mobile-game illustration style, seen from a slightly elevated front three-quarter angle. The wood should look used and a little dirty: worn/scuffed plank edges, subtle grease marks and crumbs, not rotten or filthy. Fill nearly the whole tabletop with many ready-to-serve Argentine choripanes: long split pan-francés rolls, each visibly holding one grilled chorizo, arranged in two generous rows, browned bread and savory charred reddish sausage. These are assembled sandwiches on the table, not loose sausages, not hamburger buns, not raw meat. Keep table construction recognizable, visible paired trestle legs and cross-bracing. Transparent background with clean alpha around only the table and food, no cast shadow outside the silhouette. Deliver a single wide landscape object sprite, broad and low like a table, mobile-game readable at reduced size. No scene/background, floor, wall, grill, people, hands, plates, text, labels, logos, signs or watermark.


## Deadline riot and destroyed stall — 2026-10-04

Generated with built-in image_gen as a versioned full-screen defeat backdrop. Target/source: `Resources/street-background-open-street-v4.png`; style reference for supporters: `Resources/street-characters.png`. The scene keeps the Floresta mural/street setting while placing visibly enraged black-and-white supporters with raised wooden sticks before a shattered counter/side stand. Cartoon/non-graphic; no text/logo/UI. Used only on `RoundPhase.Lost` in active `StreetView` (Main); normal gameplay background and simulation are untouched.

- `Resources/street-riot-defeat-v1.png`: 940×1673 RGB, 2,853,862 bytes, SHA-256 `b50567cd5b9fa2b7b2f5ab10a61000676aa0badaa2270ef5131075af0d8eb3b0`; new `.meta` GUID `a7729078c99f4a0b899373de96c8c781`, based on existing backdrop importer (no mipmaps/NPOT resize; Android RGBA32).

### Exact image prompt

Use case: original production-quality 2D mobile game defeat-scene background, based on this game's actual Floresta street-stall environment.
Image 1 is the EDIT TARGET: preserve recognizable All Boys mural, Argentine neighborhood street, sidewalk/front-facing open stall geometry, counter, side condiment table, and warm palette/composition. It is the exact environment reference; no reinterpretation into an unrelated stadium.
Image 2 is style/wardrobe reference for the existing game's cheering football supporters. Make older supporter characters in black-and-white shirts using its crisp, polished, expressive, outlined 2D cartoon game-art vocabulary, NOT photorealism.
Create a dramatic yet family-friendly comedic defeat scene for when customers waited past the 180-second deadline. Put a tightly gathered crowd of several supporters prominently IN FRONT of the stall/counter, occupying the middle of frame. They are unmistakably furious and enajenados: deeply furrowed brows, narrowed angry eyes, red flushed cheeks, clenched teeth and wide shouting mouths; faces and expressions clearly visible rather than tiny distant silhouettes. They have wooden sticks clearly visible above/through the crowd, raised and waved angrily. It must instantly read as an enraged riot, not cheering or celebrating.
Show the food stand already visibly wrecked by the riot: serving counter face cracked/splintered, a broken plank hanging loose, one side support bent/fallen, utensils and sandwich pieces scattered, one small condiment stand tipped. The Parrilla remains recognizable nearby. Small cartoon dust/debris/spark flecks suggest chaotic motion, but no fire damage.
Maintain the actual compact street-stall game setting, mural/street placement, strong black-white supporters' clothing, bold clean outline/paint treatment and polished mobile game clarity. The crowd should dominate and communicate anger from a phone-sized screen.
Full-bleed opaque portrait illustration, same visual framing; extend art to every edge, no gray bars. Leave a small uncluttered margin at very top for a live HUD and a readable area along the lower edge for a replay button. NO words, lettering, logos, UI/panels, watermark, injury, blood, gore, guns or graphic violence. Keep the riot cartoon and non-graphic; sticks are stylized props. Do not make the people smile, cheer, wave flags, or raise their hands as celebration. One unified scene, not a collage.


## Live timeout anger atlas and broken environment — 2026-10-05

The user's follow-up clarified that timeout must be a real sequence, not the previous single crowd illustration. Built-in image_gen created a transparent four-identity/two-pose All Boys supporter atlas and an empty broken-stall background plate. The former keeps the four existing fan archetypes/outfit overlays and alternates raised-stick/swing poses; the latter preserves the damaged counter but has no static people or sticks, so StreetView draws the actual frozen queue in front.

- `Resources/street-riot-fans-v1.png`: 1774×887 RGBA, 1,737,181 bytes, SHA-256 `d20d569d09c9ee2323190e20ce3616750bb37ae0849ec748343ab1ed86a8ac2a`; new `.meta` GUID `d903eebb07c142a4b0aa0cacd395c834`, cloned from the existing fan-atlas importer (transparent, no mipmaps, Android RGBA32). Four columns map to the same brown-bob girl, capped boy, older mustached fan and curly-haired fan; raised-stick row covers y=0–493 and swinging row y=493–887.
- `Resources/street-riot-environment-v1.png`: 940×1673 RGB, 2,733,231 bytes, SHA-256 `24d54372d5122b3a705e67add2cff46e77e819cebaba1772a10faf3a932f173b`; new `.meta` GUID `c8045271401b46f4a2f47f69e6b73682`, cloned from the prior riot-background importer (no mipmaps/NPOT resize; Android RGBA32). It retains the smashed counter/debris with all crowd/stick figures removed.
- The original `street-riot-defeat-v1.png` remains intact for provenance/history and is no longer the active timeout plate.

### Exact angry-fan atlas prompt

Use case: illustration-story; transparent 2D Unity sprite atlas. Use the original `street-characters.png` fan atlas as the character/style reference and `street-riot-defeat-v1.png` only for anger/wood-stick mood. Generate exactly four columns and two rows: column identities are the brown-bob girl, round-faced boy in black/white cap, gray-haired mustached older man in black cap, and curly-haired young woman. The top row shows each shouting with furrowed brows, narrowed eyes, flushed cheeks, fist raised and wooden stick overhead. The second row repeats the same four identities/clothing/scale/footing with the stick swung down/out in a non-contact strike and clear motion marks. Keep the same bold outlined polished cartoon proportions, black/white All Boys clothing, transparent gutters and full bodies; no background, text, logo, watermark, injury, blood, flames or happy expressions.

### Exact broken-environment prompt

Use case: illustration-story; full-bleed portrait Unity background. Edit the current `street-riot-defeat-v1.png`: remove every person, human part, silhouette, human shadow and held stick; reconstruct the wall, street and obscured surfaces. Keep the exact street/mural/perspective/palette and keep the broken/splintered counter, grill, dropped sandwiches/condiments, scattered planks and debris. Same polished non-graphic cartoon style. It is an environment-only aftermath plate for live animated supporters; no people, flags, weapons, fire, injury, text, logo, UI or watermark.

## Nueva Chicago level art and Coca service assets — 2026-10-04

- `Resources/street-background-chicago-v1.png`: full portrait background plate based on the existing open-street layout and user-supplied Nueva Chicago mural/corner references. The game keeps the same crowd street, counter, floor, and lower safe-management band; Chicago motifs are rendered in the existing outlined cartoon style. Displayed only for level 2.
- `Resources/street-new-chicago-crest-v1.png`: standalone green/black/cream C.A.N.CH. shield for the level footer.
- `Resources/street-coca-bottle-v1.png`: isolated Coca-Cola 600 ml bottle sprite for order bubbles, workers, and product selection.
- `Resources/street-beverage-barrel-v1.png`: isolated blue ice barrel stocked with Coca bottles, used beside the grill as the level-2 drink station.
- All four outputs were generated with built-in ImageGen; prop sprites and crest are transparent RGBA, the background is opaque RGB. New `.meta` GUIDs/import settings were created without replacing existing art.

### Prompt summaries

```text
Background: repaint the existing portrait Hay Chori y Paty open-street gameplay background as a Nueva Chicago location, using the supplied stadium-corner/mural photos only as references. Retain the existing functional layout and blank lower controls band; add black-green club wall, C.A.N.CH. badge, stars and Mataderos mural motifs, drawn as warm outlined 2D mobile-game art. No gameplay sprites, HUD or buttons.

Coca bottle: one isolated full-body 600 ml Coca-Cola original bottle, recognizable red label and cap, clean outlined cartoon-game illustration, centered and fully visible on transparent background; no extra props or scene.

Drink station: one isolated blue open-top street ice barrel filled with ice and several red-labeled Coca bottles, same warm polished outlined 2D game-art style, whole object visible on transparent background; no floor or people.

Club shield: one isolated C.A.N.CH. football shield, cream upper field with black C.A.N.CH. lettering and alternating black/green lower vertical stripes, bold outlined game-icon style, centered with transparent background.
```

Generated source outputs remain under `/Users/celestino/.codex/generated_images/01a107c9-7a49-7782-bfac-9bc8d5a60f16/`.

### Nueva Chicago fan wardrobe atlas — 2026-10-04

- Project asset: `Resources/ChicagoWardrobe/chicago-fan-wardrobe-sheet.png`, 1254×1254 RGBA, true transparent background, 2 columns × 4 rows. SHA-256: `9aae200c8ab5d1f3763d32080b3c43227b8e776d2f0aa203701eea3a12caa4db`.
- Row-major order: green/black striped jersey; black jersey with green band; white jersey with green/black panels; black tee with green shoulders; black/white/green track jacket; green hoodie; white sweatshirt with green/black band; black/green windbreaker with a blank shield patch. Renderer chooses a repeatable outfit by customer ID only on level 2; Floresta art stays unchanged.
- Style/color source: official store's 2026 kits, shirts, sweatshirts, jackets and windbreaker in the green/black/white team palette: https://tiendanuevachicago.com.ar/ . Generated artwork is generic; no store photos, exact crest or logos are reused.
- Generated with built-in ImageGen, transparent background, no reference image, and no external brand marks.

```text
Create eight separate flat-front upper-body fan garments as a transparent sprite sheet with exactly two columns and four rows. All garments are broad readable silhouettes with sleeves, centered inside equal invisible cells with no overlap or dividers. Top to bottom, left to right: forest-green jersey with black vertical bars and white trim; black jersey with green chest band and white piping; mostly white jersey with green/black panels; black tee with green shoulders; black zip track jacket with white sleeve panels and green piping; deep-green hoodie with black sleeves and white opening; off-white sweatshirt with green/black chest band; black windbreaker with forest-green blocks, white zipper piping and a blank shield patch. Use polished warm outlined 2D cartoon game art, cel-shaded fabric folds, true alpha; clothing colors only dark green, black and off-white. Inspired broadly by official-store garment categories/colors, not an exact replica. No people, mannequin, hanger, photo, text, sponsor, brand, crest or watermark.
```


## User-selected startup cover — 2026-10-05

The user supplied the portrait illustration and selected it as the latest game presentation cover. It is retained byte-for-byte as `Resources/street-cover-user-v5.png` (941×1672 PNG, SHA-256 `e7bc3d59b722432c53da3eb63a6bfd3e18dd5ebd22ab607af097d4b52b791462`) with fresh Unity metadata cloned from the previous opaque cover importer (no mipmaps, no resizing, Android RGBA32). `StreetView` loads this scene as the single full-screen intro background; the original logo reveal, timing, menu buttons, and separate gameplay art stay unchanged.

## Nueva Chicago integrated fan outfit atlases — 2026-10-05

- `Resources/street-chicago-fans-front-v1.png` (1254×1254 RGBA, SHA-256 `4e9ea6ea1b2c67897032384567d40d1b973b1f35bc25002e2df1d1900e9396a8`) and `Resources/street-chicago-fans-walk-v1.png` (1254×1254 RGBA, SHA-256 `a4a516527fdea8b524e9a2ace74ba877c3b5360d2ec7aedee36d406325b1a7da`): matching 3×3 atlases with eight distinct complete fan identities/outfits in green, black and off-white; bottom-right cell is unused. Clothes, shoes and body are one sprite, not runtime layers.
- `Resources/street-chicago-riot-fans-v1.png` (1774×887 RGBA, SHA-256 `9a1e58e3627cc241400543cca5da4d04973e71438c7db12cbb50c2334ebdbbe7`): 4×2 set of four Chicago-outfitted angry supporters in raised-stick and non-contact swing poses. Used for Level 2's timeout animation so those outfits remain integrated while rioting.
- Built-in ImageGen referenced the project's existing All Boys complete outfit art, Chicago garment concepts and generic riot poses to retain the game's visual language while preserving Chicago green/black/off-white identity. Android importer is transparent RGBA32, uncompressed, mipmaps off, NPOT scale off.

```text
Chicago front atlas: create eight original complete full-body Nueva Chicago supporters in a precise 3×3 transparent sprite grid. Use distinct cartoon identities and green/black/off-white outfits inspired by generic striped jerseys, chest-band shirts, jackets and hoodies; no copied crests, logos, text or retail product photography. Cheer facing forward, full body from hair to shoes, common scale and safe cell gutters.

Chicago walk atlas: match the eight identities and outfit order from the Chicago front atlas, each full body in a right-facing profile walk pose with alternating legs/arms, same 3×3 transparent grid, style, scale and colors.

Chicago timeout atlas: create a transparent 4×2 atlas of four full-body Chicago supporters, same polished outlined game cartoon style and coherent green/black/off-white outfits. First row raised plain wooden sticks, second row same identities swinging non-contact; angry animated expressions, no logos/text/background.
```


## All Boys complete outfit fan atlases — 2026-10-05

- `Resources/street-allboys-fans-front-v1.png` (1,429,253 bytes, SHA-256 `a1517a77c8b9ed8a9b4b62fbbe66aba842f096e53c882915ad34c256ce2cee46`) and `Resources/street-allboys-fans-walk-v1.png` (1,433,062 bytes, SHA-256 `b1ce911623e9db239e6dc3b99a071cd22cc9f82525f5bef98b9d3a5bcb304b5c`): two 1254×1254 transparent RGBA 3×3 atlases. Row-major cells repeat the same nine identities/styles: white/black home jersey; charcoal/white piping; broad black chest band; black tee; monochrome polo; gray tee; white long-sleeve thermal; black/white track jacket; cream/black hoodie. Both poses depict full characters from hair through shoes with garments painted into the body art. No garment is runtime-layered. The styles reference retained neutral candidates 01/05/06/10/11/12/13/14/15; blue/pink/bright goalkeeper variants are excluded.
- Generated with built-in ImageGen using the existing `street-characters.png` plus the neutral candidate illustrations as style and garment references. The front atlas established the outfit/identity ordering; the second prompt used that front atlas and existing people atlas for matching profile-walking characters. Generated originals remain under the Codex image generation output folder; project copies have independent Unity importer metadata.
- Intended import: alpha transparency on, mipmaps off, NPOT scaling off, uncompressed, Android RGBA32.

```text
FRONT: Create a transparent sprite atlas, exactly 3 columns × 3 rows, with nine separate complete cheerful All Boys supporters. Every cell contains exactly one full-body character from hair through shoes, feet aligned at the bottom with transparent gutters, centered and independently readable. Keep one coherent family of small expressive round-headed 2D cartoon people with warm skin, bold dark outline, lively faces, varied hair/age/identity and the same proportions/style as the provided game fan atlas. Row-major outfit order: white shirt with black vertical bars; charcoal jersey with white piping; white jersey with one broad horizontal black chest band; black cotton tee; white-and-black polo; medium-gray tee with black accents; white long-sleeve thermal with black trim; black-and-white track jacket; cream hoodie with black details. Clothing palette strictly white, black, charcoal, gray, muted cream; no blue/pink/green/orange, no visible overshirt layer. Natural hands/arms/shorts/skirts/trousers/shoes belong to each complete body, varied neutral styles suitable for All Boys supporters, upbeat front-facing cheering poses. Match the overall visual language and scale of the source without copying its people exactly. No background, dividers, captions, letters, sponsor, branded logos, exact crest, watermark, flags, cropped bodies or extra limbs.

WALK: Make a matching transparent 3×3 atlas in exactly the same order and same nine identities, hair, face, skin colors, height, palette and clothing as the provided front-facing All Boys supporter atlas. One complete full-body cartoon supporter per cell, in a clear side-profile walking stride facing RIGHT; separate alternating leg positions, visible feet, natural swinging arms, clothing forms part of the whole body, not a pasted garment. Same camera scale, bold outlines, warm clean mobile-game style and transparent gutters. Keep clothing monochrome white/black/gray/charcoal/muted cream. No background, props, text, logo, watermark, blue/pink/green/orange clothing, cropped parts, extra limbs or panels crossing cells.
```


## All Boys riot outfit continuation — 2026-10-05

Built-in image generation used the nine complete Floresta outfit sprites and existing raised-stick/swing riot atlas as visual references. The generated transparent atlas keeps the same nine outfit slots, each with adjacent raised-stick and non-contact swing poses; character/outfit pairs now use the same customer-ID wardrobe index as the normal front and walking atlases. A final edit reduced each sprite slightly to provide safe transparent gutters. Asset: Resources/street-allboys-riot-fans-v1.png (1774×887 RGBA, 1,479,593 bytes, SHA-256 6cb8574b6f1662e8bd7bce9201e948467571e6e6338570ccb0ef4fec1564f97f, 6 columns × 3 rows); importer keeps transparent sprites, no mipmaps/NPOT resizing, uncompressed Android RGBA32.


## Nueva Chicago riot outfit continuation — 2026-10-05

Built-in image generation referenced the eight complete Chicago front-facing crowd outfits and the original Chicago angry/stick-swing atlas. It produced a transparent 4×4 atlas: eight wardrobe variants in existing customer-ID order, each paired with raised-stick and non-contact swing poses. A final edit added safe transparent margins while preserving outfits and identities. Asset: Resources/street-chicago-riot-fans-v2.png (1254×1254 RGBA, 1,350,013 bytes, SHA-256 c9581b3ec09c607a292fed8ce2bfc5d8baa25d0cb055c80ad1dabe7aaced928a, 4 columns × 4 rows); importer keeps transparent sprites, no mipmaps/NPOT resizing, uncompressed Android RGBA32.


## Timeout-to-trifulca impact cloud — 2026-10-05

`Resources/street-riot-fight-cloud-v1.png` (1774×887 RGBA) is a transparent, broad cartoon dust cloud in warm ivory, beige and gray with dark hand-inked outlines, yellow impact flashes and speed swirls. It is a single isolated overlay asset, animated in `StreetView` by rapid scale-in, a subtle pulse/drift, and fade-out. The sprite is composited over the queue only during the transition; the frozen crowd and destroyed-stall reveal continue behind it. Android importer uses RGBA32 without compression; mipmaps and NPOT scaling are disabled.

Prompt: “One transparent-background visual-effects sprite for the provided cartoon game's art style: a large wide horizontal irregular billowing dust-and-impact cloud, as in a playful comic brawl. Dense overlapping warm ivory, beige and soft gray smoke puffs, dark warm-brown hand-inked outlines, subtle painted shading, a few small yellow-orange impact starbursts and curved motion streaks peeking from behind. Single cohesive mass with lumpy puffs on top, broad enough for a mobile game's center screen transition. Isolated and centered with true alpha transparency around it. No people, no hands, no bodies, no weapons, no injuries, no blood, no text or letters, no scenery, no ground, no shadow. Match the supplied game's hand-painted cartoon shading and heavy clean outlines.”

SHA-256: `693fbe95da43432a358a17369797b5da8503c215160ba639bc1807cf9b103223`.

## Selected wood-and-iron victory popup — 2026-10-05

Original built-in ImageGen asset: Resources/street-victory-popup-wood-v1.png (1122×1402 RGBA). Generated as a transparent blank football/wood/iron/parchment panel with a green exit button and three decorative icons; ALL heading, captions and numeric values are rendered live by StreetView. Copied without raster editing/resizing. Import is full resolution, mipmaps/NPOT scaling off, Android RGBA32. User-selected concept and existing gold coin were style references; gameplay background was excluded. Unity import/Game-view review deferred.

### Exact generation prompt

Create ONE original transparent production UI artwork for the Argentine 2D cartoon mobile game Hay Chori y Paty. Image 1 is the user's chosen popup composition/style reference, not a background to reproduce. Image 2 is the game's gold coin motif to follow for the coin illustration.

Deliver ONLY the isolated victory popup panel on a real transparent alpha background: no gameplay screenshot, no scenery, no dark overlay baked in. Tight portrait composition approximately 4:5 aspect, panel fills almost the whole canvas with a small transparent margin, every ornament and bottom button fully visible.

Match the selected wood-and-forged-iron football-stall aesthetic: rich warm brown horizontal wooden planks, grain and worn edges, thick black/charcoal riveted iron brackets, subtle brass accents, thick clean cartoon contours, soft glossy hand-drawn shading, cream worn parchment inset, NOT photorealism or 3D. At the top, a cream/black football in a gold-rimmed medallion, small green laurels, two gold stars and two small waving black-white-black flags. Large wide blank wooden header below the medallion; reserve room for a two-line runtime heading. Below it, a wide cream parchment content area with three roomy equal horizontal rows and two fine dark dividers. Left illustration of first row: one delicious Argentine choripán in French bread, second row: a small stack of 100% gold game coins with embossed food/monogram details following image 2, third row: a large cream-faced stopwatch with blue rim. Left icons take only about 18% of content width. Leave the middle and right of every row completely BLANK to overlay dynamic labels and values in Unity. Under the parchment, a large glossy bright green rounded button enclosed by black riveted iron, about 60% panel width, centered, its entire green center completely BLANK.

Normalized design targets within the trimmed panel: ornaments y0.00–0.13; blank wood title y0.15–0.34; parchment y0.37–0.80, with row centers around y0.44,0.58,0.73; green blank button y0.84–0.97. Maintain these spacious regions and balanced width. Do not paint any letters, words, digits, amounts, currency signs, labels, logo, text, watermark or interface outside the panel. No 'turno', no 'salir', no numbers: ALL lettering is rendered live by the game. Only one finished asset, not a concept sheet.

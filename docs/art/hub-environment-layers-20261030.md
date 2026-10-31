# Hub environmental layers — source/art notes

Built-in image_gen editing mode; no external CLI/API. Original master retained: `Assets/Art/Street/Resources/street-hub-stadium-v1.png`, 841×1870, SHA-256 `0573cc633b2d7aa2bfdde5433d1c1a01b75665b66310fc6e4924ce9e7305c098`.

## Prompt set

- Clean plate (precise-object-edit): preserve exact canvas/camera/composition, characters, lettering and surroundings. Remove only five cloth flags on stadium roof (keep poles), hung black/white shirts on right fence (keep fence/vendor/crest panel), and dangling fabric valances of the two awnings. Reconstruct clean sky/stadium/fence/shop behind removed cloth. No new objects/redesign or changes to people.
- Flags (background-extraction, transparent_background=true): extract only five black/white stadium-top cloth flags, preserving folds, stripes and outlines; exclude poles/background/street flags/people/text, actual transparent alpha, source canvas placement requested.
- Shirts (background-extraction, transparent_background=true): extract only hung black/white shirts, original cloth/stripes, exclude fence, vendor/hand/face, beige crest panel and folded merchandise; actual transparent alpha, source canvas placement requested.
- Awnings (background-extraction, transparent_background=true): extract only upper red/cream grill-stall and lower red/orange grocery fabric canopies, original perspective/scallops/shading; exclude supports/walls/windows/lamps/characters; actual transparent alpha, source canvas placement requested.

## Integration facts

Generated foreground placement did not remain exactly aligned to the master. Runtime uses independent top-origin normalized source-UV rectangles and artwork-space destinations, NOT inferred pixel identity. Flags/awnings/clean plate 841×1870; shirts 928×1695. New assets under `Assets/Art/Street/Resources/StreetHubEnvironment/`, no original asset overwritten. Cleanplate readable+sRGB; foreground native alpha; NPOT rescaling/mipmaps disabled and max size2048. Runtime creates only local feathered patches behind cloth, leaving the rest of the master intact. Cloud shadows, small bird wings and warm glows are separate code-native layers, not baked into art.

Visual acceptance is tracked in `specs/features/home-hub.md`; generated art alone is not proof of successful in-game compositing.

## Uniform-mapping correction / acceptance

The final renderer maps master-space bounds directly to physical pixels via the same ScaleAndCrop backdrop transform and restores GUI.matrix afterwards. UI LayoutRect is deliberately not used: its y-only vertical scaling truncated patch height at1080×2400. Birds are now cached code-native curved-wing/body silhouettes, not rectangular strokes. Seven focused EditMode cases and one PlayMode case passed; independent full Game View review passed at1080×1920/2400 (master and poses0/2/5). Evidence: Logs/Acceptance/HubLayers-20261030/final/. No Android/APK acceptance implied.

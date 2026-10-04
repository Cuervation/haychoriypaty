# Original street-service art

Generated with the built-in image_gen tool (no external API/packages), 2026-10-03. Original cartoon art, not copied from Food Fever. All sprites are alpha-backed and render independently; text is live UI, not baked labels. Existing art/metas are preserved.

- `Resources/street-background.png`: portrait frontal/slightly elevated Floresta sidewalk outside All Boys; black-white pennants/canopy, empty product signs, horizontal wood/iron counter, open paved worker zone, condiment mesita, blank lower management area. No baked customers/equipment/UI; no industrial kitchen/3D/photorealism.
- `Resources/street-characters.png`: 4×4 sheet; red-apron bandana parrillero front idle/two walks/handoff, rear idle/two walks/profile pickup; four black-white fans idle/waving plus respective side walking poses. Big heads, small bodies, crisp dark outlines, rounded cel-shaded shapes; no club logos. Runtime directional selection, flip, bob and pickup/handoff pulses use actual simulation states.
- `Resources/street-items.png`: 20 individually bounded sprites: seven definitive products (four sandwiches on bread, small cola cup, large fernet cup, beer can), Argentine iron parrilla/embers, cooler, mesita, coin, bubble, upgrade card, Start button, modal panel, speed shoe, bandana worker portrait, lock, star, smoke. Same vivid rounded cartoon language; no extra products.

Sheets are not exact geometric grids. StreetView has reviewed source bounds, preserving alpha and aspect; importer disables NPOT resize/mips/compression. UI cards deliberately stretch their empty backgrounds, icons preserve aspect. Import and runtime checks are recorded in specs/status.md, not inferred from image generation. Source video frames are analysis-only under ignored Logs/ReferenceAnalysis, never gameplay sprites.

- `street-background-v2.png`: targeted built-in imagegen edit moved/resized counter to32.5–40% image height; preserved style, neighborhood, blank worker/UI areas. It is the active backdrop. Tight alpha-derived character rects prevent equal-grid padding from shrinking characters.

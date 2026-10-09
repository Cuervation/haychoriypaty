# Technical decision log

## 2026-10-08 — Ten-point clearance for grills

Set the common prep-row-to-grill gap to10 logical units. Preserve table/barrel sprite sizes and prevent barrel/grill overlap by aligning barrel bounds with the table row bottoms; move barrel pickup anchors with that small station shift.

## 2026-10-08 — Four-point clearance for grills

Reduce the shared prep-row-to-grill clearance from10 to4 logical units at the user's request. Keep station dimensions and barrel alignment unchanged; retain the active catalog non-overlap and grill safe-bottom checks.

## 2026-10-08 — Shortest safe employee routes

Use catalog-derived visibility routing around active physical station solids expanded by the worker's36×12 feet, preferring a clear direct/diagonal segment. Recalculate outbound, return, and subsequent-unit legs from the employee's actual position; never reverse a fixed route or force a trip home. Do not add worker-vs-worker collision, NavMesh, or packages.

## 2026-10-08 — Native distance-driven worker cutouts

Reuse original contact/carry poses and 20 missing-direction poses through pooled torso/leg/finger SpriteRenderers in the existing kitchen camera. Compose two contact and two passing samples per direction, advancing by actual traveled distance rather than idle AnimationTime. Worker palm anchors and real carried objects share depth/scale; do not bake product copies or add an animation camera. This preserves worker proportions and gameplay while avoiding per-frame texture work. Release cached sprites before their palette textures on view disable.

## 2026-10-07 — All Boys master geometry, club scenery and fresh attempts

Use StreetSceneLayout for root art/counter/crowd/service geometry and the existing StreetWorkstationLayout for furniture/pickup routes. ClubVisualTheme supplies mural art/crops and scenery-only light temperature, never gameplay positions. Render the root plate in every current level; uniform-fit specific mural panels below the fixed HUD. Remove per-background counter heights, Chicago-only service/approach heights and the duplicate counter vertical transform. Every fresh load/start/select/advance/retry resets to one Parrillero, no free Cocacolero and speed ×1.00; existing purchases apply only within the active attempt. Preserve original art, apparel, progression, prices and save-schema compatibility. Uneven matched Cocacolero atlas rows use measured crops scaled to imported dimensions, not an assumed 4×4 grid.

| Date | Decision | Reason |
|---|---|---|
| 2026-10-06 | Fix all seven catalog products at $5 for every level; remove all price pop-ups and price controls, while retaining legacy save fields/setter APIs as normalized compatibility shims. | The user made product price fixed across all levels and requested removal of every price popup; $5 is the existing default and confirmed Chicago chori/Coca price. |
| 2026-10-03 | Target Unity 6000.6.3f1 already installed; avoid nonessential package additions. | Match environment; keep initialization light. |
| 2026-10-03 | Preserve the existing URP template, sample content, and cached packages; add the prototype as a separate main scene. | Avoid disturbing the user's existing Unity setup/scene while keeping the first level isolated. |
| 2026-10-03 | Use project-scoped `.codex/agents/*.toml` with native custom agents and `.agents/skills/*/SKILL.md`. | Current local Codex CLI 0.159.2 supports project custom agents and repo skills; no global config edits. |
| 2026-10-03 | Limit concurrent agent capacity to 2; delegation is optional, not an obligatory pipeline. | Explicitly cap coordination/token overhead. |
| 2026-10-03 | Implement the first GUI with built-in IMGUI and procedural placeholder shapes. | No new packages or final-art dependency; replace view later without replacing game rules. |
| 2026-10-03 | Do not launch batch Unity while a project Editor owns the lock. | The existing editor must import new files; scene play/import validation remains pending until it is closed. |
| 2026-10-03 | Treat the supplied cartoon gameplay screenshot as mandatory style/composition guidance, not an asset source; use an original provisional 2D Floresta scene plate while keeping simulation code separate. | User made the visual language fundamental; original generated art improves the prototype now, while individual sprites/animations remain a scoped next step. |
| 2026-10-03 | Use CoplayDev MCP for Unity v10.0.0 over loopback HTTP for editor operations; keep Codex pointed at `127.0.0.1:8080/mcp`. | The project uses Unity 6 and already had the matching Codex endpoint; pin the editor package, avoid duplicate MCP entries, keep the editor bridge local, and retain specs as the source of truth. |
| 2026-10-03 | Retain IMGUI rendering, route mouse/primary-touch buttons through the already-installed Input System; share UI hit rectangles and require matching press/release. | Input System-only does not send runtime OnGUI input. Avoid Both/restart, new packages, or an unrelated UI migration; PlayMode pointer tests cover the real route. |
| 2026-10-03 | Use original empty backdrop + cropped atlas with frame-based vendor states; keep simulation separate and existing art/metas intact. | Baked supporters were unrelated to queue state; generated atlas is not a precise grid, so explicit authored UV bounds and NPOT-none prevent clipping. |
| 2026-10-03 | Add focused EditMode simulation and PlayMode pointer tests using the existing Unity Test Framework; preserve runtime Assembly-CSharp and scene GUIDs. | Previous runner had zero tests. Reflection from isolated test assemblies avoids a runtime assembly migration. |
| 2026-10-03 | Validate through the already-open Unity MCP editor; capture IMGUI asynchronously without inline image. | No second editor/lock contention. v10 synchronous capture steps can pause Play and fall back to camera-only output; async files include actual UI. |
| 2026-10-03 | Build the Android prototype as IL2CPP ARM64 APK with bundled tools; reuse API36 AVD ARM translation + SwiftShader and one safe-area transform for GUI/input. | Unity6000.6 no longer supports x86_64 builds; no new dependencies/AVDs, keep touch aligned and exclude system insets. Emulator QA is not physical-device certification. |
| 2026-10-03 | Use native GameActivity IMGUI as the sole Android action owner; retain Input System-only pointer bridge in editor/desktop. | Real Android APK exposed duplicate upgrade charges with both routes, while a bridge-only APK did not start. Native-only player passed exact purchase/replay/victory checks; no Input Handling/package change. |
| 2026-10-03 | Supersede the first-prototype delivery boundary with the user-requested street automation milestone; preserve its historical tests/assets. | Explicit new request requires mass orders, moving workers, prices and five-location progression, still exactly seven products. |
| 2026-10-03 | Separate StreetGame lifecycle/save, deterministic StreetSimulation movement/reservations and replaceable StreetView; retain platform-specific sole input owner. | Real handoff must gate unit decrement/revenue; small bounded simulation and a dedicated local JSON save meet requirements without frameworks/packages. |
| 2026-10-03 | Replace defective browser sampling with complete 1,798-frame VP9 decoding and reviewed temporal contact sheets; distinguish observed behavior from requested extensions. | Initial capture repeated frame zero. Actual clip shows price adjustment, transition, 21 mass orders, one walking chef and counter/coin changes, not purchases, multiple workers or level progression. |
| 2026-10-03 | Make per-product prices alter automatic capacity, bulk size, arrival interval and product selection as well as actual unit revenue. | Prevent dominant high-price/unchanged999 demand; default $5 preserves the reference-like crowd. Curves are inspector balance choices, not observed video mechanics. |
| 2026-10-03 | Put multi-product price selectors below panel title, outside the live HUD. | Actual AndroidChicago screenshot exposed coin/sales overlap at y648; shared hit rectangle moved to y273 with focused pointer regression. |
| 2026-10-03 | Run Floresta for the full180seconds and evaluate its24-unit target only at the clock; preserve later-level early wins. | User chose a three-minute trial. Clock-only limit was already180, but earlyWin24 shortened the round; keep upgrades/prices/economy and defer waves/rebalancing. |

| 2026-10-04 | Scope fixed$5chori, +10%base/$5speed, $25helper and1–4choris to Floresta; retain later price/demand/cost rules and180s. | User explicitly changed first-level loop and requested noAPK. Fixed per-purchase prices avoid old escalation; completed orders use existing receipt/exit/free-slot replacement, not teleporting clients. |
| 2026-10-04 | Migrate save v1→v2 by resetting staff/speed to one worker and ×1.00; preserve coins, prices and unlocked locations. | User found the game opening with development values (8 workers and ×7.5) and explicitly requested a one-worker, base-speed start. Keep the reset one-time so later earned upgrades persist normally. |
| 2026-10-04 | Add original monochrome cartoon supporter art to a versioned Floresta far-wall backdrop; retain existing composed background and foreground layout. | User identified the opposite All Boys wall mural as the level-1 landmark and required the same art style. Avoid tracing exact mural artwork/marks and keep scene interaction areas unobscured. |

- 2026-10-04: Replace miniature hot-food BBQs with one broad original iron parrilla (494x108 for1–4 unlocked foods;280x90 when drinks need the right side). Keep simulation pickup anchors and all seven products; only unlocked markers are drawn. Original grill remains missing-resource fallback. Build APK only, no phone/emulator control.
| 2026-10-04 | Apply wardrobe PNGs as deterministic per-customer overlays only in Floresta instead of repainting the shared fan atlas. | Retains every existing fan pose and keeps later levels unchanged while using the new apparel options.
| 2026-10-05 | Restrict active All Boys fanwear to nine white/black/neutral wardrobe variants; exclude blue, pink and bright goalkeeper outfits from the crowd rotation. | The previous mix looked unlike the club's supporters; retain the original poses, art assets and later-level appearance while reinforcing All Boys' monochrome identity.
| 2026-10-05 | Render Floresta supporters with their original pose-matched black/white All Boys fan sprites; archive universal standalone shirt overlays until they can fit each pose. | A single fixed torso sticker visibly conflicted with sleeves/arms and side-walking poses; the source atlas already has consistent complete team clothing for every active pose. |
| 2026-10-05 | Keep Level-1 All Boys uniforms integrated into the complete supporter sprites; never layer standalone garment stickers at runtime. | The screenshot showed the added casual garments did not read as naturally worn club apparel; the existing full-body fan poses already contain coherent black-and-white team clothing. |
| 2026-10-04 | Keep the supplied cover/logo intro in StreetView with unscaled timing and a central action guard; no new scene/controller/package. | Reuse the 2D renderer and both existing platform input owners, avoid an Android tap starting the round behind the cover, and preserve gameplay. |

| 2026-10-04 | Replace auto-dismiss intro with≥4s fully visible cover +≥4s fully visible logo, then persistent Jugar/Salir; author glossy blue button texture and bundle Apache 2.0 Luckiest Guy. | User explicitly requested NEXT-blue styling and direct Play/Quit. Keep StreetView/unscaled timing/shared input guards; no new scene, package or game rules. No tests/APK until later combined validation. |

| 2026-10-04 | Floresta speed costs $25 and helper $200; reset first-level staff/speed on simulation construction and every StartRound to one parrillero/x1.00. | Explicit user request overrides first-level purchase persistence. Keep coins/unlocks, +10% increments and later-level behavior; synchronize Main serialized balance with defaults. No tests/APK now. |

| 2026-10-04 | Keep the seven existing columns as FIFO lanes: compact on departure, admit newcomers only at tails, serve only settled front customers. | User forbids newcomers replacing older waiting people at the front. Add Advancing state/reuse walking/bubbles; cancel stale reservations without income and keep bounded population, art/layout and other rules. No tests/APK now. |

| 2026-10-04 | Versioned v4 backdrop crops upper framing to mural, removes stadium/roof/signage/supports, and widens only the crowd street behind the retained counter; remove obsolete top sign-product overlays. | Explicit user request for readable queues/orders. Keep counter/floor/station/HUD/input anchors and previous art/GUIDs; built-in image edit, static review only, no tests/APK. |

| 2026-10-04 | Animate stationary supporters procedurally around their feet, using existing AnimationTime and per-ID rhythms; share transform with clothing and restore it before bubbles. | User wants lively waiting fans. Front atlas entries are different identities, not idle frames; avoid swapping people/new assets or moving logical FIFO anchors. No tests/APK now. |

| 2026-10-04 | Reuse the exact presentation logo for default/Android single-layer icons; compose adaptive foreground with native percentage-inset XML during Gradle generation over a cream background. | User wants the existing logo, not redesigned art. Native composition preserves PNG bytes and keeps lettering inside launcher masks without raster edits/new dependencies; source-only until the next authorized build. |

| 2026-10-04 | Floresta wins on real unit1000with180s deadline preserved; stop workers at threshold. Place thin upper-corner balance/progress panels above rear bubbles and reuse a generated gold-only coin for HUD/income effects. | User explicitly chose1000sales, retained180s and rejected colored coin accents. Synchronize defaults/fallback/Main; preserve FIFO, fixed$5, costs25/200, reset and later rules; no tests/APK now. |


- 2026-10-04 combined delivery: latest explicit user request supersedes earlier deferred tests/builds. Validate only affected Street logic/art/input with updated fixtures; retain historical evidence as historical. Version new APK0.2.5/code7 and archive it; use same debug certificate and`install -r` to preserve phone data. No device install/exit success claimed while Motorola is disconnected.

- 2026-10-04 full-screen intro: decouple edge-to-edge decorative backdrop from the540×960 safe-area interactive canvas; exact existing Parrillero portrait is a foreground layer, avoiding AI likeness drift. Replace only the backdrop with a versioned no-hero scene; gameplay transform/input/timing/logo unchanged.


## 2026-10-04 — Exact progressive purchase tables
Use explicit serialized cost arrays shared across locations, not fixed prices or a growth formula. Array rows bound purchasable speed/staff tiers; capped cards display MAX, never a free extra purchase. Update Main and defaults/fallbacks together, clamp old purchases in the simulation constructor without changing save version2, and retain attempt-local coins and first-level baseline reset.


- 2026-10-04 unified HUD: original cached procedural amber frame/colored capsules plus existing coin/product art and bundled Luckiest Guy, rather than a generated bitmap with baked values. Keep all three statuses in logicaly2–36 above rear bubbles, derive m:ss solely from simulation and release runtime texture on disable; lower timer removed. Gameplay/input and pending upgrade-card selection unchanged.


- 2026-10-04 real mural: latest user explicitly requests recognizable real street/corner motifs rather than the earlier generic fan reinterpretation. Preserve photos as appearance references (no watermark/photo paste). Because two generated full plates shifted lower gameplay bands, consume only new wall crop as a separate native layer over exact v4 base; retain all lower geometry and existing assets.


- 2026-10-04 location badge: reuse licensed bundled Luckiest Guy instead of installing a font, and retain original club-hosted crest PNG byte-for-byte instead of AI logo recreation. Center icon+caption as decorative footer only for Floresta; auto-fit other location names without All Boys mark. Record third-party source, no licensing/endorsement assumption.


## 2026-10-04 — Isolate batch pointer focus in test fixture
When no editor/MCP is running, use installed6000.6.3f1 sequential batch checks. Queued pointer fixture temporarily setsIgnoreFocus andAllDeviceInputAlwaysGoesToGameView, preserving/restoring enum values on existingInputSettings. Do not clone/replace transient settings: InputManager destroys the previousHideAndDontSaveobject. Runtime native/editor ownership unchanged; final15/15PlayModepassed.


## 2026-10-04 — Fill gameplay portrait by extending logical height
User superseded gameplay letterboxing with full-height screen fill. Preserve uniform screen scale (avoid oval buttons/portraits) and all touch areas: make logical portrait height aspect-responsive, remap only vertical anchors for drawing and invert the same mapping for pointer input, with ScaleAndCrop on scenery rather than UI. No scene/simulation/input ownership or asset change.


## 2026-10-04 — Route choripán pickups to loaded trestle table
Place a slightly used, fully loaded wooden caballete table in the lower-left foreground beside the shared grill. Route product-zero workers to its right edge, but retain pickup anchors for other products; draw the table behind live station labels/workers and avoid a duplicate floating chori badge. This adds a visible supply surface without changing recipes, unit delivery, or income.
| 2026-10-04 | Discount only Floresta's upgrade costs to make the 200-sale first level playable; retain later-level prices and +10% speed tiers. | The current progression spent income on speed but reached only 82/200; the tuned earned-income playthrough reached 200/200 in 98.9 seconds. |

- 2026-10-04: Gameplay gray strip was safe-area-only rendering, not disabled Android fullscreen (settings already enable immersive/outside-safe-area). Bleed scenery across physical display and anchor read-only HUD separately, keeping safe control transforms. Floresta table-left/grill-right uses one explicit clear waypoint in both directions, preserving physical pickup/handoff gating instead of masking actors or minting sales on a timer. Later product stations/economy unchanged; no tests/APK requested.

## Render the timeout riot as an unscaled queue animation — 2026-10-05

Keep the loss sequence in existing `StreetView`/IMGUI rather than adding an Animator, scene or package. Reuse the frozen simulation queue/slots, transition each fan into a two-pose angry atlas, and fade a people-free broken-stall environment behind them. This preserves the exact affected customers and keeps replay/results independent of the stopped simulation.
## 2026-10-05 — Reduce the Floresta timer to two minutes

Set Level 1 to 120 seconds at the user's request. Keep the 200-handoff early win, deadline-loss behavior and all later-level durations unchanged.
## 2026-10-05 — Set the Nueva Chicago timer to three minutes

Set Level 2 to 180 seconds at the user's request. Keep Floresta at 120 seconds and Levels 3–5 at their existing durations.

| 2026-10-05 | Keep each club’s supporter outfit consistent across normal and failure states by using the same deterministic customer-ID variant in complete-body crowd and riot atlases; apply to every future team. | Fans should remain recognizably dressed as their team when play transitions into a trifulca; separate riot art must not silently change their wardrobe. |


## Mask the timeout-to-riot cut with a short comic impact cloud — 2026-10-05

Keep the existing live `StreetView` riot sequence, but reveal it through one large transparent smoke-and-impact overlay. Animate its fast expansion and fade on unscaled time over the actual frozen queue; do not add combat logic, a new scene, packages or a persistent obstruction of HUD/results.

## 2026-10-06 — Preserve the existing Chicago team/economy while adding a Coca specialty

Keep the existing single free starting worker, five-worker cap and every current hire/upgrade price unchanged. In Nueva Chicago the first Parrillero remains the baseline; choose the next hired role from the least-represented specialty, making the first hire Cocacolero and keeping later hires balanced. This avoids adding a free worker or changing the established economy just to make the new role available.

## Level 3 Vélez integration — 2026-10-06

Use selector slot 3 for `Liniers - Velez Sarsfield`, preserving the slot's existing 65-unit goal, 240-second timer and demand setting. Keep catalog IDs fixed and offer products 0/1/4 (Chori/Paty/Coca); do not add new products or price rules. Reuse specialist roles: Parrillero owns the full Chori+Paty portion of one customer in ticket order, Cocacolero owns Coca, and the two may work in parallel. Use the existing queue, two-line bubble window, barrel, Cocacolero atlas and price systems. The level gets one exterior mural background, matching complete front/walk/riot fan outfits, a two-zone single grill and a team badge.

## Level-transition purchase baseline — 2026-10-06

Reset the next level to one Parrillero and speed x1.00 on successful advancement; let the active level's first upgrade rows determine the displayed hire and speed prices. Keep unlocks/product prices and within-level retry behavior unchanged.


## 2026-10-06 — Make the timeout result clear and return to level selection

Use the direct failure copy “No llegaste a entregar todos los pedidos.”, a pulsing/outlined GAME OVER below it, and a single Volver action. Volver resets the failed attempt and opens the unlocked level selector instead of restarting gameplay immediately. Preserve the existing riot, sales counters, outfit mapping and progression.
## 2026-10-06 — Integrate Ferro and Independiente as cumulative locations

Keep product IDs stable; Level 4 uses catalog order `0,1,2,4,6`, Level 5 uses `0,1,2,3,4,6,5`. Retain the shared specialist worker, FIFO, station-routing and multi-product order architecture, with a per-ticket cap of five types. Their new art remains replaceable resources and must keep the same customer outfit across front, walk and timeout/riot poses.


## 2026-10-06 — Permanent Nueva Chicago kitchen order

User explicitly fixes Chicago to barrel-left / centered-grill / ready-chori-table-right, superseding the old table-left/barrel-right composition. Keep the three assets proportional at 72% scale, y686 ground line and equal 56px visible gaps so each specialist can stand beside its station without putting a shoe on the grill. Food uses (395,510) → (395,665); Coca uses (155,510) → (155,648), returning through the same approach. Correct Chicago-only pose selection from existing art; no new animation assets. Preserve the old barrel rectangle and Coca crop table for Vélez/later levels. No changes to role ownership, FIFO, clients, requests, economy, upgrades, timers, goals or HUD.

- 2026-10-06: Replace alternating single hiring card/shared total-tier cap with independent role commands/cards. Reuse existing price values and maxStaff default per specialty; optional separate role caps. Zero Coca uses first cost row (same as count one) to preserve configured five-role cap without inventing an extra price. Persist composition on retry/load; keep advancement reset and all service/routes unchanged.

## 2026-10-06 — Expand Chicago parrilla capacity modestly

Keep the barrel-left / grill-center / ready-chori-table-right layout. Use a separate original grill sprite with exactly four visible chorizo rows (one added), compact bread rolls approximately chorizo-sized, and only a modest 10% horizontal increase; retain the common ground line and side clearances. Preserve station pickup routes and all gameplay values.


## 2026-10-06 — Resolve club colors for decorative pennants

Keep the All Boys pennants baked into its existing backdrop so its appearance remains pixel-identical. Because later club backdrops do not share that decoration and Chicago still has a baked monochrome row, render one cached transparent pennant strip from the club theme after the selected backdrop; use the same geometry for levels 2–5 and overlay Chicago's old row. Theme data is keyed to the active StreetSimulation.LevelNames names, not scene-specific colors. This avoids duplicate backdrop art and leaves layout/gameplay unchanged.


## 2026-10-06 — Fit order speech backgrounds to visible content

The live UI is IMGUI, not a prefab/uGUI layout. Reuse Items[11] with nine-slice border drawing and measure the current icon/quantity rows before drawing. Preserve the two-visible-line +N window and its gameplay-independent reveal behavior; three/four/five-product tickets share a footer-sized frame rather than adding artificial blank space. Transform the customer-relative tail baseline once on tall portraits, then keep local content sizes/offsets fixed. No additional sprite or UI system.

## 2026-10-06 — Reuse the cover as the only ordinary button style

Inspection found no active UI Button components or prefabs; Main renders IMGUI. Consolidate its existing DrawMenuButton into DrawStandardButton, retaining CreateMenuButton and Luckiest Guy instead of introducing a parallel system or assets. Cover controls retain exact mapping; wider/compact controls resize their middle only and overlap internal edges by less than one physical pixel to avoid fractional-transform seams. Migrate green result actions and Ready numeric navigation. Keep user-selected wood/iron upgrade cards and mural selector tiles as explicit design exceptions; leave inactive PrototypeView scenes untouched.

## 2026-10-06 — Freeze Floresta furniture frames, move secondary workstations

Live MCP inspection confirms stations are IMGUI textures rather than prefab/SpriteRenderer objects or physics colliders. Use one pure StreetWorkstationLayout configuration shared by existing rendering and simulation station APIs; do not add a parallel scene/physics/layout system. Freeze the measured282×94/196×98 main frames, proportionally render existing grill variants, reuse the Level1 table asset, and solve circulation with a staggered arrangement. Coca/beer height80 and source aspect ratios leave access while main furniture stays full-sized. All food pickup routes converge at the standard finished table; later route stages traverse the clear upper lane. Conservative full-sprite and foot AABBs validate other props; only target-station hand reach is permitted. On tall portraits, fade a local presentation correction near pickup instead of changing gameplay positions. Preserve all current art/metas/catalog. The previous Chicago single-row reduced composition is superseded by the user's new all-level size/access priority.


## 2026-10-06 — Temporarily share the Floresta upgrade economy across all current levels

For test builds, map every level in the current runtime catalog to one TEST_ECONOMY_PROFILE: Parrillero/Cocacolero hire rows $15/$30/$60/$100 (with the existing free starting Parrillero) and speed rows $5/$10/$15/$20/$30/$45/$65/$90/$125, +10% per purchase, capped at ×1.90. This replaces the prior active distinction between Floresta and later-level curves; fixed product prices, employee caps, speed increment, save/purchase persistence and gameplay remain unchanged. Keep level-index-to-profile mapping data-driven so later club-specific curves need only new profile data and mapping, not code branches. The Cocacolero's independent hire curve uses the same shared hire-cost source.


## 2026-10-07 — Catalog-derived worker/station authority

Keep stable product and original role IDs; append Premium/Fernetero. One StreetSpecialties mapping drives responsibility, stations, availability and cards. Share the current cost profile but count each paid tier independently. Extend saves to v3 without restoring legacy attempt purchases or resetting unlocks. Preserve normal furniture scale; add an equal-size independent Premium grill, catalog-selected kitchen graph and two-row4/5-card HUD. Reuse exact worker pose atlases via blue/black apron palettes rather than regenerate mismatched animations. Historical shared-grill/two-role evidence is superseded by this contract, not deleted.

Expanded route occupancy uses full36×12 feet, not projected90×98 body AABBs: frontal-elevated2D depth legitimately overlaps bodies and props. Requiring90px corridors for both282px grills and196px table in540px would force prohibited shrinking. Preserve original strict body-clearance tests for the no-Premium layout and pair expanded foot-route sampling with real portrait Game View inspection. Preserve/integrate the pre-existing removal of drink-station labels in the changed station renderer; do not reintroduce labels during this feature.

## 2026-10-07 — Approved real modular kitchen

User explicitly approved extending the single StreetSimulation with real food/stock/cooking and fitting the authored760-wide kitchen uniformly into540 canvas. See features/modular-kitchen.md for capacities/timings/initialpreparedstock, which did not exist previously. Render independent SpriteRenderer prefabs through transparent live targets composed with existing IMGUI scenery/HUD; this is not a staticbackground and does not change the counter/club/progression/economy. Preserve worker90×98 proportions and blue Premium/blackFernet palettes. Physical base footprints, not tabletop projection, constrain36×12workerfeet.

Keep original PNGs unchanged and create alpha-trimmed standalone Sprite.asset resources with Sprite.Create/CopySerialized. Unity6 spritesheet API is obsolete and Sprite Editor data provider assemblies are not enabled in this checkout; no new package is installed. Existing GUIDs survive repeated generation. No secondUnityeditor, service, parallelcookingengine or new manual gameplaycontrol.

## 2026-10-08 — Show beverage stock as a loose barrel pile

Widen both barrels20% horizontally while retaining their height, and scatter each barrel's existing12 real drinks into stable staggered positions/angles. Do not raise inventory capacity merely to create a fuller-looking barrel; keep the current service count and return each drink upright when carried.

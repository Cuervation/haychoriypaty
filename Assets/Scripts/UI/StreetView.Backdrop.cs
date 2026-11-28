using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Fondos, murales y banderines de partida. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private void DrawGameplayScreenFill(bool sceneryOnly = false)
        {
            // SafeArea is for controls, not a clipping boundary for scenery.
            // Paint behind the camera/cutout and navigation insets before the safe canvas.
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float scale = Screen.width / W;
            float fullHeight = Screen.height / scale;
            Rect viewport = CanvasViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            float canvasHeight = CanvasLogicalHeight(viewport);
            float bottomBleed = SafeAreaBottomInsetLogical(viewport, Screen.height);
            Rect pennantArtBounds = sceneryOnly ? Rect.zero : FooterPennantRenderBounds(
                CurrentResponsiveFooter(game.Sim, canvasHeight, bottomBleed).Pennants, canvasHeight, bottomBleed);
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                GUI.color = Color.white;
                DrawBackdropLayers(fullHeight, fullHeight / H, viewport, pennantArtBounds, sceneryOnly);
            }
            finally { GUI.matrix = previous; GUI.color = color; }
        }
        private static Rect FooterPennantRenderBounds(Rect layoutBounds, float safeCanvasHeight, float bottomBleed)
        {
            // A zero slot means the playfield/footer has no safe pennant band at this aspect.
            // Do not fake one behind the cards: that hides fabric and makes the block spacing lie.
            return layoutBounds.height > 1f ? layoutBounds : Rect.zero;
        }
        private Texture2D CurrentGameplayBackdrop()
        {
            if (game == null || game.Sim == null) return backdrop;
            if (VisualLevelIndex == RiverVisualLevelIndex && riverMural != null) return riverMural;
            if (game.Sim.LevelIndex == 1 && chicagoBackground != null) return chicagoBackground;
            if (game.Sim.LevelIndex == 2 && velezBackground != null) return velezBackground;
            if (game.Sim.LevelIndex == 3 && ferroBackground != null) return ferroBackground;
            if (game.Sim.LevelIndex == 4 && independienteBackground != null) return independienteBackground;
            if (game.Sim.LevelIndex == 5 && racingBackground != null) return racingBackground;
            if (game.Sim.LevelIndex == 6 && sanLorenzoBackground != null) return sanLorenzoBackground;
            if (game.Sim.LevelIndex == 8 && bocaBackground != null) return bocaBackground;
            if (game.Sim.LevelIndex == 9 && camionerosBackground != null) return camionerosBackground;
            if (game.Sim.LevelIndex == 10 && losRedondosBackground != null) return losRedondosBackground;
            return backdrop;
        }
        private void DrawBackdropLayers(float canvasHeight, float verticalScale, Rect safeViewport, Rect safePennants, bool sceneryOnly)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            // The root public ground/counter/operator floor is invariant; only scenery is themed.
            Color previous = GUI.color;
            GUI.color = theme.SceneryTint;
            if (backdrop != null)
                GUI.DrawTexture(StreetSceneLayout.BackgroundBounds(canvasHeight), backdrop, ScaleMode.StretchToFill, true);
            GUI.color = previous;
            if (game != null && game.Sim != null && game.Sim.UsesActiveCooks && backdrop != null)
            {
                // The wide root plate contains an obsolete condiment table on its right edge.
                // Reuse its clean opposite counter/floor edge, not a new or repainted asset.
                DrawRaw(StreetSceneLayout.ProjectSourceRect(StreetSceneLayout.BakedSideTableSource, canvasHeight),
                    backdrop, StreetSceneLayout.CleanCounterSideSource, true, true);
            }
            Rect muralBounds = DrawGameplayMural(canvasHeight, verticalScale);
            DrawFerroMuralSidewalk(muralBounds);
            DrawLosRedondosMuralSidewalk(muralBounds);
            if (sceneryOnly)
            {
                if (backdrop == null) return;
                // Continue the original pavement in one uninterrupted projection, without the baked footer.
                // Start below the counter and table in the original artwork; keep horizontal grout aligned.
                const float floorStart = 825f, floorEnd = 1154f;
                Rect floor = StreetSceneLayout.BackgroundBounds(canvasHeight);
                float top = StreetSceneLayout.SourceY(floorStart, canvasHeight);
                float sy = backdrop.height / StreetSceneLayout.ReferenceBackdropSize.y;
                GUI.color = theme.SceneryTint;
                DrawRaw(new Rect(floor.x, top, floor.width, canvasHeight - top), backdrop,
                    new Rect(0, floorStart * sy, backdrop.width, (floorEnd - floorStart) * sy), true, false);
                GUI.color = previous;
            }
            else DrawClubPennants(canvasHeight, verticalScale, safeViewport, safePennants);
        }

        private void DrawFerroMuralSidewalk(Rect muralBounds)
        {
            if (VisualLevelIndex != 3 || ferroBackground == null || Screen.width <= 0) return;
            Rect destination = StreetSceneLayout.FerroMuralSidewalkBounds(muralBounds, Screen.width);
            Rect source = StreetSceneLayout.FerroMuralSidewalkSource(
                new Vector2(ferroBackground.width, ferroBackground.height));
            if (destination.width <= 0f || destination.height <= 0f || source.width <= 0f || source.height <= 0f) return;

            // Reuse only the sidewalk/curb at the mural foot; stretch it to one thin strip rather
            // than layering the former full-height curb-and-road crop over the shared ground.
            DrawRaw(destination, ferroBackground, source, true, false);
        }

        private void DrawLosRedondosMuralSidewalk(Rect muralBounds)
        {
            if (VisualLevelIndex != 10 || losRedondosBackground == null || Screen.width <= 0) return;
            Rect destination = StreetSceneLayout.LosRedondosMuralSidewalkBounds(muralBounds, Screen.width);
            Rect source = StreetSceneLayout.LosRedondosMuralSidewalkSource(
                new Vector2(losRedondosBackground.width, losRedondosBackground.height));
            if (destination.width <= 0f || destination.height <= 0f || source.width <= 0f || source.height <= 0f) return;

            // Match Ferro: stretch one original tile-and-curb crop into a single thin strip
            // directly below the mural. Do not retain the broad road band or draw a second curb.
            DrawRaw(destination, losRedondosBackground, source, true, false);
        }

        private Rect DrawGameplayMural(float canvasHeight, float verticalScale)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            if (!string.IsNullOrEmpty(theme.MuralResource))
                return DrawThemedMural(theme, canvasHeight, verticalScale);
            else if (muralArt != null)
            {
                Rect band = new Rect(0f, HudBar.yMax, W, MuralBounds.height * verticalScale);
                DrawRawCover(band, muralArt, MuralSource);
                return band;
            }
            return Rect.zero;
        }

        private void DrawGameplayMuralInScreenSpace()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            float scale = Screen.width / W;
            float fullHeight = Screen.height / scale;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
                GUI.color = Color.white;
                DrawGameplayMural(fullHeight, fullHeight / H);
            }
            finally { GUI.matrix = previous; GUI.color = previousColor; }
        }

        private Rect DrawThemedMural(ClubVisualTheme theme, float canvasHeight, float verticalScale)
        {
            if (clubMuralResource != theme.MuralResource)
            {
                clubMuralResource = theme.MuralResource;
                clubMuralTexture = Resources.Load<Texture2D>(clubMuralResource);
            }
            if (clubMuralTexture == null) return Rect.zero;
            // Fit the complete mural below the fixed HUD, never hide portraits behind it.
            Rect band = new Rect(0, HudBar.yMax, W, MuralBounds.height * verticalScale);
            Rect normalized = theme.MuralSource;
            Rect source = new Rect(normalized.x * clubMuralTexture.width, normalized.y * clubMuralTexture.height,
                normalized.width * clubMuralTexture.width, normalized.height * clubMuralTexture.height);
            float naturalHeight = W * source.height / Mathf.Max(1f, source.width);
            float availableHeight = Mathf.Max(0f, StreetSceneLayout.CounterTop(canvasHeight) - band.y);
            // Align the complete, aspect-preserved crop directly below the HUD; expand only to its
            // natural aspect or the available counter band, whichever is smaller.
            band.height = Mathf.Min(Mathf.Max(band.height, naturalHeight), availableHeight);
            if (game != null && game.Sim != null && game.Sim.UsesActiveCooks)
                band = StreetSceneLayout.ActiveMuralBand(canvasHeight, verticalScale, HudBar.yMax);
            // Draw an independent neighborhood continuation behind the HUD. Prefer the full
            // club backdrop when the cropped mural has no suitable matching pixels above it.
            Texture2D continuation = VisualLevelIndex == RiverVisualLevelIndex && riverHeaderExtension != null
                ? riverHeaderExtension : CurrentGameplayBackdrop();
            if (continuation != null && continuation != clubMuralTexture)
                DrawMuralHeaderFromBackdrop(continuation, band);
            else
                DrawMuralContinuation(clubMuralTexture, source, band);
            DrawRawCover(band, clubMuralTexture, source);
            return band;
        }

        private static void DrawMuralHeaderFromBackdrop(Texture2D texture, Rect muralBand)
        {
            if (texture == null || muralBand.y <= 0f || muralBand.width <= 0f) return;
            float extensionHeight = Mathf.Min(muralBand.y, HudBar.yMax);
            float sourceHeight = texture.width * extensionHeight / muralBand.width;
            if (sourceHeight > texture.height)
            {
                sourceHeight = texture.height;
                extensionHeight = sourceHeight * muralBand.width / texture.width;
            }
            Rect source = new Rect(0f, 0f, texture.width, sourceHeight);
            Rect destination = new Rect(muralBand.x, muralBand.y - extensionHeight,
                muralBand.width, extensionHeight);
            DrawRaw(destination, texture, source, true, false);
        }

        private static void DrawMuralContinuation(Texture2D texture, Rect muralSource, Rect muralBand)
        {
            if (texture == null || muralBand.y <= 0f || muralBand.width <= 0f || muralBand.height <= 0f) return;

            // DrawRawCover crops the source to this aspect ratio. Use that same crop and scale so
            // adjacent authored pixels join the unchanged mural band without distortion.
            Rect visibleSource = CropSourceToAspect(muralSource, muralBand.width / muralBand.height);
            if (visibleSource.y <= 0f || visibleSource.height <= 0f) return;

            float extensionHeight = Mathf.Min(muralBand.y,
                visibleSource.y * muralBand.height / visibleSource.height);
            if (extensionHeight <= 0f) return;

            float sourceHeight = extensionHeight * visibleSource.height / muralBand.height;
            Rect source = new Rect(visibleSource.x, visibleSource.y - sourceHeight,
                visibleSource.width, sourceHeight);
            Rect destination = new Rect(muralBand.x, muralBand.y - extensionHeight,
                muralBand.width, extensionHeight);
            DrawRaw(destination, texture, source, true, false);
        }

        private void DrawClubPennants(float canvasHeight, float verticalScale, Rect safeViewport, Rect safePennants)
        {
            ClubVisualTheme theme = ClubVisualTheme.ForLevel(VisualLevelIndex);
            Color previousColor = GUI.color;
            try
            {
                if (game != null && game.Sim != null && game.Sim.UsesActiveCooks)
                {
                    // Continue the tiled pavement over the baked cream footer. The source art
                    // changes from tile to cream below row ~1150, so map a reusable tile band
                    // instead of sampling deeper into the backdrop (which creates a white stripe).
                    if (backdrop != null)
                    {
                        Vector2 textureSize = new Vector2(backdrop.width, backdrop.height);
                        Rect pavement = StreetSceneLayout.PavementContinuationSource(textureSize);
                        float floorTop = StreetKitchenLayout.TiledPlayfieldBottom * verticalScale;
                        float fieldBottom = game.Sim.KitchenLayout.PlayfieldBottom * verticalScale;
                        float overlap = StreetSceneLayout.PavementContinuationOverlap(Screen.width);
                        float tileHeight = StreetSceneLayout.PavementContinuationTileHeight(canvasHeight, textureSize);
                        if (pavement.width > 0f && pavement.height > 0f && tileHeight > 0f)
                        {
                            // Match the backdrop's scenery tint so the continuation does not
                            // introduce a brighter horizontal band at its edge.
                            GUI.color = theme.SceneryTint;
                            for (float tileY = floorTop; tileY < fieldBottom; tileY += tileHeight)
                            {
                                // The clean source span avoids grout joints; overdraw closes raster gaps.
                                float drawTop = Mathf.Max(0f, tileY - overlap);
                                float drawBottom = Mathf.Min(fieldBottom, tileY + tileHeight + overlap);
                                float drawnHeight = Mathf.Max(0f, drawBottom - drawTop);
                                // Keep sampling inside the clean crop even when the screen-space bleed expands the destination.
                                float sourceHeight = pavement.height * Mathf.Min(1f, drawnHeight / tileHeight);
                                Rect uv = new Rect(pavement.x / backdrop.width,
                                    1f - (pavement.y + sourceHeight) / backdrop.height,
                                    pavement.width / backdrop.width, sourceHeight / backdrop.height);
                                GUI.DrawTextureWithTexCoords(new Rect(0f, drawTop, W, drawnHeight), backdrop, uv, true);
                            }
                        }
                    }
                    DrawFooterBluePanel(new Rect(0f, game.Sim.KitchenLayout.PlayfieldBottom * verticalScale,
                        W, Mathf.Max(0f, canvasHeight - game.Sim.KitchenLayout.PlayfieldBottom * verticalScale)), verticalScale);
                }
                else
                {
                    // Replace the complete baked cream footer, including behind the transparent pennants.
                    float footerTop = game.Sim.KitchenLayout.PlayfieldBottom * verticalScale;
                    DrawFooterBluePanel(new Rect(0f, footerTop, W, Mathf.Max(0f, canvasHeight - footerTop)), verticalScale);
                }

                if (safePennants.height <= 1f || safeViewport.width <= 0f) return;
                if (clubPennantTexture == null || clubPennantThemeName != theme.ClubName ||
                    Mathf.Abs(clubPennantLogicalHeight - safePennants.height) > .25f)
                {
                    if (clubPennantTexture != null) Destroy(clubPennantTexture);
                    clubPennantTexture = CreateFooterPennants(theme, safePennants.height);
                    clubPennantThemeName = theme.ClubName;
                    clubPennantLogicalHeight = safePennants.height;
                }

                float fullScale = Screen.width / W;
                float safeScale = safeViewport.width / W;
                Rect destination = new Rect(0f,
                    (safeViewport.y + safePennants.y * safeScale) / fullScale,
                    W, safePennants.height * safeScale / fullScale);
                GUI.color = Color.white;
                // The generated texture matches this full-width destination aspect; keep its pennant geometry undistorted.
                GUI.DrawTexture(destination, clubPennantTexture, ScaleMode.ScaleToFit, true);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private static Texture2D CreateFooterPennants(ClubVisualTheme theme, float logicalHeight, bool keepReadable = false) =>
            ClubPennantRenderer.CreateAdaptiveTexture(theme, logicalHeight, keepReadable, false);

        private void DrawFooterBluePanel(Rect bounds, float verticalScale)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            float scale = Mathf.Max(.01f, verticalScale);
            float darkEdgeHeight = Mathf.Min(2f * scale, bounds.height);
            float warmEdgeHeight = Mathf.Min(scale, Mathf.Max(0f, bounds.height - darkEdgeHeight));
            Rect woodBounds = new Rect(bounds.x, bounds.y + darkEdgeHeight + warmEdgeHeight,
                bounds.width, Mathf.Max(0f, bounds.height - darkEdgeHeight - warmEdgeHeight));
            Color previous = GUI.color;
            try
            {
                if (gameplayFooterTexture != null && woodBounds.height > 0f)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(woodBounds, gameplayFooterTexture, ScaleMode.StretchToFill, true);
                }
                else
                {
                    GUI.color = new Color(.025f, .12f, .34f, 1f);
                    GUI.DrawTexture(woodBounds, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                }

                GUI.color = new Color(.012f, .035f, .13f, 1f);
                GUI.DrawTexture(new Rect(bounds.x, bounds.y, bounds.width, darkEdgeHeight),
                    Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                if (warmEdgeHeight > 0f)
                {
                    GUI.color = new Color(.12f, .72f, 1f, 1f);
                    GUI.DrawTexture(new Rect(bounds.x, bounds.y + darkEdgeHeight, bounds.width, warmEdgeHeight),
                        Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
                }
            }
            finally { GUI.color = previous; }
        }
    }
}

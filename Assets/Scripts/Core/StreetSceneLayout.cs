using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>All Boys is the spatial root. Club art never controls gameplay geometry.</summary>
    public static class StreetSceneLayout
    {
        public const float Width = 540f, Height = 960f;
        public const float WorkerServiceY = 400f;
        public const float CustomerHeight = 76f, CustomerHiddenLegHeight = 32f;
        public const float MuralSidewalkMaxScreenPixels = 25f;
        public const float FerroMuralSidewalkMaxScreenPixels = MuralSidewalkMaxScreenPixels;
        public static readonly Vector2 ReferenceBackdropSize = new Vector2(940, 1673);
        public static readonly Vector2 FerroBackdropSize = new Vector2(941, 1672);
        public static readonly Vector2 LosRedondosBackdropSize = new Vector2(941, 1672);
        public static readonly Rect CounterSource = new Rect(0, 548, 940, 108);
        public static readonly Rect MuralBounds = new Rect(0, 0, Width, 118);

        public static float SourceY(float sourceY, float canvasHeight)
        {
            float scale = Mathf.Max(Width / ReferenceBackdropSize.x, canvasHeight / ReferenceBackdropSize.y);
            return (canvasHeight - ReferenceBackdropSize.y * scale) * .5f + sourceY * scale;
        }
        public static float CounterTop(float canvasHeight) => SourceY(CounterSource.y, canvasHeight);

        // Keep the far-wall overlay out of the public street, even with a wide/tall club mural.
        public static Rect ActiveMuralBand(float canvasHeight, float verticalScale, float hudBottom)
        {
            float available = CounterTop(canvasHeight) - hudBottom - 90f * verticalScale;
            return new Rect(0f, hudBottom, Width, Mathf.Min(MuralBounds.height * verticalScale, Mathf.Max(0f, available)));
        }

        public static readonly Rect CleanCounterSideSource = new Rect(0f, 548f, 470f, 608f);
        public static readonly Rect BakedSideTableSource = new Rect(470f, 548f, 470f, 608f);
        public static Rect ProjectSourceRect(Rect source, float canvasHeight)
        {
            Rect background = BackgroundBounds(canvasHeight);
            float scale = background.width / ReferenceBackdropSize.x;
            return new Rect(background.x + source.x * scale, background.y + source.y * scale,
                source.width * scale, source.height * scale);
        }

        /// <summary>Project the single baked full-screen counter into the controls' top-origin safe viewport.</summary>
        public static float CounterTopInViewport(float screenWidth, float screenHeight, Rect viewport)
        {
            if (screenWidth <= 0f || viewport.width <= 0f) return CounterTop(Height);
            float fullScale = screenWidth / Width;
            float safeScale = viewport.width / Width;
            float fullCanvasHeight = screenHeight / fullScale;
            float physicalCounterTop = CounterTop(fullCanvasHeight) * fullScale;
            return Mathf.Clamp((physicalCounterTop - viewport.y) / safeScale, 0f,
                viewport.height / safeScale);
        }

        public static float CounterFrontBottom(float canvasHeight) => SourceY(CounterSource.yMax, canvasHeight);
        public static Rect BackgroundBounds(float canvasHeight)
        {
            float scale = Mathf.Max(Width / ReferenceBackdropSize.x, canvasHeight / ReferenceBackdropSize.y);
            Vector2 size = ReferenceBackdropSize * scale;
            return new Rect((Width - size.x) * .5f, (canvasHeight - size.y) * .5f, size.x, size.y);
        }
        /// <summary>Returns a reusable band of tiled pavement, excluding the baked cream footer.</summary>
        public static Rect PavementContinuationSource(Vector2 textureSize)
        {
            float sy = textureSize.y / ReferenceBackdropSize.y;
            // Use a grout-free interior (source rows 935–1055), clear of both nearby joints and their filter taps.
            return new Rect(0f, 935f * sy, textureSize.x, 120f * sy);
        }
        public static float PavementContinuationTileHeight(float canvasHeight, Vector2 textureSize)
        {
            // Fit the clean crop to the active-cook extension, avoiding a second repeated grout joint.
            float verticalScale = canvasHeight / Height;
            return (StreetKitchenLayout.ActivePlayfieldBottom - StreetKitchenLayout.TiledPlayfieldBottom) * verticalScale;
        }
        /// <summary>Covers the baked edge with at least four logical units of floor overlap.</summary>
        public static float PavementContinuationOverlap(float screenWidth)
        {
            return screenWidth > 0f ? Mathf.Max(4f, 4f * Width / screenWidth) : 0f;
        }
        public static Rect CounterBounds(float canvasHeight)
        {
            Rect background = BackgroundBounds(canvasHeight);
            return new Rect(background.x, CounterTop(canvasHeight), background.width,
                CounterFrontBottom(canvasHeight) - CounterTop(canvasHeight));
        }
        /// <summary>One thin mural-foot sidewalk strip, attached to the mural and capped in rendered pixels.</summary>
        private static Rect MuralSidewalkBounds(Rect muralBounds, float screenWidth)
        {
            if (muralBounds.width <= 0f || screenWidth <= 0f) return Rect.zero;
            float widthScale = screenWidth / Width;
            float logicalHeight = MuralSidewalkMaxScreenPixels / widthScale;
            return new Rect(muralBounds.x, muralBounds.yMax, muralBounds.width, logicalHeight);
        }
        /// <summary>One very thin Ferro sidewalk strip attached to the mural.</summary>
        public static Rect FerroMuralSidewalkBounds(Rect muralBounds, float screenWidth)
        {
            return MuralSidewalkBounds(muralBounds, screenWidth);
        }
        /// <summary>Only the Ferro wall-foot tile and a single curb edge; excludes the broad road crop.</summary>
        public static Rect FerroMuralSidewalkSource(Vector2 textureSize)
        {
            float scale = textureSize.y / FerroBackdropSize.y;
            return new Rect(0f, 775f * scale, textureSize.x, 75f * scale);
        }
        /// <summary>One original Los Redondos tile-and-curb slice in top-origin texture pixels.</summary>
        public static Rect LosRedondosMuralSidewalkBounds(Rect muralBounds, float screenWidth)
        {
            return MuralSidewalkBounds(muralBounds, screenWidth);
        }
        /// <summary>Original Los Redondos tiled sidewalk ending at a single curb, without the roadway.</summary>
        public static Rect LosRedondosMuralSidewalkSource(Vector2 textureSize)
        {
            float scale = textureSize.y / LosRedondosBackdropSize.y;
            return new Rect(0f, 680f * scale, textureSize.x, 130f * scale);
        }
        public static float CustomerOffset(float counterTop, float verticalScale)
        {
            float originalFrontFeet = (StreetSimulation.FrontQueueY - CustomerHeight) * verticalScale + CustomerHeight;
            return (counterTop + CustomerHiddenLegHeight - originalFrontFeet) / verticalScale;
        }
        public static Rect PlayerZone => new Rect(0, WorkerServiceY, Width, 700f - WorkerServiceY);
    }
}

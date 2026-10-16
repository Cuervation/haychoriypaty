using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>All Boys is the spatial root. Club art never controls gameplay geometry.</summary>
    public static class StreetSceneLayout
    {
        public const float Width = 540f, Height = 960f;
        public const float WorkerServiceY = 400f;
        public const float CustomerHeight = 76f, CustomerHiddenLegHeight = 32f;
        public static readonly Vector2 ReferenceBackdropSize = new Vector2(940, 1673);
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
        public static Rect CounterBounds(float canvasHeight)
        {
            Rect background = BackgroundBounds(canvasHeight);
            return new Rect(background.x, CounterTop(canvasHeight), background.width,
                CounterFrontBottom(canvasHeight) - CounterTop(canvasHeight));
        }
        public static float CustomerOffset(float counterTop, float verticalScale)
        {
            float originalFrontFeet = (StreetSimulation.FrontQueueY - CustomerHeight) * verticalScale + CustomerHeight;
            return (counterTop + CustomerHiddenLegHeight - originalFrontFeet) / verticalScale;
        }
        public static Rect PlayerZone => new Rect(0, WorkerServiceY, Width, 700f - WorkerServiceY);
    }
}

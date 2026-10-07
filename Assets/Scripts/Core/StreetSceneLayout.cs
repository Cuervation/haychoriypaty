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

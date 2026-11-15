using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        // The hub and the grill matches are drawn entirely with IMGUI, which URP renders after post-processing,
        // so the camera's Bloom / Tonemapping / Vignette stack only ever processed an empty frame (about twenty
        // extra full-screen passes per frame). Keep it for Tower Defense, whose world sprites may use it.
        private UniversalAdditionalCameraData postProcessingCameraData;
        private Camera postProcessingCamera;

        private void UpdatePostProcessingPolicy()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            if (postProcessingCamera != camera || postProcessingCameraData == null)
            {
                postProcessingCamera = camera;
                postProcessingCameraData = camera.GetUniversalAdditionalCameraData();
            }
            if (postProcessingCameraData == null) return;
            bool needed = IsTowerDefenseNavigationActive;
            if (postProcessingCameraData.renderPostProcessing != needed)
                postProcessingCameraData.renderPostProcessing = needed;
        }
    }
}

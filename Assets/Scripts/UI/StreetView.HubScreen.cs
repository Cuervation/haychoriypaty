using System.Collections;
using UnityEngine;

namespace HayChoriYPaty
{
    // Pantalla Hub: controles, ilustración y layout. Extraído de StreetView.cs sin cambios de lógica.
    public sealed partial class StreetView
    {
        private void DrawHubScreen()
        {
            Texture2D artwork = hubBackdrop != null ? hubBackdrop : coverArt;
            // Use the same illustration as one full-bleed background at every resolution.
            // ScaleAndCrop preserves its proportions and avoids both colored bars and duplicates.
            DrawIntroScreen(artwork, Color.white, ScaleMode.ScaleAndCrop);

            if (settingsActive)
            {
                DrawHubSettingsOverlay();
                return;
            }

            // This 841×1870 reference is already a complete, tightly composed illustration.
            // Do not overlay cutouts from the previous master: they covered the flag and wall sign.
            DrawHubAmbientEffects();
            DrawHubCharacterAnimations();
            DrawHubSignage();
            DrawHubHotspot(HubRankingAction);
            DrawHubHotspot(HubBadgesAction);
            DrawHubHotspot(HubMarketAction);
            DrawHubHotspot(HubPlayAction);
            DrawHubSettingsButton();
            DrawHubTopMusicButton();
        }

        private void DrawHubSettingsButton()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            float previousVerticalScale = layoutVerticalScale;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                layoutVerticalScale = 1f;
                Rect bounds = HubSettingsButtonScreenBounds(
                    new Vector2(Screen.width, Screen.height), Screen.safeArea);
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                if (HubArtworkControlIsVisible(HubSettingsArtworkSourceBounds, screenSize, Screen.safeArea))
                {
                    if (GUI.Button(bounds, string.Empty, invisible)) NativeAction(HubSettingsAction);
                }
                else DrawHubIconButton(bounds, HubSettingsAction,
                    GetHubControlIcon(HubControlIconKind.Settings), HubSettingsButtonContent);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                layoutVerticalScale = previousVerticalScale;
            }
        }

        private void DrawHubTopMusicButton()
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            float previousVerticalScale = layoutVerticalScale;
            try
            {
                GUI.matrix = Matrix4x4.identity;
                layoutVerticalScale = 1f;
                Rect bounds = HubTopMusicButtonScreenBounds(
                    new Vector2(Screen.width, Screen.height), Screen.safeArea);
                bool musicEnabled = HubMusicEnabled();
                Vector2 screenSize = new Vector2(Screen.width, Screen.height);
                if (HubArtworkControlIsVisible(HubMusicArtworkSourceBounds, screenSize, Screen.safeArea))
                {
                    if (GUI.Button(bounds, string.Empty, invisible)) NativeAction(HubTopMusicAction);
                    if (!musicEnabled)
                    {
                        Rect icon = HubLayeredArtworkScreenRect(HubMusicArtworkSourceBounds, screenSize);
                        GUI.color = Color.white;
                        GUI.DrawTexture(icon, GetHubControlIcon(HubControlIconKind.Muted), ScaleMode.ScaleToFit, true);
                    }
                }
                else DrawHubIconButton(bounds, HubTopMusicAction,
                    GetHubControlIcon(musicEnabled ? HubControlIconKind.Music : HubControlIconKind.Muted),
                    musicEnabled ? HubMusicOnButtonContent : HubMusicOffButtonContent);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                layoutVerticalScale = previousVerticalScale;
            }
        }

        private void DrawHubIconButton(Rect bounds, int action, Texture2D icon, GUIContent buttonContent)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            DrawStandardButton(bounds, action, string.Empty, true, null, null, buttonContent);
            if (icon == null) return;
            Rect drawn = StandardButtonVisualBounds(bounds, pressedAction == action);
            float iconSize = Mathf.Min(drawn.width, drawn.height) * .72f;
            Color previousColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(drawn.center.x - iconSize * .5f, drawn.center.y - iconSize * .5f,
                    iconSize, iconSize), icon, ScaleMode.ScaleToFit, true);
            }
            finally { GUI.color = previousColor; }
        }

        private void DrawHubSettingsOverlay()
        {
            DrawIntroScreen(Texture2D.whiteTexture, new Color(0f, 0f, 0f, .62f), ScaleMode.StretchToFill);
            Rect panel = HubSettingsPanelBounds;
            FillRect(panel, new Color(.018f, .075f, .145f, .98f));
            Color accent = new Color(.20f, .79f, 1f, 1f);
            FillRect(new Rect(panel.x, panel.y, panel.width, 4f), accent);
            FillRect(new Rect(panel.x, panel.yMax - 4f, panel.width, 4f), accent);
            FillRect(new Rect(panel.x, panel.y, 4f, panel.height), accent);
            FillRect(new Rect(panel.xMax - 4f, panel.y, 4f, panel.height), accent);

            Styles();
            GUIStyle heading = new GUIStyle(menuTitle)
            {
                fontSize = 31,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            heading.normal.textColor = Color.white;
            GUIStyle caption = new GUIStyle(text)
            {
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            caption.normal.textColor = new Color(.84f, .94f, 1f);
            Label(new Rect(panel.x + 18f, panel.y + 20f, panel.width - 36f, 52f), "AJUSTES", heading);
            Label(new Rect(panel.x + 18f, panel.y + 77f, panel.width - 36f, 30f), "MÚSICA DEL BARRIO", caption);
            DrawStandardButton(HubSettingsMusicButtonBounds, HubSettingsMusicAction,
                HubMusicEnabled() ? "MÚSICA: ACTIVADA" : "MÚSICA: DESACTIVADA");
            DrawStandardButton(HubSettingsBackButtonBounds, HubSettingsBackAction, "VOLVER AL BARRIO");
        }
        private void DrawHubHotspot(int action)
        {
            Rect bounds = HubArtworkActionBounds(action);
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            if (GUI.Button(LayoutRect(bounds), "", invisible)) NativeAction(action);
        }
        private static Rect HubArtActionSourceRect(int action)
        {
            switch (action)
            {
                // Four visible hotspots remapped to the single 841x1870 vertical 9:20 master.
                // Account/login remains available from JUGAR's Firebase gate, not as a separate icon.
                case HubRankingAction: return new Rect(486f, 447f, 236f, 128f);
                case HubPlayAction: return new Rect(121f, 837f, 224f, 112f);
                case HubMarketAction: return new Rect(91f, 1207f, 292f, 104f);
                case HubBadgesAction: return new Rect(594f, 837f, 242f, 142f);
                default: return Rect.zero;
            }
        }
        private Rect HubArtworkActionBounds(int action) => HubArtworkRectToLayout(
            HubSignageTouchSourceBounds(action), HubArtworkSize, new Vector2(Screen.width, Screen.height), Screen.safeArea);
        private static Rect HubSettingsButtonScreenBounds(Vector2 screenSize, Rect safeArea)
        {
            return HubArtworkControlTouchBounds(HubSettingsArtworkSourceBounds, screenSize, safeArea,
                HubSettingsButtonWidth * HubControlScale(screenSize), false);
        }
        // The top controls were authored for the 540-wide reference canvas; grow them with wider screens so
        // they stay tappable on high-resolution phones (never below the reference size).
        internal static float HubControlScale(Vector2 screenSize) => Mathf.Clamp(screenSize.x / W, 1f, 3f);
        internal static Rect HubAccountButtonScreenBounds(Vector2 screenSize)
        {
            float scale = HubControlScale(screenSize);
            return new Rect(screenSize.x - HubAccountButtonRightInset * scale, HubAccountButtonTop * scale,
                HubAccountButtonWidth * scale, HubAccountButtonHeight * scale);
        }

        private static Rect HubTopMusicButtonScreenBounds(Vector2 screenSize, Rect safeArea)
        {
            return HubArtworkControlTouchBounds(HubMusicArtworkSourceBounds, screenSize, safeArea,
                HubTopMusicButtonSize * HubControlScale(screenSize), true);
        }

        private static Rect HubArtworkControlTouchBounds(Rect source, Vector2 screenSize, Rect safeArea,
            float minSize, bool fallbackRight)
        {
            Rect viewport = CanvasViewport(screenSize, safeArea);
            if (viewport.width < minSize + 16f || viewport.height < minSize + 16f) return Rect.zero;
            Rect artwork = HubLayeredArtworkScreenRect(source, screenSize);
            bool visible = artwork.xMin >= viewport.xMin && artwork.xMax <= viewport.xMax &&
                artwork.yMin >= viewport.yMin && artwork.yMax <= viewport.yMax;
            float width = Mathf.Max(minSize, artwork.width);
            float height = Mathf.Max(minSize, artwork.height);
            Rect account = HubAccountButtonScreenBounds(screenSize);
            // Leave extra room beside Ranking on compact 9:16 previews when the artwork's
            // own lower speaker control has been cropped out.
            float controlScale = HubControlScale(screenSize);
            float fallbackMusicCenterX = Mathf.Min(account.xMin - (HubTopControlsGap + 22f) * controlScale - width * .5f,
                viewport.xMax - 8f * controlScale - width * .5f);
            float centerX = visible ? artwork.center.x : fallbackRight
                ? fallbackMusicCenterX : viewport.xMin + 8f * controlScale + width * .5f;
            float centerY = visible ? artwork.center.y : viewport.yMin + 8f * controlScale + height * .5f;
            centerX = Mathf.Clamp(centerX, viewport.xMin + width * .5f, viewport.xMax - width * .5f);
            centerY = Mathf.Clamp(centerY, viewport.yMin + height * .5f, viewport.yMax - height * .5f);
            return new Rect(centerX - width * .5f, centerY - height * .5f, width, height);
        }

        private static bool HubArtworkControlIsVisible(Rect source, Vector2 screenSize, Rect safeArea)
        {
            Rect viewport = CanvasViewport(screenSize, safeArea);
            Rect artwork = HubLayeredArtworkScreenRect(source, screenSize);
            return artwork.width > 0f && artwork.height > 0f &&
                artwork.xMin >= viewport.xMin && artwork.xMax <= viewport.xMax &&
                artwork.yMin >= viewport.yMin && artwork.yMax <= viewport.yMax;
        }

        private static Rect HubTopMusicButtonLayoutBounds(Vector2 screenSize, Rect safeArea) =>
            HubScreenRectToLayout(HubTopMusicButtonScreenBounds(screenSize, safeArea), screenSize, safeArea);

        private static Rect HubSettingsButtonLayoutBounds(Vector2 screenSize, Rect safeArea)
        {
            return HubScreenRectToLayout(HubSettingsButtonScreenBounds(screenSize, safeArea), screenSize, safeArea);
        }

        private static Rect HubScreenRectToLayout(Rect screenBounds, Vector2 screenSize, Rect safeArea)
        {
            Rect viewport = CanvasViewport(screenSize, safeArea);
            if (viewport.width <= 0f || viewport.height <= 0f || screenBounds.width <= 0f || screenBounds.height <= 0f)
                return Rect.zero;
            float scaleX = viewport.width / W, scaleY = viewport.height / H;
            return new Rect((screenBounds.x - viewport.xMin) / scaleX,
                (screenBounds.y - viewport.yMin) / scaleY,
                screenBounds.width / scaleX, screenBounds.height / scaleY);
        }

        private Texture2D GetHubControlIcon(HubControlIconKind kind)
        {
            switch (kind)
            {
                case HubControlIconKind.Settings:
                    if (hubSettingsGearIcon == null) hubSettingsGearIcon = CreateHubControlIconTexture(kind);
                    return hubSettingsGearIcon;
                case HubControlIconKind.Music:
                    if (hubMusicOnIcon == null) hubMusicOnIcon = CreateHubControlIconTexture(kind);
                    return hubMusicOnIcon;
                default:
                    if (hubMusicOffIcon == null) hubMusicOffIcon = CreateHubControlIconTexture(HubControlIconKind.Muted);
                    return hubMusicOffIcon;
            }
        }

        private static Texture2D CreateHubControlIconTexture(HubControlIconKind kind)
        {
            const int size = 64;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = HubControlIconPixel(kind, new Vector2(x + .5f, y + .5f));

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Hub " + kind + " control icon",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color HubControlIconPixel(HubControlIconKind kind, Vector2 point)
        {
            Color outline = new Color(.025f, .075f, .15f, 1f);
            Color white = new Color(1f, .99f, .94f, 1f);
            if (kind == HubControlIconKind.Settings)
            {
                Vector2 center = new Vector2(32f, 32f);
                Vector2 offset = point - center;
                float radius = offset.magnitude;
                float angle = Mathf.Atan2(offset.y, offset.x);
                float outside = 21f + (Mathf.Cos(angle * 8f) > .72f ? 5f : 0f);
                if (radius < 8.5f || radius > outside + 1f) return Color.clear;
                return radius < 10f || radius > outside - 1.8f ? outline : white;
            }

            bool insideSpeaker = HubPointInIconPolygon(point, HubSpeakerPolygon);
            float speakerEdge = HubIconPolygonEdgeDistance(point, HubSpeakerPolygon);
            if (kind == HubControlIconKind.Muted)
            {
                float muteLine = HubIconSegmentDistance(point, new Vector2(15f, 49f), new Vector2(50f, 14f));
                if (muteLine <= 1.8f) return new Color(1f, .43f, .35f, 1f);
                if (muteLine <= 4f) return outline;
            }
            if (speakerEdge <= 2f) return outline;
            if (insideSpeaker) return white;

            if (kind == HubControlIconKind.Music)
            {
                Vector2 arcCenter = new Vector2(28f, 32f);
                for (int i = 0; i < 2; i++)
                {
                    float radius = i == 0 ? 18f : 26f;
                    float arcDistance = HubIconArcDistance(point, arcCenter, radius, .82f);
                    if (arcDistance <= .75f) return white;
                    if (arcDistance <= 2f) return outline;
                }
                return Color.clear;
            }
            return Color.clear;
        }

        private static bool HubPointInIconPolygon(Vector2 point, Vector2[] vertices)
        {
            bool inside = false;
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                Vector2 a = vertices[i], b = vertices[j];
                bool crosses = (a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses) inside = !inside;
            }
            return inside;
        }

        private static float HubIconPolygonEdgeDistance(Vector2 point, Vector2[] vertices)
        {
            float distance = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
                distance = Mathf.Min(distance,
                    HubIconSegmentDistance(point, vertices[i], vertices[(i + 1) % vertices.Length]));
            return distance;
        }

        private static float HubIconSegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / Mathf.Max(.001f, segment.sqrMagnitude));
            return Vector2.Distance(point, start + segment * t);
        }

        private static float HubIconArcDistance(Vector2 point, Vector2 center, float radius, float halfAngle)
        {
            Vector2 offset = point - center;
            if (Mathf.Abs(Mathf.Atan2(offset.y, offset.x)) <= halfAngle)
                return Mathf.Abs(offset.magnitude - radius);
            Vector2 first = center + new Vector2(Mathf.Cos(halfAngle), -Mathf.Sin(halfAngle)) * radius;
            Vector2 last = center + new Vector2(Mathf.Cos(halfAngle), Mathf.Sin(halfAngle)) * radius;
            return Mathf.Min(Vector2.Distance(point, first), Vector2.Distance(point, last));
        }

        private void ReleaseHubControlIcons()
        {
            if (hubSettingsGearIcon != null) Destroy(hubSettingsGearIcon);
            if (hubMusicOnIcon != null) Destroy(hubMusicOnIcon);
            if (hubMusicOffIcon != null) Destroy(hubMusicOffIcon);
            hubSettingsGearIcon = hubMusicOnIcon = hubMusicOffIcon = null;
        }
        private static Rect HubArtworkScreenBounds(Vector2 artSize, Vector2 screenSize)
        {
            if (artSize.x <= 0f || artSize.y <= 0f || screenSize.x <= 0f || screenSize.y <= 0f) return Rect.zero;
            // Match ScaleMode.ScaleAndCrop over the physical screen. The virtual image may extend
            // beyond one edge, but the same master always covers the entire screen without bars.
            float artScale = Mathf.Max(screenSize.x / artSize.x, screenSize.y / artSize.y);
            float drawnWidth = artSize.x * artScale, drawnHeight = artSize.y * artScale;
            return new Rect((screenSize.x - drawnWidth) * .5f,
                (screenSize.y - drawnHeight) * .5f, drawnWidth, drawnHeight);
        }
        private static Rect HubArtworkRectToLayout(Rect artRect, Vector2 artSize, Vector2 screenSize, Rect safeArea)
        {
            if (artSize.x <= 0f || artSize.y <= 0f || screenSize.x <= 0f || screenSize.y <= 0f) return Rect.zero;
            Rect viewport = CanvasViewport(screenSize, safeArea);
            Rect artwork = HubArtworkScreenBounds(artSize, screenSize);
            if (viewport.width <= 0f || viewport.height <= 0f || artwork.width <= 0f || artwork.height <= 0f) return Rect.zero;
            float artScale = artwork.width / artSize.x;
            Rect screenRect = new Rect(artwork.xMin + artRect.x * artScale,
                artwork.yMin + artRect.y * artScale, artRect.width * artScale, artRect.height * artScale);
            float scaleX = viewport.width / W, scaleY = viewport.height / H;
            float xMin = Mathf.Clamp((screenRect.xMin - viewport.xMin) / scaleX, 0f, W);
            float xMax = Mathf.Clamp((screenRect.xMax - viewport.xMin) / scaleX, 0f, W);
            float yMin = Mathf.Clamp((screenRect.yMin - viewport.yMin) / scaleY, 0f, H);
            float yMax = Mathf.Clamp((screenRect.yMax - viewport.yMin) / scaleY, 0f, H);
            return xMax <= xMin || yMax <= yMin ? Rect.zero : Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}

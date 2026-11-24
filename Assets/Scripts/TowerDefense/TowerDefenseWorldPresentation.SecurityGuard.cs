using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// La Renga (2026-11-23, a pedido): four "TECH SEGURIDAD" guards patrol the top sidewalk, above the lane the fans walk, and stop
    /// from time to time to look at the street. Purely decorative: no collision, order or economy; positions are derived from the
    /// run clock, so they stand still while the match is paused. Art: street-security-guard-v1, the bald bearded supporter of the
    /// supporters' sheets dressed in the uniform (1x4 atlas of 256 px cells: walk A, walk B facing right, front A, front B).
    /// </summary>
    public sealed partial class TowerDefenseWorldPresentation
    {
        private const string SecurityGuardResource = "street-security-guard-v1";
        private const int SecurityGuardCell = 256;
        private const float SecurityGuardHeightLogical = 72f;       // cell height (a fan's cell is 76): a touch smaller, they are further back
        private const float SecurityGuardSideMarginLogical = 44f;
        private const float SecurityGuardHalfStrideLogical = 12f;   // the walk pose changes every 12 px walked
        private const float SecurityGuardIdleFrameSeconds = 1f;

        /// <summary>One guard's patrol: the stretch of the sidewalk it walks (fractions of the usable width), its depth on the strip, its pace and where it is in its round at second 0.</summary>
        private readonly struct SecurityGuardPatrol
        {
            public readonly float From, To, LaneLiftLogical, SpeedLogical, PauseSeconds, StartCycleFraction;
            public SecurityGuardPatrol(float from, float to, float lift, float speed, float pause, float start)
            { From = from; To = to; LaneLiftLogical = lift; SpeedLogical = speed; PauseSeconds = pause; StartCycleFraction = start; }
        }

        // The first one is the original guard (whole sidewalk). The others share it with different depths, paces and rounds so they rarely line up.
        private static readonly SecurityGuardPatrol[] SecurityGuardPatrols =
        {
            new SecurityGuardPatrol(0.00f, 1.00f, 40f, 30f, 3.2f, 0.11f),
            new SecurityGuardPatrol(0.04f, 0.46f, 37f, 26f, 2.6f, 0.95f),
            new SecurityGuardPatrol(0.54f, 0.96f, 43f, 28f, 3.8f, 0.95f),
            new SecurityGuardPatrol(0.22f, 0.78f, 35f, 33f, 2.2f, 0.90f),
        };

        private Sprite[] securityGuardSprites;
        private GameObject[] securityGuardObjects;
        private SpriteRenderer[] securityGuardRenderers;

        private bool HasSecurityGuard => visualLevelId == TowerDefenseLevelConfig.LaRengaCampaignLevelId;

        private void SyncSecurityGuard(TowerDefenseGameMode gameMode)
        {
            if (!HasSecurityGuard || gameMode == null || sourceCamera == null) return;
            Vector2[] centerline = gameMode.RouteGeometry != null ? gameMode.RouteGeometry.Centerline : null;
            if (centerline == null || centerline.Length == 0) return;
            if (securityGuardObjects == null && !CreateSecurityGuards()) return;

            float pixel = gameMode.WorldUnitsPerLogicalPixel;
            Rect pixels = sourceCamera.pixelRect;
            if (pixels.width <= 0f) return;
            float depth = Vector3.Dot(Vector3.zero - sourceCamera.transform.position, sourceCamera.transform.forward);
            float left = sourceCamera.ScreenToWorldPoint(new Vector3(pixels.xMin, pixels.center.y, depth)).x + SecurityGuardSideMarginLogical * pixel;
            float right = sourceCamera.ScreenToWorldPoint(new Vector3(pixels.xMax, pixels.center.y, depth)).x - SecurityGuardSideMarginLogical * pixel;
            if (right - left < pixel * 40f) return;

            for (int i = 0; i < securityGuardObjects.Length; i++)
                SyncSecurityGuard(i, gameMode, centerline[0].y, pixel, left, right);
        }

        private void SyncSecurityGuard(int index, TowerDefenseGameMode gameMode, float laneY, float pixel, float usableLeft, float usableRight)
        {
            SecurityGuardPatrol patrol = SecurityGuardPatrols[index];
            float left = Mathf.Lerp(usableLeft, usableRight, patrol.From), right = Mathf.Lerp(usableLeft, usableRight, patrol.To);
            if (right - left < pixel * 20f) return;

            // One round trip = walk right, stop, walk left, stop.
            float span = right - left, speed = patrol.SpeedLogical * pixel;
            float walk = span / speed, pause = patrol.PauseSeconds, cycle = 2f * (walk + pause);
            float t = Mathf.Repeat(gameMode.Elapsed + cycle * patrol.StartCycleFraction, cycle);
            float x; bool walking, goingRight;
            if (t < walk) { x = left + speed * t; walking = true; goingRight = true; }
            else if (t < walk + pause) { x = right; walking = false; goingRight = true; }
            else if (t < 2f * walk + pause) { x = right - speed * (t - walk - pause); walking = true; goingRight = false; }
            else { x = left; walking = false; goingRight = false; }

            int idleOffset = index; // so they do not all change their idle pose on the same beat
            int frame = walking
                ? Mathf.FloorToInt(t * patrol.SpeedLogical / SecurityGuardHalfStrideLogical) % 2
                : 2 + Mathf.FloorToInt(gameMode.Elapsed / SecurityGuardIdleFrameSeconds + idleOffset) % 2;
            SpriteRenderer renderer = securityGuardRenderers[index];
            renderer.sprite = securityGuardSprites[frame];
            renderer.flipX = walking && !goingRight;

            float feetY = laneY + patrol.LaneLiftLogical * pixel;
            float frameHeight = securityGuardSprites[frame].rect.height / securityGuardSprites[frame].pixelsPerUnit;
            Transform body = securityGuardObjects[index].transform;
            body.localScale = Vector3.one * (SecurityGuardHeightLogical * pixel / Mathf.Max(.001f, frameHeight));
            // A tiny step bounce while walking (like the supporters); the sidewalk is behind the fans, so a lower fan always draws over a guard.
            float bounce = walking ? Mathf.Abs(Mathf.Sin(t * patrol.SpeedLogical / SecurityGuardHalfStrideLogical * Mathf.PI)) * pixel * 2f : 0f;
            body.position = new Vector3(x, feetY + bounce, 0f);
            renderer.sortingOrder = 32 + Mathf.RoundToInt(-feetY * 12f);
        }

        private bool CreateSecurityGuards()
        {
            Texture2D atlas = Resources.Load<Texture2D>(SecurityGuardResource);
            if (atlas == null || presentationRoot == null) return false;
            int columns = Mathf.Max(1, atlas.width / SecurityGuardCell), rows = Mathf.Max(1, atlas.height / SecurityGuardCell);
            securityGuardSprites = new Sprite[columns * rows];
            for (int i = 0; i < securityGuardSprites.Length; i++)
            {
                int column = i % columns, row = i / columns; // row 0 is the top row of the PNG
                var rect = new Rect(column * SecurityGuardCell, atlas.height - (row + 1) * SecurityGuardCell, SecurityGuardCell, SecurityGuardCell);
                securityGuardSprites[i] = Sprite.Create(atlas, rect, new Vector2(.5f, .02f), 100f, 0, SpriteMeshType.Tight);
                securityGuardSprites[i].name = "TD security guard " + i;
                securityGuardSprites[i].hideFlags = HideFlags.DontSave;
            }
            securityGuardObjects = new GameObject[SecurityGuardPatrols.Length];
            securityGuardRenderers = new SpriteRenderer[SecurityGuardPatrols.Length];
            for (int i = 0; i < securityGuardObjects.Length; i++)
            {
                var guard = new GameObject("TD security guard " + (i + 1));
                guard.layer = presentationLayer;
                guard.transform.SetParent(presentationRoot.transform, true);
                var guardRenderer = guard.AddComponent<SpriteRenderer>();
                guardRenderer.sortingLayerName = "Default";
                guardRenderer.sprite = securityGuardSprites[Mathf.Min(2, securityGuardSprites.Length - 1)];
                securityGuardObjects[i] = guard;
                securityGuardRenderers[i] = guardRenderer;
            }
            return true;
        }

        private void DisposeSecurityGuard()
        {
            if (securityGuardSprites != null)
                for (int i = 0; i < securityGuardSprites.Length; i++) DestroyPresentationObject(securityGuardSprites[i]);
            securityGuardSprites = null;
            if (securityGuardObjects != null)
                for (int i = 0; i < securityGuardObjects.Length; i++)
                    if (securityGuardObjects[i] != null) DestroyPresentationObject(securityGuardObjects[i]);
            securityGuardObjects = null;
            securityGuardRenderers = null;
        }
    }
}

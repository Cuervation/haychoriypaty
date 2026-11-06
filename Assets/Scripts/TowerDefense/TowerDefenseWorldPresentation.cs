using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>
    /// Pooled SpriteRenderer presentation for the TD route, fences, supporters, stations and staff.
    /// A transparent camera target is composited by the existing IMGUI view so the campaign renderer stays untouched.
    /// </summary>
    public sealed class TowerDefenseWorldPresentation : MonoBehaviour
    {
        private const float FenceScreenEdgeInsetLogical = 4f;

        private sealed class FanView
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
        }

        private sealed class WorkerView
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
        }

        private readonly Dictionary<int, FanView> activeFans = new Dictionary<int, FanView>();
        private readonly Stack<FanView> pooledFans = new Stack<FanView>();
        private readonly Dictionary<int, WorkerView> workerViews = new Dictionary<int, WorkerView>();
        private readonly Dictionary<string, Sprite> fanSpriteCache = new Dictionary<string, Sprite>();
        private readonly List<Sprite> generatedWorkerSprites = new List<Sprite>();
        private readonly List<GameObject> stationObjects = new List<GameObject>();
        private readonly List<int> staleIds = new List<int>();

        private Camera sourceCamera, overlayCamera;
        private GameObject presentationRoot;
        private TowerDefenseModularFenceGenerator fenceGenerator;
        private TowerDefenseGameMode mode;
        private StreetView view;
        private RenderTexture overlayTexture;
        private int originalSourceCameraMask;
        private int presentationLayer = 5;
        private bool configured;

        public RenderTexture OverlayTexture => overlayTexture;
        public int GeneratedFencePieceCount => fenceGenerator != null ? fenceGenerator.GeneratedPieceCount : 0;
        public string FenceError => fenceGenerator != null ? fenceGenerator.LatestError : null;

        public bool Configure(StreetView streetView, TowerDefenseLevelConfig runtimeConfig, TowerDefenseGameMode gameMode,
            Camera camera, Vector2 screenSize)
        {
            if (streetView == null || runtimeConfig == null || gameMode == null || camera == null) return false;
            view = streetView;
            mode = gameMode;
            sourceCamera = camera;
            presentationLayer = LayerMask.NameToLayer("UI");
            if (presentationLayer < 0) presentationLayer = 5;
            originalSourceCameraMask = sourceCamera.cullingMask;
            sourceCamera.cullingMask = originalSourceCameraMask & ~(1 << presentationLayer);

            presentationRoot = new GameObject("Tower Defense — Camioneros scene objects");
            presentationRoot.transform.position = Vector3.zero;
            presentationRoot.layer = presentationLayer;
            fenceGenerator = presentationRoot.AddComponent<TowerDefenseModularFenceGenerator>();
            fenceGenerator.PresentationLayer = presentationLayer;
            fenceGenerator.SortingOrder = 32;
            fenceGenerator.LevelConfig = runtimeConfig;
            fenceGenerator.FencePanelSprite = Resources.Load<Sprite>("fence-panel-v1");
            fenceGenerator.SetLogicalPanelSize(mode.WorldUnitsPerLogicalPixel);
            FitFenceToCameraEdges();
            if (!fenceGenerator.Generate())
            {
                Debug.LogWarning("Tower Defense fences were not generated: " + fenceGenerator.LatestError);
                DisposePresentation();
                return false;
            }

            CreateStations(runtimeConfig);
            CreateOverlayCamera(screenSize);
            configured = true;
            Sync(gameMode);
            return true;
        }

        public void RebuildRoute(TowerDefenseLevelConfig runtimeConfig, Vector2 screenSize)
        {
            if (!configured || runtimeConfig == null || presentationRoot == null) return;
            if (fenceGenerator != null) fenceGenerator.LevelConfig = runtimeConfig;
            if (fenceGenerator != null)
            {
                fenceGenerator.SetLogicalPanelSize(mode.WorldUnitsPerLogicalPixel);
                FitFenceToCameraEdges();
                fenceGenerator.Generate();
            }
            for (int i = stationObjects.Count - 1; i >= 0; i--) DestroyPresentationObject(stationObjects[i]);
            stationObjects.Clear();
            CreateStations(runtimeConfig);
            ResizeTarget(screenSize);
            Sync(mode);
        }

        private void FitFenceToCameraEdges()
        {
            if (fenceGenerator == null || sourceCamera == null || mode == null)
                return;

            Rect cameraPixels = sourceCamera.pixelRect;
            if (cameraPixels.width <= 0f || cameraPixels.height <= 0f)
            {
                fenceGenerator.ClearVisualHorizontalBounds();
                return;
            }

            float depth = Vector3.Dot(Vector3.zero - sourceCamera.transform.position, sourceCamera.transform.forward);
            Vector3 leftEdge = sourceCamera.ScreenToWorldPoint(
                new Vector3(cameraPixels.xMin, cameraPixels.center.y, depth));
            Vector3 rightEdge = sourceCamera.ScreenToWorldPoint(
                new Vector3(cameraPixels.xMax, cameraPixels.center.y, depth));
            float inset = mode.WorldUnitsPerLogicalPixel * FenceScreenEdgeInsetLogical;
            if (!fenceGenerator.SetVisualHorizontalBounds(
                    Mathf.Min(leftEdge.x, rightEdge.x) + inset,
                    Mathf.Max(leftEdge.x, rightEdge.x) - inset))
                fenceGenerator.ClearVisualHorizontalBounds();
        }

        public void Sync(TowerDefenseGameMode gameMode)
        {
            if (!configured || gameMode == null) return;
            mode = gameMode;
            SyncFans(gameMode);
            SyncWorkers(gameMode);
        }

        public void ResizeTarget(Vector2 screenSize)
        {
            if (overlayCamera == null) return;
            int width = Mathf.Max(1, Mathf.RoundToInt(screenSize.x));
            int height = Mathf.Max(1, Mathf.RoundToInt(screenSize.y));
            if (overlayTexture != null && overlayTexture.width == width && overlayTexture.height == height) return;
            if (overlayTexture != null)
            {
                overlayCamera.targetTexture = null;
                overlayTexture.Release();
                DestroyPresentationObject(overlayTexture);
            }
            overlayTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = "Tower Defense transparent Game View layer",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            overlayTexture.Create();
            overlayCamera.targetTexture = overlayTexture;
            overlayCamera.aspect = width / (float)height;
        }

        public void DisposePresentation()
        {
            configured = false;
            if (sourceCamera != null) sourceCamera.cullingMask = originalSourceCameraMask;
            if (overlayCamera != null)
            {
                overlayCamera.targetTexture = null;
                overlayCamera.enabled = false;
            }
            for (int i = 0; i < stationObjects.Count; i++) DestroyPresentationObject(stationObjects[i]);
            stationObjects.Clear();
            foreach (var pair in activeFans) DestroyPresentationObject(pair.Value.GameObject);
            activeFans.Clear();
            while (pooledFans.Count > 0) DestroyPresentationObject(pooledFans.Pop().GameObject);
            foreach (var pair in workerViews) DestroyPresentationObject(pair.Value.GameObject);
            workerViews.Clear();
            foreach (Sprite sprite in fanSpriteCache.Values) DestroyPresentationObject(sprite);
            fanSpriteCache.Clear();
            for (int i = 0; i < generatedWorkerSprites.Count; i++) DestroyPresentationObject(generatedWorkerSprites[i]);
            generatedWorkerSprites.Clear();
            if (overlayTexture != null)
            {
                overlayTexture.Release();
                DestroyPresentationObject(overlayTexture);
                overlayTexture = null;
            }
            if (presentationRoot != null) DestroyPresentationObject(presentationRoot);
            presentationRoot = null;
            fenceGenerator = null;
            overlayCamera = null;
            sourceCamera = null;
            mode = null;
            view = null;
        }

        private void CreateOverlayCamera(Vector2 screenSize)
        {
            GameObject cameraObject = new GameObject("Tower Defense transparent route camera");
            cameraObject.layer = presentationLayer;
            cameraObject.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
            overlayCamera = cameraObject.AddComponent<Camera>();
            overlayCamera.CopyFrom(sourceCamera);
            overlayCamera.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
            overlayCamera.clearFlags = CameraClearFlags.SolidColor;
            overlayCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            overlayCamera.cullingMask = 1 << presentationLayer;
            overlayCamera.depth = sourceCamera.depth + 1f;
            overlayCamera.targetDisplay = sourceCamera.targetDisplay;
            overlayCamera.useOcclusionCulling = false;
            overlayCamera.allowHDR = false;
            overlayCamera.allowMSAA = false;
            ResizeTarget(screenSize);
        }

        private void CreateStations(TowerDefenseLevelConfig runtimeConfig)
        {
            string[] resources = StartingStationResources(runtimeConfig.AvailableProducts);
            for (int zone = 0; zone < 2; zone++)
                CreateStationRow(resources, mode.RouteGeometry == null ? Vector2.zero : mode.WorkZone(zone), zone);
        }

        private static string[] StartingStationResources(int[] products)
        {
            var resources = new List<string>(2);
            if (Contains(products, 0) || Contains(products, 1)) resources.Add("Station_NormalGrill");
            if (Contains(products, 4)) resources.Add("Station_CocaBarrel");
            return resources.ToArray();
        }

        private void CreateStationRow(string[] resourceNames, Vector2 center, int zone)
        {
            if (resourceNames == null || resourceNames.Length == 0) return;
            float worldPerPixel = mode.WorldUnitsPerLogicalPixel;
            float spacing = worldPerPixel * 140f;
            float start = -(resourceNames.Length - 1) * spacing * 0.5f;
            for (int i = 0; i < resourceNames.Length; i++)
            {
                GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + resourceNames[i]);
                if (prefab == null)
                {
                    Debug.LogWarning("Tower Defense station prefab is missing: ModularKitchen/Prefabs/" + resourceNames[i]);
                    continue;
                }
                GameObject station = Instantiate(prefab, presentationRoot.transform, false);
                station.name = "TD zone " + zone + " " + resourceNames[i];
                station.transform.position = new Vector3(center.x + start + i * spacing, center.y, 0f);
                float scale = worldPerPixel * 5.2f;
                if (resourceNames[i] == "Station_NormalGrill")
                {
                    // Reuse the campaign's chorizo-loaded grill artwork from frame
                    // zero. Its illustrated chorizos are display-only, never stock.
                    Sprite loadedGrill = Resources.Load<Sprite>("street-parrilla-large");
                    SpriteRenderer renderer = station.GetComponentInChildren<SpriteRenderer>(true);
                    if (loadedGrill != null && renderer != null)
                    {
                        renderer.sprite = loadedGrill;
                        scale = worldPerPixel * 150f / Mathf.Max(.001f, loadedGrill.bounds.size.x);
                        // Put the loaded grate in front of the worker's feet rather
                        // than beneath them, without changing any service route.
                        station.transform.position += Vector3.down * (worldPerPixel * 18f);
                    }
                }
                station.transform.localScale = Vector3.one * scale;
                SetLayerRecursively(station, presentationLayer);
                SetSortingRecursively(station, 12 + i);
                stationObjects.Add(station);
            }
        }

        private void SyncFans(TowerDefenseGameMode gameMode)
        {
            staleIds.Clear();
            foreach (int id in activeFans.Keys) staleIds.Add(id);
            for (int i = 0; i < gameMode.Fans.Count; i++)
            {
                TowerDefenseFanState fan = gameMode.Fans[i];
                FanView fanView;
                if (!activeFans.TryGetValue(fan.Id, out fanView))
                {
                    fanView = pooledFans.Count > 0 ? pooledFans.Pop() : CreateFanView(fan.Id);
                    fanView.GameObject.name = "TD supporter " + fan.Id;
                    fanView.GameObject.SetActive(true);
                    activeFans.Add(fan.Id, fanView);
                }
                staleIds.Remove(fan.Id);
                Vector3 departure = FanDepartureVisual(fan.IsDeparting ? fan.DepartureProgress : 0f);
                fanView.Renderer.color = new Color(1f, 1f, 1f, departure.z);
                Sprite sprite = FanSprite(fan.Id, fan.IsReceiving || fan.IsDeparting);
                if (sprite != null)
                {
                    fanView.Renderer.sprite = sprite;
                // Match DrawCustomerBody/DrawFan in regular levels: fit the same atlas
                // source into a 78 x 76 logical-pixel box without stretching it, then
                // center that full body between the two route rails.
                    float targetHeight = FanTargetHeightLogical(sprite.rect.width, sprite.rect.height)
                        * gameMode.WorldUnitsPerLogicalPixel;
                    float frameHeight = sprite.rect.height / sprite.pixelsPerUnit;
                    fanView.GameObject.transform.localScale = Vector3.one * (targetHeight / Mathf.Max(0.001f, frameHeight) * departure.y);
                }
                Vector2 position = fan.RouteFollower.Position;
                float bob = fan.IsDeparting ? 0f : Mathf.Sin(fan.AnimationTime * 8f + fan.Id * 1.7f) * mode.WorldUnitsPerLogicalPixel * 2.2f;
                fanView.GameObject.transform.position = new Vector3(position.x, position.y + bob + departure.x * mode.WorldUnitsPerLogicalPixel, 0f);
                fanView.Renderer.flipX = fan.RouteFollower.Facing.x < -0.15f;
                fanView.Renderer.sortingOrder = 32 + Mathf.RoundToInt(-position.y * 12f);
            }
            for (int i = 0; i < staleIds.Count; i++)
            {
                FanView stale;
                if (!activeFans.TryGetValue(staleIds[i], out stale)) continue;
                activeFans.Remove(staleIds[i]);
                stale.GameObject.SetActive(false);
                pooledFans.Push(stale);
            }
        }

        // x = upward logical pixels, y = scale, z = opacity. Existing fan art only.
        private static Vector3 FanDepartureVisual(float progress)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            return new Vector3(16f * t, 1f - .20f * t, 1f - t);
        }

        private static float FanTargetHeightLogical(float sourceWidth, float sourceHeight)
        {
            return Mathf.Min(StreetSceneLayout.CustomerHeight,
                78f * sourceHeight / Mathf.Max(0.001f, sourceWidth));
        }

        private void SyncWorkers(TowerDefenseGameMode gameMode)
        {
            staleIds.Clear();
            foreach (int id in workerViews.Keys) staleIds.Add(id);
            for (int i = 0; i < gameMode.Workers.Count; i++)
            {
                TowerDefenseWorkerState worker = gameMode.Workers[i];
                WorkerView workerView;
                if (!workerViews.TryGetValue(worker.Id, out workerView))
                {
                    workerView = CreateWorkerView(worker);
                    workerViews.Add(worker.Id, workerView);
                }
                staleIds.Remove(worker.Id);
                int frame = worker.IsMoving ? Mathf.FloorToInt(worker.TravelDistance / Mathf.Max(.01f, gameMode.WorldUnitsPerLogicalPixel * 16f)) % workerView.Frames.Length
                    : Mathf.FloorToInt(worker.AnimationTime * 2f) % workerView.Frames.Length;
                workerView.Renderer.sprite = workerView.Frames[frame];
                float bob = worker.IsMoving ? Mathf.Sin(worker.AnimationTime * 12f) * gameMode.WorldUnitsPerLogicalPixel * 1.3f
                    : Mathf.Sin(worker.AnimationTime * 2.2f + worker.Id) * gameMode.WorldUnitsPerLogicalPixel * 1.1f;
                workerView.GameObject.transform.position = new Vector3(worker.Position.x, worker.Position.y + bob, 0f);
                workerView.Renderer.flipX = worker.IsMoving && worker.TargetZone < worker.ServiceZone;
                workerView.Renderer.sortingOrder = 28 + Mathf.RoundToInt(-worker.Position.y * 12f);
            }
            for (int i = 0; i < staleIds.Count; i++)
            {
                WorkerView stale;
                if (!workerViews.TryGetValue(staleIds[i], out stale)) continue;
                DestroyPresentationObject(stale.GameObject);
                workerViews.Remove(staleIds[i]);
            }
        }

        private FanView CreateFanView(int id)
        {
            var obj = new GameObject("TD supporter " + id);
            obj.layer = presentationLayer;
            obj.transform.SetParent(presentationRoot.transform, true);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 30;
            SetLayerRecursively(obj, presentationLayer);
            return new FanView { GameObject = obj, Renderer = renderer };
        }

        private WorkerView CreateWorkerView(TowerDefenseWorkerState worker)
        {
            Sprite[] frames = view != null ? view.CreateTowerDefenseWorkerFrames(worker.Role) : null;
            if (frames == null || frames.Length == 0) return CreateFallbackWorker(worker);
            generatedWorkerSprites.AddRange(frames);
            var obj = new GameObject("TD worker " + worker.Role + " " + worker.Id);
            obj.layer = presentationLayer;
            obj.transform.SetParent(presentationRoot.transform, true);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 28;
            float targetHeight = 94f * mode.WorldUnitsPerLogicalPixel;
            obj.transform.localScale = Vector3.one * (targetHeight / Mathf.Max(.001f, frames[0].bounds.size.y));
            SetLayerRecursively(obj, presentationLayer);
            return new WorkerView { GameObject = obj, Renderer = renderer, Frames = frames };
        }

        private WorkerView CreateFallbackWorker(TowerDefenseWorkerState worker)
        {
            var obj = new GameObject("TD worker " + worker.Role + " " + worker.Id);
            obj.layer = presentationLayer;
            obj.transform.SetParent(presentationRoot.transform, true);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 28;
            Sprite fallback = Resources.Load<Sprite>("street-parrillero");
            if (fallback != null) renderer.sprite = fallback;
            Sprite[] frames = fallback == null ? Array.Empty<Sprite>() : new[] { fallback };
            if (fallback == null) frames = new[] { CreatePlaceholderSprite() };
            obj.transform.localScale = Vector3.one * (94f * mode.WorldUnitsPerLogicalPixel / Mathf.Max(.001f, frames[0].bounds.size.y));
            return new WorkerView { GameObject = obj, Renderer = renderer, Frames = frames };
        }

        private Sprite FanSprite(int customerId, bool receiving)
        {
            ClubFanDefinition fans = ClubVisualTheme.ForLevel(9).Fans;
            if (fans == null) return null;
            Texture2D texture = receiving ? fans.LoadFrontFans() : fans.LoadWalkingFans();
            if (texture == null) texture = fans.LoadFrontFans();
            if (texture == null) return null;
            Rect source = receiving ? fans.FrontSourceRect(texture.width, texture.height, customerId)
                : fans.WalkingSourceRect(texture.width, texture.height, customerId);
            string key = texture.name + ":" + source.x + ":" + source.y + ":" + source.width + ":" + source.height;
            Sprite sprite;
            if (fanSpriteCache.TryGetValue(key, out sprite)) return sprite;
            Rect bottomRect = new Rect(source.x, texture.height - source.yMax, source.width, source.height);
            sprite = Sprite.Create(texture, bottomRect, new Vector2(.5f, .02f), 100f, 0, SpriteMeshType.Tight);
            sprite.name = "TD Ferro-configured supporter " + fans.GetVariant(customerId) + (receiving ? " front" : " walk");
            sprite.hideFlags = HideFlags.DontSave;
            fanSpriteCache[key] = sprite;
            return sprite;
        }

        private Sprite CreatePlaceholderSprite()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "TD worker placeholder texture",
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels(new[] { Color.clear, Color.clear, Color.clear, Color.clear, Color.clear, Color.white, Color.white, Color.clear,
                Color.clear, Color.white, Color.white, Color.clear, Color.clear, Color.clear, Color.clear, Color.clear });
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.5f, 0f), 4f);
            sprite.hideFlags = HideFlags.DontSave;
            generatedWorkerSprites.Add(sprite);
            return sprite;
        }

        private void LateUpdate()
        {
            if (configured && mode != null) Sync(mode);
        }

        private void OnDestroy()
        {
            DisposePresentation();
        }

        private static bool Contains(int[] values, int value)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++) if (values[i] == value) return true;
            return false;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null) return;
            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }

        private static void SetSortingRecursively(GameObject target, int order)
        {
            if (target == null) return;
            foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = order;
            }
        }

        private static void DestroyPresentationObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}

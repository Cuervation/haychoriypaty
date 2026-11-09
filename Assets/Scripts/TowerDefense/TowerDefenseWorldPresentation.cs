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
        private const float HandoffPickupEnd = .28f;
        private const float HandoffThrowStart = .44f;
        private const string CamionerosReverseWalkingFansResource = "street-ferro-fans-walk-reverse-v1";
        private const string CamionerosPassingWalkingFansResource = "street-ferro-fans-walk-passing-v1";
        private const string CamionerosDownWalkingFansResource = "street-ferro-fans-walk-down-v1";
        private const string CamionerosDownReverseWalkingFansResource = "street-ferro-fans-walk-down-reverse-v1";
        private const string CamionerosDownPassingWalkingFansResource = "street-ferro-fans-walk-down-passing-v1";
        private const int CamionerosWalkingPoseCount = 3;
        private const float FanWalkHalfStrideLogicalPixels = 18f;

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
            public Sprite[] HandoffFrames;
        }

        private sealed class DeliveryCue
        {
            public GameObject GameObject;
            public SpriteRenderer Renderer;
            public TowerDefenseDelivery Receipt;
            public GameObject SourceDisplay;
            public Vector2 SourcePosition;
            public bool HasSourcePosition;
        }

        private readonly List<DeliveryCue> deliveryCues = new List<DeliveryCue>();
        private readonly Stack<DeliveryCue> pooledDeliveryCues = new Stack<DeliveryCue>();
        private readonly Dictionary<int, FanView> activeFans = new Dictionary<int, FanView>();
        private readonly Stack<FanView> pooledFans = new Stack<FanView>();
        private readonly Dictionary<int, WorkerView> workerViews = new Dictionary<int, WorkerView>();
        private readonly Dictionary<string, Sprite> fanSpriteCache = new Dictionary<string, Sprite>();
        private readonly List<Sprite> generatedWorkerSprites = new List<Sprite>();
        private readonly List<GameObject> stationObjects = new List<GameObject>();
        private readonly Dictionary<int, List<GameObject>> choriDisplaysByZone = new Dictionary<int, List<GameObject>>();
        private readonly Dictionary<int, List<GameObject>> cocaDisplaysByZone = new Dictionary<int, List<GameObject>>();
        private readonly List<int> staleIds = new List<int>();

        private Camera sourceCamera, overlayCamera;
        private GameObject presentationRoot;
        private TowerDefenseModularFenceGenerator fenceGenerator;
        private TowerDefenseGameMode mode;
        private StreetView view;
        private int visualLevelId = 9;
        private Texture2D camionerosReverseWalkingFans;
        private Texture2D camionerosPassingWalkingFans;
        private Texture2D camionerosDownWalkingFans;
        private Texture2D camionerosDownReverseWalkingFans;
        private Texture2D camionerosDownPassingWalkingFans;
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
            visualLevelId = runtimeConfig.SourceCampaignLevelId;
            camionerosReverseWalkingFans = visualLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
                ? Resources.Load<Texture2D>(CamionerosReverseWalkingFansResource) : null;
            camionerosPassingWalkingFans = visualLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
                ? Resources.Load<Texture2D>(CamionerosPassingWalkingFansResource) : null;
            camionerosDownWalkingFans = visualLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
                ? Resources.Load<Texture2D>(CamionerosDownWalkingFansResource) : null;
            camionerosDownReverseWalkingFans = visualLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
                ? Resources.Load<Texture2D>(CamionerosDownReverseWalkingFansResource) : null;
            camionerosDownPassingWalkingFans = visualLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId
                ? Resources.Load<Texture2D>(CamionerosDownPassingWalkingFansResource) : null;
            sourceCamera = camera;
            presentationLayer = LayerMask.NameToLayer("UI");
            if (presentationLayer < 0) presentationLayer = 5;
            originalSourceCameraMask = sourceCamera.cullingMask;
            sourceCamera.cullingMask = originalSourceCameraMask & ~(1 << presentationLayer);

            presentationRoot = new GameObject("Tower Defense — " + runtimeConfig.DisplayName + " scene objects");
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
            mode.DeliveryCompleted += OnDeliveryCompleted;
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
            ClearDeliveryCues();
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
            if (!fenceGenerator.SetVisualHorizontalBounds(
                    Mathf.Min(leftEdge.x, rightEdge.x),
                    Mathf.Max(leftEdge.x, rightEdge.x)))
                fenceGenerator.ClearVisualHorizontalBounds();
        }

        public void Sync(TowerDefenseGameMode gameMode)
        {
            if (!configured || gameMode == null) return;
            mode = gameMode;
            SyncFans(gameMode);
            SyncWorkers(gameMode);
            SyncDeliveryCues(gameMode.Elapsed);
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
            if (mode != null) mode.DeliveryCompleted -= OnDeliveryCompleted;
            ClearDeliveryCues();
            while (pooledDeliveryCues.Count > 0) DestroyPresentationObject(pooledDeliveryCues.Pop().GameObject);
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
            choriDisplaysByZone.Clear();
            cocaDisplaysByZone.Clear();
            string[] resources = StartingStationResources(runtimeConfig.AvailableProducts);
            for (int zone = 0; zone < 2; zone++)
                CreateStationRow(resources, mode.RouteGeometry == null ? Vector2.zero : mode.WorkZone(zone), zone);
        }

        private static string[] StartingStationResources(int[] products)
        {
            var resources = new List<string>(2);
            if (Contains(products, 0)) resources.Add("Station_NormalTable");
            if (Contains(products, 4)) resources.Add("Station_CocaBarrel");
            return resources.ToArray();
        }

        private void CreateStationRow(string[] resourceNames, Vector2 center, int zone)
        {
            if (resourceNames == null) return;
            float pixel = mode.WorldUnitsPerLogicalPixel;
            for (int i = 0; i < resourceNames.Length; i++)
            {
                string resource = resourceNames[i];
                GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + resource);
                if (prefab == null)
                {
                    Debug.LogWarning("Tower Defense station prefab is missing: " + resource);
                    continue;
                }
                bool table = resource == "Station_NormalTable";
                StreetWorkerRole role = table ? StreetWorkerRole.Parrillero : StreetWorkerRole.Cocacolero;
                GameObject station = Instantiate(prefab, presentationRoot.transform, false);
                station.name = "TD zone " + zone + " " + resource;
                Vector2 anchor = mode.StationPosition(role, zone);
                station.transform.position = new Vector3(anchor.x, anchor.y - 18f * pixel, 0f);
                int depth = 32 + Mathf.RoundToInt(-(anchor.y - 54f * pixel) * 12f);
                if (table)
                {
                    // Same three original parts, fit and proportions as StreetKitchenRenderer.
                    Rect bounds = new Rect(-StreetKitchenLayout.TableWidth * .5f, -40f,
                        StreetKitchenLayout.TableWidth, StreetKitchenLayout.SandwichTableHeight);
                    Rect visual = StreetKitchenLayout.FoodTableVisualBounds(bounds);
                    float topDepth = visual.height * StreetKitchenLayout.SandwichTableTopDepthRatio;
                    float supportHeight = visual.height - topDepth + StreetKitchenLayout.SandwichTableSupportOverlap;
                    FitTablePart(station, "Tabletop", visual.center.x, visual.yMin + topDepth * .5f,
                        bounds.width, topDepth, pixel, depth + 2);
                    float supportX = bounds.width * (.5f - StreetKitchenLayout.SandwichTableSupportCenterFraction);
                    float supportWidth = bounds.width * StreetKitchenLayout.SandwichTableSupportWidthFraction;
                    FitTablePart(station, "Trestle_Left", -supportX, visual.yMax - supportHeight * .5f,
                        supportWidth, supportHeight, pixel, depth);
                    FitTablePart(station, "Trestle_Right", supportX, visual.yMax - supportHeight * .5f,
                        supportWidth, supportHeight, pixel, depth);
                    // Cosmetic only: match the fully stocked Level 1 table using its shared
                    // slot layout and the same prepared-serving capacity. This display never
                    // adds units to TD Kitchen or affects deliveries, orders, or coins.
                    var normalLevelLayout = new StreetKitchenLayout(new[] { 0 }, false);
                    Rect normalTableBounds = normalLevelLayout.NormalTableBounds;
                    int displayCapacity = mode.Kitchen != null ? mode.Kitchen.TableCapacity(0) : 0;
                    Vector2 footprint = StreetKitchenLayout.TableServingFootprint(0);
                    for (int slot = 0; slot < displayCapacity; slot++)
                    {
                        Vector2 point = normalLevelLayout.TableSlotPosition(0, slot, false, displayCapacity);
                        Vector2 tableLocalOffset = point - normalTableBounds.center;
                        GameObject display = CreateProductDisplay(station.transform, "Sandwich_Chori",
                            "Prepared Chori display " + slot.ToString("00"),
                            (Vector2)station.transform.position + new Vector2(tableLocalOffset.x, -tableLocalOffset.y) * pixel,
                            footprint, depth + 3 + StreetKitchenLayout.TableSlotDepthOrder(slot, false));
                        if (display != null)
                        {
                            display.transform.localRotation = Quaternion.Euler(0f, 0f,
                                StreetKitchenLayout.TableSlotRotation(0, slot, false));
                            List<GameObject> zoneDisplays;
                            if (!choriDisplaysByZone.TryGetValue(zone, out zoneDisplays))
                            {
                                zoneDisplays = new List<GameObject>(displayCapacity);
                                choriDisplaysByZone.Add(zone, zoneDisplays);
                            }
                            zoneDisplays.Add(display);
                        }
                    }
                }
                else
                {
                    SpriteRenderer barrel = station.GetComponent<SpriteRenderer>();
                    if (barrel != null && barrel.sprite != null)
                    {
                        // Match StreetKitchenRenderer's exact Coca barrel bounds. Its regular
                        // station fit fills the logical width and height independently rather
                        // than preserving the sprite's aspect ratio inside a smaller box.
                        Vector2 sourceSize = barrel.sprite.bounds.size;
                        Vector3 parentScale = station.transform.parent != null
                            ? station.transform.parent.lossyScale : Vector3.one;
                        station.transform.localScale = new Vector3(
                            StreetKitchenLayout.BarrelWidth * pixel / (sourceSize.x * Mathf.Max(.0001f, Mathf.Abs(parentScale.x))),
                            StreetKitchenLayout.BarrelHeight * pixel / (sourceSize.y * Mathf.Max(.0001f, Mathf.Abs(parentScale.y))),
                            1f);
                    }
                    SetSortingRecursively(station, depth);
                    Transform ice = station.transform.Find("Ice");
                    if (ice != null) ice.GetComponent<SpriteRenderer>().sortingOrder = depth + 1;
                    for (int row = 0; row < 2; row++)
                        for (int col = 0; col < 3; col++)
                        {
                            GameObject display = CreateProductDisplay(station.transform, "Drink_Coca",
                                "Loaded Coca display " + (row * 3 + col).ToString("00"),
                                (Vector2)station.transform.position + new Vector2((col - 1) * 13f, 27f - row * 7f) * pixel,
                                StreetKitchenLayout.TableServingSize(4), depth + 2 + row);
                            if (display != null)
                            {
                                List<GameObject> zoneDisplays;
                                if (!cocaDisplaysByZone.TryGetValue(zone, out zoneDisplays))
                                {
                                    zoneDisplays = new List<GameObject>(6);
                                    cocaDisplaysByZone.Add(zone, zoneDisplays);
                                }
                                zoneDisplays.Add(display);
                            }
                        }
                }
                SetLayerRecursively(station, presentationLayer);
                stationObjects.Add(station);
            }
        }

        private static void FitTablePart(GameObject station, string name, float x, float y,
            float width, float height, float pixel, int depth)
        {
            Transform part = station.transform.Find(name);
            if (part == null) return;
            SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;
            Vector2 size = renderer.sprite.bounds.size;
            part.localPosition = new Vector3(x * pixel, -y * pixel, 0f);
            part.localScale = new Vector3(width * pixel / size.x, height * pixel / size.y, 1f);
            renderer.sortingOrder = depth;
        }

        private GameObject CreateProductDisplay(Transform parent, string prefabName, string name,
            Vector2 position, Vector2 logicalSize, int depth)
        {
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefabName);
            if (prefab == null) return null;
            GameObject item = Instantiate(prefab, parent, false);
            item.name = name;
            item.transform.position = new Vector3(position.x, position.y, 0f);
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
            {
                Vector2 size = renderer.sprite.bounds.size;
                float fit = Mathf.Min(logicalSize.x / size.x, logicalSize.y / size.y) * mode.WorldUnitsPerLogicalPixel;
                item.transform.localScale = Vector3.one * (fit / Mathf.Max(.0001f, parent.lossyScale.x));
                renderer.sortingOrder = depth;
            }
            SetLayerRecursively(item, presentationLayer);
            return item;
        }

        private void OnDeliveryCompleted(TowerDefenseDelivery receipt)
        {
            if (!configured || (receipt.Product != 0 && receipt.Product != 4)) return;
            DeliveryCue cue = pooledDeliveryCues.Count > 0 ? pooledDeliveryCues.Pop() : new DeliveryCue();
            if (cue.GameObject == null)
            {
                cue.GameObject = new GameObject("TD physical handoff cue");
                cue.GameObject.transform.SetParent(presentationRoot.transform, false);
                cue.GameObject.layer = presentationLayer;
                cue.Renderer = cue.GameObject.AddComponent<SpriteRenderer>();
                cue.Renderer.sortingOrder = 1000;
            }
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" +
                (receipt.Product == 0 ? "Sandwich_Chori" : "Drink_Coca"));
            cue.Renderer.sprite = prefab != null ? prefab.GetComponent<SpriteRenderer>().sprite : null;
            cue.Receipt = receipt;
            RestoreSourceDisplay(cue);
            cue.HasSourcePosition = false;
            cue.SourceDisplay = HideNextSourceDisplay(receipt.Product, receipt.Zone);
            if (cue.SourceDisplay != null)
            {
                cue.SourcePosition = cue.SourceDisplay.transform.position;
                cue.HasSourcePosition = true;
            }
            cue.Renderer.enabled = true;
            cue.Renderer.color = Color.white;
            cue.GameObject.transform.localRotation = Quaternion.identity;
            cue.GameObject.name = "TD delivered product " + receipt.Product + " unit " + receipt.UnitId + " to fan " + receipt.FanId;
            cue.GameObject.SetActive(true);
            deliveryCues.Add(cue);
        }

        private GameObject HideNextSourceDisplay(int product, int zone)
        {
            List<GameObject> displays;
            Dictionary<int, List<GameObject>> sourceDisplays = product == 0 ? choriDisplaysByZone : cocaDisplaysByZone;
            if (!sourceDisplays.TryGetValue(zone, out displays)) return null;
            for (int i = 0; i < displays.Count; i++)
            {
                GameObject display = displays[i];
                if (display == null || !display.activeSelf) continue;
                display.SetActive(false);
                return display;
            }
            return null;
        }

        private static void RestoreSourceDisplay(DeliveryCue cue)
        {
            if (cue == null) return;
            if (cue.SourceDisplay != null) cue.SourceDisplay.SetActive(true);
            cue.SourceDisplay = null;
            cue.HasSourcePosition = false;
        }

        private void SyncDeliveryCues(float elapsed)
        {
            for (int i = deliveryCues.Count - 1; i >= 0; i--)
            {
                DeliveryCue cue = deliveryCues[i];
                float t = Mathf.Clamp01((elapsed - cue.Receipt.Elapsed) / TowerDefenseGameMode.DeliveryCueDurationSeconds);
                if (t >= 1f)
                {
                    cue.GameObject.SetActive(false);
                    RestoreSourceDisplay(cue);
                    deliveryCues.RemoveAt(i); pooledDeliveryCues.Push(cue);
                    continue;
                }
                float pixel = mode.WorldUnitsPerLogicalPixel;
                Vector2 from = cue.HasSourcePosition ? cue.SourcePosition : cue.Receipt.Source + Vector2.up * 14f * pixel;
                Vector2 to = cue.Receipt.Target + Vector2.up * 28f * pixel;
                Vector2 hand = WorkerHandPosition(cue, pixel);
                Vector2 throwOrigin = hand;
                WorkerView worker;
                if (workerViews.TryGetValue(cue.Receipt.WorkerId, out worker) && worker.GameObject != null)
                {
                    float facing = cue.Receipt.Target.x < worker.GameObject.transform.position.x ? -1f : 1f;
                    throwOrigin += new Vector2(-facing * 4f, 3f) * pixel;
                }

                Vector2 point;
                float throwT = 0f;
                if (t < HandoffPickupEnd)
                {
                    point = Vector2.Lerp(from, hand, Mathf.SmoothStep(0f, 1f, t / HandoffPickupEnd));
                }
                else if (t < HandoffThrowStart)
                {
                    float windup = Mathf.SmoothStep(0f, 1f,
                        (t - HandoffPickupEnd) / Mathf.Max(.001f, HandoffThrowStart - HandoffPickupEnd));
                    point = Vector2.Lerp(hand, throwOrigin, windup);
                }
                else
                {
                    throwT = Mathf.Clamp01((t - HandoffThrowStart) / Mathf.Max(.001f, 1f - HandoffThrowStart));
                    float flight = Mathf.SmoothStep(0f, 1f, throwT);
                    float arc = Mathf.Sin(throwT * Mathf.PI) * (cue.Receipt.Product == 0 ? 46f : 40f) * pixel;
                    point = Vector2.Lerp(throwOrigin, to, flight) + Vector2.up * arc;
                }
                cue.GameObject.transform.position = new Vector3(point.x, point.y, 0f);
                if (cue.Renderer.sprite == null) continue;
                Vector2 size = StreetKitchenLayout.TableServingSize(cue.Receipt.Product) * 1.3f;
                float fit = Mathf.Min(size.x / cue.Renderer.sprite.bounds.size.x, size.y / cue.Renderer.sprite.bounds.size.y) * pixel;
                float stretch = Mathf.Sin(throwT * Mathf.PI);
                cue.GameObject.transform.localScale = new Vector3(fit * (1f + stretch * .20f), fit * (1f - stretch * .10f), 1f);
                cue.GameObject.transform.localRotation = Quaternion.Euler(0f, 0f, throwT * 360f);
                bool cocaStillInHandPose = cue.Receipt.Product == 4 && t < HandoffThrowStart;
                cue.Renderer.enabled = !cocaStillInHandPose;
                float throwFade = throwT <= .88f ? 1f : 1f - Mathf.SmoothStep(.88f, 1f, throwT);
                cue.Renderer.color = new Color(1f, 1f, 1f, throwFade);
            }
        }

        private Vector2 WorkerHandPosition(DeliveryCue cue, float pixel)
        {
            Vector2 workerPosition = cue.Receipt.Source;
            WorkerView worker;
            if (workerViews.TryGetValue(cue.Receipt.WorkerId, out worker) && worker.GameObject != null)
                workerPosition = worker.GameObject.transform.position;
            float facing = cue.Receipt.Target.x < workerPosition.x ? -1f : 1f;
            // Coca's approved reach pose extends the bottle beyond the worker's elbow;
            // the Parrillero's tongs stay closer to the apron.
            float handX = cue.Receipt.Product == 4 ? 34f : 16f;
            float handY = cue.Receipt.Product == 4 ? 48f : 49f;
            return workerPosition + new Vector2(facing * handX, handY) * pixel;
        }

        private void ClearDeliveryCues()
        {
            for (int i = 0; i < deliveryCues.Count; i++)
            {
                deliveryCues[i].GameObject.SetActive(false);
                RestoreSourceDisplay(deliveryCues[i]);
                pooledDeliveryCues.Push(deliveryCues[i]);
            }
            deliveryCues.Clear();
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
                bool receivingOrDeparting = fan.IsReceiving || fan.IsDeparting;
                int walkingPose = FanWalkingFrameIndex(visualLevelId, fan.Id, fan.RouteFollower.Progress,
                    gameMode.WorldUnitsPerLogicalPixel);
                bool walkingDownwards = IsWalkingDownwards(fan.RouteFollower.Facing);
                Sprite sprite = FanSpriteForFacing(fan.Id, receivingOrDeparting, walkingPose, walkingDownwards);
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
                TowerDefenseResponsiveLayout.FanFrameWidthLogical * sourceHeight / Mathf.Max(0.001f, sourceWidth));
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
                // Station staff breathe/serve, never cycle cooking poses at an invisible grill.
                if (!worker.IsMoving) frame = 0;
                float actionProgress = Mathf.Clamp01(1f - worker.ServeAnimationSeconds /
                    TowerDefenseGameMode.DeliveryCueDurationSeconds);
                workerView.Renderer.sprite = workerView.Frames[frame];
                if (worker.ServeAnimationSeconds > 0f && workerView.HandoffFrames != null && workerView.HandoffFrames.Length > 0)
                {
                    if (worker.Role == StreetWorkerRole.Cocacolero)
                    {
                        // This authored pose already contains the Coca; switch back to an empty
                        // hand at release so the physical cue becomes the one visible bottle.
                        workerView.Renderer.sprite = actionProgress < HandoffThrowStart
                            ? workerView.HandoffFrames[0] : workerView.Frames[Mathf.Min(2, workerView.Frames.Length - 1)];
                    }
                    else
                    {
                        int actionFrame = Mathf.Min(workerView.HandoffFrames.Length - 1,
                            Mathf.FloorToInt(actionProgress * workerView.HandoffFrames.Length));
                        workerView.Renderer.sprite = workerView.HandoffFrames[actionFrame];
                    }
                }
                float serve = Mathf.Sin(actionProgress * Mathf.PI);
                float facing = worker.LastServiceTarget.x < worker.Position.x ? -1f : 1f;
                float windup = actionProgress > HandoffPickupEnd && actionProgress < HandoffThrowStart
                    ? Mathf.Sin(Mathf.InverseLerp(HandoffPickupEnd, HandoffThrowStart, actionProgress) * Mathf.PI) : 0f;
                float followThrough = actionProgress >= HandoffThrowStart
                    ? Mathf.Sin(Mathf.InverseLerp(HandoffThrowStart, 1f, actionProgress) * Mathf.PI) : 0f;
                float gesture = -facing * windup * 8f + facing * followThrough * 11f;
                workerView.GameObject.transform.localRotation = Quaternion.Euler(0f, 0f, gesture - facing * serve * 2f);
                Sprite activeSprite = workerView.Renderer.sprite;
                if (activeSprite != null)
                {
                    float targetHeight = 94f * gameMode.WorldUnitsPerLogicalPixel;
                    workerView.GameObject.transform.localScale = Vector3.one *
                        (targetHeight / Mathf.Max(.001f, activeSprite.bounds.size.y));
                }
                float bob = worker.IsMoving ? Mathf.Sin(worker.AnimationTime * 12f) * gameMode.WorldUnitsPerLogicalPixel * 1.3f
                    : Mathf.Sin(worker.AnimationTime * 2.2f + worker.Id) * gameMode.WorldUnitsPerLogicalPixel * 1.1f;
                workerView.GameObject.transform.position = new Vector3(worker.Position.x, worker.Position.y + bob + serve * gameMode.WorldUnitsPerLogicalPixel * 2f, 0f);
                workerView.Renderer.flipX = worker.IsMoving ? worker.TargetZone < worker.ServiceZone
                    : worker.ServeAnimationSeconds > 0f && worker.LastServiceTarget.x < worker.Position.x;
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
            Sprite[] handoffFrames = view != null ? view.CreateTowerDefenseHandoffFrames(worker.Role) : null;
            if (handoffFrames != null) generatedWorkerSprites.AddRange(handoffFrames);
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
            return new WorkerView { GameObject = obj, Renderer = renderer, Frames = frames, HandoffFrames = handoffFrames };
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

        // Keep the original reflection-visible signature used by existing PlayMode fixtures.
        private Sprite FanSprite(int customerId, bool receiving, int walkingPose)
        {
            return FanSpriteForFacing(customerId, receiving, walkingPose, false);
        }

        private Sprite FanSpriteForFacing(int customerId, bool receiving, int walkingPose, bool walkingDownwards)
        {
            ClubFanDefinition fans = ClubVisualTheme.ForLevel(visualLevelId).Fans;
            if (fans == null) return null;
            Texture2D texture = receiving ? fans.LoadFrontFans() : fans.LoadWalkingFans();
            if (!receiving && visualLevelId == TowerDefenseLevelConfig.CamionerosCampaignLevelId)
            {
                int pose = walkingPose % CamionerosWalkingPoseCount;
                if (walkingDownwards)
                {
                    switch (pose)
                    {
                        case 0:
                            if (camionerosDownWalkingFans != null) texture = camionerosDownWalkingFans;
                            break;
                        case 1:
                            if (camionerosDownReverseWalkingFans != null) texture = camionerosDownReverseWalkingFans;
                            break;
                        case 2:
                            if (camionerosDownPassingWalkingFans != null) texture = camionerosDownPassingWalkingFans;
                            break;
                    }
                }
                else
                {
                    switch (pose)
                    {
                        case 1:
                            if (camionerosReverseWalkingFans != null) texture = camionerosReverseWalkingFans;
                            break;
                        case 2:
                            if (camionerosPassingWalkingFans != null) texture = camionerosPassingWalkingFans;
                            break;
                    }
                }
            }
            if (texture == null) texture = fans.LoadFrontFans();
            if (texture == null) return null;
            Rect source = receiving ? fans.FrontSourceRect(texture.width, texture.height, customerId)
                : fans.WalkingSourceRect(texture.width, texture.height, customerId);
            string key = texture.name + ":" + source.x + ":" + source.y + ":" + source.width + ":" + source.height;
            Sprite sprite;
            if (fanSpriteCache.TryGetValue(key, out sprite)) return sprite;
            Rect bottomRect = new Rect(source.x, texture.height - source.yMax, source.width, source.height);
            sprite = Sprite.Create(texture, bottomRect, new Vector2(.5f, .02f), 100f, 0, SpriteMeshType.Tight);
            sprite.name = "TD Ferro-configured supporter " + fans.GetVariant(customerId) +
                (receiving ? " front" : " walk " + (walkingDownwards ? "down " : string.Empty) + (walkingPose % CamionerosWalkingPoseCount));
            sprite.hideFlags = HideFlags.DontSave;
            fanSpriteCache[key] = sprite;
            return sprite;
        }

        private static bool IsWalkingDownwards(Vector2 facing)
        {
            return facing.y < -0.5f && Mathf.Abs(facing.y) > Mathf.Abs(facing.x);
        }

        private static int FanWalkingFrameIndex(int levelId, int fanId, float routeProgress, float worldUnitsPerLogicalPixel)
        {
            if (levelId != TowerDefenseLevelConfig.CamionerosCampaignLevelId ||
                float.IsNaN(routeProgress) || float.IsInfinity(routeProgress) || routeProgress < 0f ||
                float.IsNaN(worldUnitsPerLogicalPixel) || float.IsInfinity(worldUnitsPerLogicalPixel) ||
                worldUnitsPerLogicalPixel <= 0f)
                return 0;

            float halfStride = FanWalkHalfStrideLogicalPixels * worldUnitsPerLogicalPixel;
            int stridePhase = Mathf.FloorToInt(routeProgress / halfStride);
            return (stridePhase + (fanId % CamionerosWalkingPoseCount)) % CamionerosWalkingPoseCount;
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

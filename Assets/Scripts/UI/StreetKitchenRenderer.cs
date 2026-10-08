using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Projects real kitchen props and StreetSimulation food entities through isolated transparent
    /// orthographic cameras. The existing IMGUI scene, crowd, workers and HUD remain untouched.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetKitchenRenderer : MonoBehaviour
    {
        private const int PropLayer = 30, CarryLayer = 31;
        private const int RenderScale = 2;
        private const string PrefabRoot = "ModularKitchen/Prefabs/";
        private const string SpriteRoot = "ModularKitchen/Sprites/";
        private readonly Dictionary<int, GameObject> foodObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<int, SpriteRenderer> foodRenderers = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, GameObject> cookingEffects = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> heatEffects = new Dictionary<int, GameObject>();
        private readonly List<int> staleIds = new List<int>();
        private GameObject propsRoot, carryRoot;
        private Camera propsCamera, carryCamera;
        private RenderTexture propsTexture, carryTexture;
        private StreetSimulation simulation;
        private float verticalScale = 1f, canvasHeight = StreetSceneLayout.Height;
        private int renderedFrame = -1;
        private bool loggedMissingResources;

        public bool IsReady { get; private set; }
        public Texture BackgroundTexture => propsTexture;
        public Texture ForegroundTexture => null;
        public Texture CarriedTexture => carryTexture;

        /// <summary>Called by StreetView. Native SpriteRenderer objects always reflect live sim IDs and states.</summary>
        public void Prepare(StreetSimulation sim, float layoutVerticalScale, float logicalHeight)
        {
            simulation = sim;
            verticalScale = Mathf.Max(.01f, layoutVerticalScale);
            canvasHeight = Mathf.Max(1f, logicalHeight);
            if (simulation == null || !EnsureCreated()) return;
            SyncStations();
            SyncFood();
            ConfigureCamera(propsCamera, propsTexture);
            ConfigureCamera(carryCamera, carryTexture);
            IsReady = true;
        }

        private void LateUpdate()
        {
            if (!IsReady || simulation == null) return;
            // Transforms and sprites are updated here, before the native cameras render for this frame.
            SyncFood();
            PositionCamera(propsCamera);
            PositionCamera(carryCamera);
            if (renderedFrame != Time.frameCount)
            {
                renderedFrame = Time.frameCount;
                // Cameras are kept enabled and render to their dedicated transparent targets; no manual
                // Camera.Render call is used, so this remains compatible with the project's URP pipeline.
            }
        }

        private bool EnsureCreated()
        {
            if (propsRoot != null) return true;
            string[] prefabNames = {
                "Station_NormalGrill", "Station_PremiumGrill", "Station_NormalTable", "Station_PremiumTable",
                "Station_FernetTable", "Station_BeerBarrel", "Station_CocaBarrel",
                "Food_Chori", "Food_Paty", "Food_Bondiola", "Food_Vacio",
                "Sandwich_Chori", "Sandwich_Paty", "Sandwich_Bondiola", "Sandwich_Vacio",
                "Drink_Coca", "Drink_Beer", "Drink_Fernet", "Effect_Smoke", "Effect_Heat", "Prop_Ice"
            };
            string[] singleArt = {
                "empty-grill", "empty-table", "blue-barrel", "sandwich-chori", "sandwich-paty",
                "sandwich-bondiola", "sandwich-vacio", "coca", "beer", "fernet", "ice", "smoke", "heat"
            };
            var missing = new List<string>();
            for (int i = 0; i < prefabNames.Length; i++)
            {
                GameObject prefab = Prefab(prefabNames[i]);
                if (prefab == null) { missing.Add(PrefabRoot + prefabNames[i]); continue; }
                SpriteRenderer renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer == null || renderer.sprite == null) missing.Add(PrefabRoot + prefabNames[i] + " (SpriteRenderer/sprite)");
                else if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null || !renderer.sharedMaterial.shader.isSupported) missing.Add(PrefabRoot + prefabNames[i] + " (URP-compatible sprite material/shader)");
            }
            for (int i = 0; i < singleArt.Length; i++) if (Sprites(singleArt[i]).Length == 0) missing.Add(SpriteRoot + singleArt[i]);
            string[] meatArt = { "meat-chori", "meat-paty", "meat-bondiola", "meat-vacio" };
            for (int i = 0; i < meatArt.Length; i++) if (Sprites(meatArt[i]).Length != 4) missing.Add(SpriteRoot + meatArt[i] + " (four state sprites)");
            if (missing.Count > 0)
            {
                if (!loggedMissingResources)
                {
                    Debug.LogError("Modular kitchen visuals are not ready. Missing/invalid resources: " + string.Join(", ", missing.ToArray()) + ". Build them with Hay Chori y Paty/Modular Kitchen/Build Reusable Prefabs.", this);
                    loggedMissingResources = true;
                }
                return false;
            }
            propsRoot = CreateRoot("Street Kitchen Props");
            carryRoot = CreateRoot("Street Kitchen Carried Items");
            propsCamera = CreateCamera("Street Kitchen Props Camera", PropLayer, out propsTexture);
            carryCamera = CreateCamera("Street Kitchen Carried Camera", CarryLayer, out carryTexture);
            return true;
        }

        private GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.hideFlags = HideFlags.DontSave;
            root.transform.SetParent(transform, false);
            return root;
        }

        private Camera CreateCamera(string name, int layer, out RenderTexture target)
        {
            GameObject cameraObject = new GameObject(name);
            cameraObject.hideFlags = HideFlags.DontSave;
            cameraObject.transform.SetParent(transform, false);
            cameraObject.layer = layer;
            Camera cam = cameraObject.AddComponent<Camera>();
            cam.enabled = true;
            cam.orthographic = true;
            cam.orthographicSize = canvasHeight * .005f;
            cam.aspect = StreetSceneLayout.Width / canvasHeight;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << layer;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.depth = -100;
            cam.nearClipPlane = .1f;
            cam.farClipPlane = 100f;
            target = CreateTarget("StreetKitchen_" + layer);
            cam.targetTexture = target;
            PositionCamera(cam);
            return cam;
        }

        private RenderTexture CreateTarget(string name)
        {
            int width = Mathf.CeilToInt(StreetSceneLayout.Width * RenderScale);
            int height = Mathf.CeilToInt(canvasHeight * RenderScale);
            var target = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            target.name = name;
            target.antiAliasing = 1;
            target.useMipMap = false;
            target.autoGenerateMips = false;
            target.filterMode = FilterMode.Bilinear;
            target.wrapMode = TextureWrapMode.Clamp;
            target.Create();
            return target;
        }

        private void ConfigureCamera(Camera cam, RenderTexture target)
        {
            if (cam == null) return;
            int width = Mathf.CeilToInt(StreetSceneLayout.Width * RenderScale);
            int height = Mathf.CeilToInt(canvasHeight * RenderScale);
            if (target == null || target.width != width || target.height != height)
            {
                if (target != null) { target.Release(); Destroy(target); }
                target = CreateTarget(cam.name);
                cam.targetTexture = target;
                if (cam == propsCamera) propsTexture = target; else carryTexture = target;
            }
            cam.orthographicSize = canvasHeight * .005f;
            cam.aspect = StreetSceneLayout.Width / canvasHeight;
            PositionCamera(cam);
        }

        private void PositionCamera(Camera cam)
        {
            if (cam == null) return;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
        }

        private void SyncStations()
        {
            // Show all empty furniture shells in a stable layout; simulation availability still
            // gates every product, item, worker, and stock flow independently.
            EnsureStation("Station_NormalGrill", StreetKitchenLayout.NormalGrillBounds, true);
            EnsureStation("Station_PremiumGrill", StreetKitchenLayout.PremiumGrillBounds, true);
            EnsureStation("Station_NormalTable", StreetKitchenLayout.NormalTableBounds, true);
            EnsureStation("Station_PremiumTable", StreetKitchenLayout.PremiumTableBounds, true);
            EnsureStation("Station_FernetTable", StreetKitchenLayout.FernetTableBounds, true);
            EnsureStation("Station_BeerBarrel", StreetKitchenLayout.BeerBarrelBounds, true);
            EnsureStation("Station_CocaBarrel", StreetKitchenLayout.CocaBarrelBounds, true);
        }

        private bool Available(int product) => simulation != null && simulation.IsProductAvailable(product);

        private void EnsureStation(string prefabName, Rect bounds, bool visible)
        {
            string key = "station:" + prefabName;
            for (int i = 0; i < propsRoot.transform.childCount; i++)
                if (propsRoot.transform.GetChild(i).name == key)
                {
                    GameObject existing = propsRoot.transform.GetChild(i).gameObject;
                    existing.SetActive(visible);
                    SetStationTransform(existing.transform, bounds);
                    FitFirstSprite(existing, bounds.width, bounds.height);
                    return;
                }
            GameObject source = Prefab(prefabName);
            if (source == null) return;
            GameObject instance = Instantiate(source, propsRoot.transform);
            instance.name = key;
            SetLayerRecursively(instance, PropLayer);
            SetStationTransform(instance.transform, bounds);
            FitFirstSprite(instance, bounds.width, bounds.height);
            SetSorting(instance, 0);
            instance.SetActive(visible);
        }

        private void SyncFood()
        {
            if (simulation == null || simulation.Kitchen == null) return;
            staleIds.Clear();
            foreach (int id in foodObjects.Keys) staleIds.Add(id);
            foreach (StreetFoodUnit unit in simulation.Kitchen.Units)
            {
                if (unit == null || unit.Id <= 0) continue;
                if (unit.Location == StreetFoodLocation.Carried) { staleIds.Remove(unit.Id); SyncCookingEffect(unit); continue; }
                GameObject go;
                if (!foodObjects.TryGetValue(unit.Id, out go) || go == null)
                {
                    go = CreateFoodObject(unit);
                    if (go == null) continue;
                    foodObjects[unit.Id] = go;
                }
                staleIds.Remove(unit.Id);
                if (go.transform.parent != propsRoot.transform) { go.transform.SetParent(propsRoot.transform, false); SetLayerRecursively(go, PropLayer); }
                UpdateFoodObject(unit, go, false);
                SyncCookingEffect(unit);
            }

            foreach (int id in staleIds)
            {
                GameObject old;
                if (foodObjects.TryGetValue(id, out old) && old != null) Destroy(old);
                foodObjects.Remove(id);
                foodRenderers.Remove(id);
                GameObject effect; if (cookingEffects.TryGetValue(id, out effect) && effect != null) Destroy(effect);
                cookingEffects.Remove(id);
                if (heatEffects.TryGetValue(id, out effect) && effect != null) Destroy(effect);
                heatEffects.Remove(id);
            }
            SyncCarriedFood();
        }

        private GameObject CreateFoodObject(StreetFoodUnit unit)
        {
            string name = unit.Location == StreetFoodLocation.Grill ? MeatName(unit.Product)
                : unit.Product <= 3 ? SandwichName(unit.Product) : DrinkName(unit.Product);
            GameObject source = Prefab(name);
            if (source == null) return null;
            GameObject go = Instantiate(source, propsRoot.transform);
            go.name = "Food_" + unit.Id + "_" + name;
            SetLayerRecursively(go, PropLayer);
            SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) foodRenderers[unit.Id] = sr;
            return go;
        }

        private void UpdateFoodObject(StreetFoodUnit unit, GameObject go, bool carried)
        {
            bool isGrill = unit.Location == StreetFoodLocation.Grill;
            string key = isGrill ? MeatArt(unit.Product) : unit.Product <= 3 ? SandwichArt(unit.Product) : DrinkArt(unit.Product);
            SpriteRenderer renderer;
            if (foodRenderers.TryGetValue(unit.Id, out renderer) && renderer != null)
            {
                if (isGrill)
                {
                    string state = unit.State.ToString();
                    renderer.sprite = StateSprite(key, state);
                }
                else renderer.sprite = FirstSprite(key);
                SetDesiredSize(go.transform, renderer, isGrill ? MeatSize(unit.Product) : ServingSize(unit.Product, carried));
            }
            Vector2 position;
            if (isGrill) position = StreetKitchenLayout.GrillSlotPosition(unit.Product, unit.Slot, Available(unit.Product == 0 ? 1 : unit.Product == 1 ? 0 : unit.Product == 2 ? 3 : 2), simulation.Kitchen.GrillCapacity(unit.Product));
            else if (unit.Product == 4 || unit.Product == 6) position = StreetKitchenLayout.BarrelSlotPosition(unit.Product, unit.Slot);
            else position = StreetKitchenLayout.TableSlotPosition(unit.Product, unit.Slot, Available(unit.Product == 0 ? 1 : unit.Product == 1 ? 0 : unit.Product == 2 ? 3 : unit.Product == 3 ? 2 : -1), simulation.Kitchen.TableCapacity(unit.Product));
            SetItemTransform(go.transform, position, isGrill
                ? StreetKitchenLayout.GrillBoundsForProduct(unit.Product)
                : StreetKitchenLayout.BoundsForProduct(unit.Product));
            SetSorting(go, 10 + unit.Slot);
        }

        private void SyncCookingEffect(StreetFoodUnit unit)
        {
            if (unit == null || unit.Id <= 0 || unit.Product > 3) return;
            bool cooking = unit.Location == StreetFoodLocation.Grill && unit.State == StreetFoodState.Cooking;
            GameObject effect;
            if (!cooking)
            {
                if (cookingEffects.TryGetValue(unit.Id, out effect) && effect != null) effect.SetActive(false);
                if (heatEffects.TryGetValue(unit.Id, out effect) && effect != null) effect.SetActive(false);
                return;
            }
            if (!cookingEffects.TryGetValue(unit.Id, out effect) || effect == null)
            {
                GameObject source = Prefab("Effect_Smoke");
                if (source == null) return;
                effect = Instantiate(source, propsRoot.transform);
                effect.name = "Cooking_Smoke_" + unit.Id;
                SetLayerRecursively(effect, PropLayer);
                SetSorting(effect, 6);
                SpriteRenderer smokeRenderer = effect.GetComponentInChildren<SpriteRenderer>(true);
                SetDesiredSize(effect.transform, smokeRenderer, new Vector2(12f, 26f));
                cookingEffects[unit.Id] = effect;
            }
            Vector2 position = StreetKitchenLayout.GrillSlotPosition(unit.Product, unit.Slot, Available(unit.Product == 0 ? 1 : unit.Product == 1 ? 0 : unit.Product == 2 ? 3 : 2), simulation.Kitchen.GrillCapacity(unit.Product));
            SetItemTransform(effect.transform, new Vector2(position.x, position.y - 5f), StreetKitchenLayout.GrillBoundsForProduct(unit.Product));
            effect.SetActive(true);
            GameObject heat;
            if (!heatEffects.TryGetValue(unit.Id, out heat) || heat == null)
            {
                GameObject heatSource = Prefab("Effect_Heat");
                if (heatSource != null) { heat = Instantiate(heatSource, propsRoot.transform); heat.name = "Cooking_Heat_" + unit.Id; SetLayerRecursively(heat, PropLayer); SetSorting(heat, 5); SpriteRenderer heatRenderer = heat.GetComponentInChildren<SpriteRenderer>(true); SetDesiredSize(heat.transform, heatRenderer, new Vector2(12f, 18f)); heatEffects[unit.Id] = heat; }
            }
            if (heat != null) { SetItemTransform(heat.transform, position, StreetKitchenLayout.GrillBoundsForProduct(unit.Product)); SpriteRenderer heatRenderer = heat.GetComponentInChildren<SpriteRenderer>(true); if (heatRenderer != null) heatRenderer.color = new Color(1f, 1f, 1f, .24f + .16f * Mathf.Clamp01(unit.Progress)); heat.SetActive(true); }
        }

        private void SyncCarriedFood()
        {
            if (carryRoot == null) return;
            var liveCarried = new HashSet<int>();
            foreach (StreetFoodUnit unit in simulation.Kitchen.Units)
            {
                if (unit == null || unit.Location != StreetFoodLocation.Carried) continue;
                StreetWorker worker = FindWorker(unit.WorkerId);
                if (worker == null) continue;
                liveCarried.Add(unit.Id);
                GameObject go;
                if (!foodObjects.TryGetValue(unit.Id, out go) || go == null)
                {
                    string prefabName = unit.Product <= 3 ? SandwichName(unit.Product) : DrinkName(unit.Product);
                    GameObject source = Prefab(prefabName);
                    if (source == null) continue;
                    go = Instantiate(source, carryRoot.transform);
                    go.name = "Carried_" + unit.Id + "_" + prefabName;
                    SetLayerRecursively(go, CarryLayer);
                    foodObjects[unit.Id] = go;
                    SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>(true);
                    if (sr != null) foodRenderers[unit.Id] = sr;
                }
                if (go.transform.parent != carryRoot.transform) { go.transform.SetParent(carryRoot.transform, false); SetLayerRecursively(go, CarryLayer); }
                UpdateFoodObject(unit, go, true);
                float bob = (worker.State == StreetWorkerState.ToStation || worker.State == StreetWorkerState.ToCounter)
                    ? Mathf.Sin(worker.AnimationTime * 16f) * 1.5f : 0f;
                Vector2 hand = new Vector2(worker.Position.x - 9.5f, worker.Position.y * verticalScale - 31.5f + bob);
                SetCanvasTransform(go.transform, hand, 1f);
            }
            staleIds.Clear();
            foreach (KeyValuePair<int, GameObject> kv in foodObjects)
                if (kv.Value != null && kv.Value.transform.parent == carryRoot.transform && !liveCarried.Contains(kv.Key)) staleIds.Add(kv.Key);
            foreach (int id in staleIds)
            {
                GameObject old = foodObjects[id];
                if (old != null) Destroy(old);
                foodObjects.Remove(id);
                foodRenderers.Remove(id);
            }
        }

        private StreetWorker FindWorker(int id)
        {
            if (id <= 0 || simulation.Workers == null) return null;
            foreach (StreetWorker worker in simulation.Workers) if (worker != null && worker.Id == id) return worker;
            return null;
        }

        private GameObject Prefab(string name)
        {
            GameObject prefab;
            if (prefabCache.TryGetValue(name, out prefab)) return prefab;
            prefab = Resources.Load<GameObject>(PrefabRoot + name);
            prefabCache[name] = prefab;
            return prefab;
        }

        private Sprite[] Sprites(string name)
        {
            Sprite[] sprites;
            if (!spriteCache.TryGetValue(name, out sprites))
            {
                sprites = Resources.LoadAll<Sprite>(SpriteRoot + name);
                spriteCache[name] = sprites ?? new Sprite[0];
            }
            return sprites;
        }

        private Sprite FirstSprite(string name)
        {
            Sprite[] items = Sprites(name);
            return items.Length == 0 ? null : items[0];
        }

        private Sprite StateSprite(string name, string state)
        {
            Sprite[] items = Sprites(name);
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].name.EndsWith("_" + state, StringComparison.OrdinalIgnoreCase)) return items[i];
            int stateIndex = state == "Raw" ? 0 : state == "Cooking" ? 1 : state == "Cooked" ? 2 : 3;
            return items.Length > stateIndex ? items[stateIndex] : (items.Length > 0 ? items[0] : null);
        }

        private static string MeatName(int product) => product == 0 ? "Food_Chori" : product == 1 ? "Food_Paty" : product == 2 ? "Food_Bondiola" : "Food_Vacio";
        private static string SandwichName(int product) => product == 0 ? "Sandwich_Chori" : product == 1 ? "Sandwich_Paty" : product == 2 ? "Sandwich_Bondiola" : "Sandwich_Vacio";
        private static string DrinkName(int product) => product == 4 ? "Drink_Coca" : product == 5 ? "Drink_Fernet" : "Drink_Beer";
        private static string MeatArt(int product) => product == 0 ? "meat-chori" : product == 1 ? "meat-paty" : product == 2 ? "meat-bondiola" : "meat-vacio";
        private static string SandwichArt(int product) => product == 0 ? "sandwich-chori" : product == 1 ? "sandwich-paty" : product == 2 ? "sandwich-bondiola" : "sandwich-vacio";
        private static string DrinkArt(int product) => product == 4 ? "coca" : product == 5 ? "fernet" : "beer";

        private static Vector2 MeatSize(int product) => product == 0 ? new Vector2(28, 14) : product == 1 ? new Vector2(34, 20) : product == 2 ? new Vector2(34, 38) : new Vector2(90, 14);
        private static Vector2 ServingSize(int product, bool carried) => product < 4 ? (carried ? new Vector2(24, 18) : new Vector2(20, 14)) : product == 5 ? new Vector2(18, 32) : new Vector2(9, 20);

        private void SetStationTransform(Transform target, Rect bounds)
        {
            // LayoutRect scales the anchor (the image's top edge) but leaves sprite dimensions unchanged.
            Vector2 center = new Vector2(bounds.center.x, bounds.y * verticalScale + bounds.height * .5f);
            SetCanvasTransform(target, center, 1f);
        }

        private void SetItemTransform(Transform target, Vector2 itemPosition, Rect stationBounds)
        {
            // Keep each item's local offset relative to the station top unscaled vertically.
            float displayedY = stationBounds.y * verticalScale + (itemPosition.y - stationBounds.y);
            SetCanvasTransform(target, new Vector2(itemPosition.x, displayedY), 1f);
        }

        private void SetCanvasTransform(Transform target, Vector2 canvasPosition, float yScale)
        {
            target.localPosition = new Vector3((canvasPosition.x - StreetSceneLayout.Width * .5f) * .01f,
                (canvasHeight * .5f - canvasPosition.y * yScale) * .01f, 0f);
        }

        private static void FitFirstSprite(GameObject instance, float width, float height)
        {
            SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null) return;
            Vector2 source = sr.sprite.bounds.size;
            if (source.x <= 0 || source.y <= 0) return;
            float scale = Mathf.Min(width * .01f / source.x, height * .01f / source.y);
            instance.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void SetDesiredSize(Transform root, SpriteRenderer sr, Vector2 size)
        {
            if (sr == null || sr.sprite == null) return;
            Vector2 source = sr.sprite.bounds.size;
            if (source.x <= 0f || source.y <= 0f) return;
            float scale = Mathf.Min(size.x * .01f / source.x, size.y * .01f / source.y);
            root.localScale = new Vector3(scale, scale, 1f);
        }

        private static void SetSorting(GameObject go, int order)
        {
            SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = order;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDestroy()
        {
            ReleaseCamera(propsCamera);
            ReleaseCamera(carryCamera);
            ReleaseTexture(ref propsTexture);
            ReleaseTexture(ref carryTexture);
            DestroyRoot(propsRoot);
            DestroyRoot(carryRoot);
        }

        private static void ReleaseCamera(Camera camera)
        {
            if (camera == null) return;
            camera.targetTexture = null;
            camera.enabled = false;
            Destroy(camera.gameObject);
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            Destroy(texture);
            texture = null;
        }

        private static void DestroyRoot(GameObject root)
        {
            if (root != null) Destroy(root);
        }
    }
}

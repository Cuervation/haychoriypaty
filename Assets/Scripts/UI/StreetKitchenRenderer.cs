using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    /// <summary>Projects real kitchen props and StreetSimulation food entities through isolated transparent
    /// orthographic cameras. Native workers share the kitchen projection; IMGUI scenery, crowd and HUD are retained.</summary>
    [DisallowMultipleComponent]
    public sealed class StreetKitchenRenderer : MonoBehaviour
    {
        private const int PropLayer = 30, CarryLayer = 31;
        private const int RenderScale = 2;
        private const float TableSandwichScale = 1.5f, CarriedSandwichScale = 1.25f;
        private const string PrefabRoot = "ModularKitchen/Prefabs/";
        private const string SpriteRoot = "ModularKitchen/Sprites/";
        private readonly Dictionary<int, GameObject> foodObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private readonly Dictionary<int, SpriteRenderer> foodRenderers = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, GameObject> cookingEffects = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> heatEffects = new Dictionary<int, GameObject>();
        private sealed class AmbientGrillEffects
        {
            public GameObject Heat, SmokeA, SmokeB;
            public SpriteRenderer HeatRenderer, SmokeARenderer, SmokeBRenderer;
        }
        private readonly AmbientGrillEffects[] ambientGrills = new AmbientGrillEffects[2];
        private readonly List<int> staleIds = new List<int>();
        private readonly HashSet<int> liveCarriedIds = new HashSet<int>();
        private GameObject propsRoot, carryRoot;
        private Camera propsCamera, carryCamera;
        private RenderTexture propsTexture, carryTexture;
        private StreetSimulation simulation;
        private StreetWorkerProjection workerProjection;
        private float verticalScale = 1f, canvasHeight = StreetSceneLayout.Height;
        private int renderedFrame = -1;
        private bool loggedMissingResources;

        public bool IsReady { get; private set; }
        public Texture BackgroundTexture => propsTexture;
        public Texture ForegroundTexture => null;
        public bool WorkersReady => workerProjection != null && workerProjection.Ready;
        public Texture CarriedTexture => WorkersReady ? null : carryTexture;

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

        public void PrepareWorkers(Texture2D normal, Texture2D diagonal, Texture2D premium, Texture2D premiumDiagonal,
            Texture2D drink, Texture2D fernet, bool matched)
        {
            if (propsRoot == null || simulation == null) return;
            if (workerProjection == null)
            {
                GameObject source = Prefab("Sandwich_Chori");
                workerProjection = new StreetWorkerProjection(propsRoot.transform, source.GetComponentInChildren<SpriteRenderer>().sharedMaterial);
            }
            workerProjection.SetArt(normal, diagonal, premium, premiumDiagonal, drink, fernet, matched);
            workerProjection.Sync(simulation, verticalScale, canvasHeight);
            SyncCarriedFood();
            if (carryCamera != null) carryCamera.enabled = !WorkersReady;
        }

        public void ReleaseWorkers()
        {
            if (workerProjection != null) workerProjection.Dispose();
            workerProjection = null;
            if (carryCamera != null) carryCamera.enabled = true;
        }

        private void LateUpdate()
        {
            if (!IsReady || simulation == null) return;
            // Transforms and sprites are updated here, before the native cameras render for this frame.
            if (WorkersReady) workerProjection.Sync(simulation, verticalScale, canvasHeight);
            SyncFood();
            UpdateAmbientEffects();
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
            StreetKitchenLayout layout = simulation.KitchenLayout;
            EnsureStation("Station_NormalGrill", layout.NormalGrillBounds, layout.HasNormalGrill);
            EnsureStation("Station_PremiumGrill", layout.PremiumGrillBounds, layout.HasPremiumGrill);
            EnsureStation("Station_NormalTable", layout.NormalTableBounds, layout.HasNormalTable);
            EnsureStation("Station_PremiumTable", layout.PremiumTableBounds, layout.HasPremiumTable);
            EnsureStation("Station_FernetTable", layout.FernetTableBounds, layout.HasFernetTable);
            EnsureStation("Station_BeerBarrel", layout.BeerBarrelBounds, layout.HasBeerBarrel);
            EnsureStation("Station_CocaBarrel", layout.CocaBarrelBounds, layout.HasCocaBarrel);
            UpdateAmbientGrill(0, layout.NormalGrillBounds, layout.HasNormalGrill, 0f);
            UpdateAmbientGrill(1, layout.PremiumGrillBounds, layout.HasPremiumGrill, 1.7f);
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
                    FitStation(existing, bounds, !IsBarrelStation(prefabName));
                    return;
                }
            GameObject source = Prefab(prefabName);
            if (source == null) return;
            GameObject instance = Instantiate(source, propsRoot.transform);
            instance.name = key;
            SetLayerRecursively(instance, PropLayer);
            SetStationTransform(instance.transform, bounds);
            FitStation(instance, bounds, !IsBarrelStation(prefabName));
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
            bool isBarrelDrink = unit.Product == 4 || unit.Product == 6;
            bool grillCompanionAvailable = isGrill && Available(unit.Product == 0 ? 1 : unit.Product == 1 ? 0 : unit.Product == 2 ? 3 : 2);
            string key = isGrill ? MeatArt(unit.Product) : unit.Product <= 3 ? SandwichArt(unit.Product) : DrinkArt(unit.Product);
            SpriteRenderer renderer;
            if (foodRenderers.TryGetValue(unit.Id, out renderer) && renderer != null)
            {
                if (isGrill)
                {
                    string state = VisualStateName(unit.State);
                    renderer.sprite = StateSprite(key, state);
                    renderer.color = unit.State == StreetFoodState.Burned
                        ? new Color(.94f, .78f, .60f, 1f) : Color.white;
                }
                else { renderer.sprite = FirstSprite(key); renderer.color = Color.white; }
                Vector2 size = isGrill ? simulation.KitchenLayout.GrillMeatSize(unit.Product, grillCompanionAvailable)
                    : isBarrelDrink && !carried ? new Vector2(8f, 15f) : ServingSize(unit.Product, carried);
                SetDesiredSize(go.transform, renderer, size);
            }
            Vector2 position;
            if (isGrill) position = simulation.KitchenLayout.GrillSlotPosition(unit.Product, unit.Slot, grillCompanionAvailable, simulation.Kitchen.GrillCapacity(unit.Product));
            else if (unit.Product == 4 || unit.Product == 6) position = simulation.KitchenLayout.BarrelSlotPosition(unit.Product, unit.Slot);
            else position = simulation.KitchenLayout.TableSlotPosition(unit.Product, unit.Slot, Available(unit.Product == 0 ? 1 : unit.Product == 1 ? 0 : unit.Product == 2 ? 3 : unit.Product == 3 ? 2 : -1), simulation.Kitchen.TableCapacity(unit.Product));
            SetItemTransform(go.transform, position, isGrill
                ? simulation.KitchenLayout.GrillBoundsForProduct(unit.Product)
                : simulation.KitchenLayout.BoundsForProduct(unit.Product));
            float rotation = isBarrelDrink && !carried ? simulation.KitchenLayout.BarrelSlotRotation(unit.Slot) : 0f;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
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
            Vector2 position = simulation.KitchenLayout.GrillSlotPosition(unit.Product, unit.Slot, Available(unit.Product == 0 ? 1 : unit.Product == 1 ? 0 : unit.Product == 2 ? 3 : 2), simulation.Kitchen.GrillCapacity(unit.Product));
            SetItemTransform(effect.transform, new Vector2(position.x, position.y - 5f), simulation.KitchenLayout.GrillBoundsForProduct(unit.Product));
            effect.SetActive(true);
            GameObject heat;
            if (!heatEffects.TryGetValue(unit.Id, out heat) || heat == null)
            {
                GameObject heatSource = Prefab("Effect_Heat");
                if (heatSource != null) { heat = Instantiate(heatSource, propsRoot.transform); heat.name = "Cooking_Heat_" + unit.Id; SetLayerRecursively(heat, PropLayer); SetSorting(heat, 5); SpriteRenderer heatRenderer = heat.GetComponentInChildren<SpriteRenderer>(true); SetDesiredSize(heat.transform, heatRenderer, new Vector2(12f, 18f)); heatEffects[unit.Id] = heat; }
            }
            if (heat != null) { SetItemTransform(heat.transform, position, simulation.KitchenLayout.GrillBoundsForProduct(unit.Product)); SpriteRenderer heatRenderer = heat.GetComponentInChildren<SpriteRenderer>(true); if (heatRenderer != null) heatRenderer.color = new Color(1f, 1f, 1f, .24f + .16f * Mathf.Clamp01(unit.Progress)); heat.SetActive(true); }
        }

        private void UpdateAmbientEffects()
        {
            if (simulation == null || propsRoot == null) return;
            StreetKitchenLayout layout = simulation.KitchenLayout;
            UpdateAmbientGrill(0, layout.NormalGrillBounds, layout.HasNormalGrill, 0f);
            UpdateAmbientGrill(1, layout.PremiumGrillBounds, layout.HasPremiumGrill, 1.7f);
        }

        private void UpdateAmbientGrill(int index, Rect bounds, bool active, float phaseOffset)
        {
            AmbientGrillEffects effects = ambientGrills[index];
            if (!active || bounds.width <= 0f)
            {
                if (effects == null) return;
                SetAmbientActive(effects.Heat, false); SetAmbientActive(effects.SmokeA, false); SetAmbientActive(effects.SmokeB, false);
                return;
            }
            if (effects == null) ambientGrills[index] = effects = new AmbientGrillEffects();
            string family = index == 0 ? "Normal" : "Premium";
            if (effects.Heat == null) effects.Heat = CreateAmbientEffect("Ambient_" + family + "_Heat", "Effect_Heat", 2, new Vector2(bounds.width * .48f, 14f), out effects.HeatRenderer);
            if (effects.SmokeA == null) effects.SmokeA = CreateAmbientEffect("Ambient_" + family + "_Smoke_A", "Effect_Smoke", 6, new Vector2(10f, 24f), out effects.SmokeARenderer);
            if (effects.SmokeB == null) effects.SmokeB = CreateAmbientEffect("Ambient_" + family + "_Smoke_B", "Effect_Smoke", 6, new Vector2(10f, 24f), out effects.SmokeBRenderer);

            if (effects.Heat != null)
            {
                SetDesiredSize(effects.Heat.transform, effects.HeatRenderer, new Vector2(bounds.width * .48f, 14f));
                float flicker = Mathf.Sin(Time.unscaledTime * 1.8f + phaseOffset);
                if (effects.HeatRenderer != null) effects.HeatRenderer.color = new Color(1f, .34f, .08f, .10f + .035f * (flicker + 1f));
                SetItemTransform(effects.Heat.transform, new Vector2(bounds.center.x, bounds.y + bounds.height * .75f), bounds);
                SetAmbientActive(effects.Heat, true);
            }
            UpdateAmbientSmoke(effects.SmokeA, effects.SmokeARenderer, bounds, .17f, 0f, phaseOffset);
            UpdateAmbientSmoke(effects.SmokeB, effects.SmokeBRenderer, bounds, .83f, .5f, phaseOffset);
        }

        private void UpdateAmbientSmoke(GameObject smoke, SpriteRenderer renderer, Rect bounds, float xFraction, float stagger, float phaseOffset)
        {
            if (smoke == null) return;
            SetDesiredSize(smoke.transform, renderer, new Vector2(10f, 24f));
            float phase = Mathf.Repeat(Time.unscaledTime * .28f + stagger + phaseOffset, 1f);
            float alpha = .15f * Mathf.Sin(phase * Mathf.PI);
            if (renderer != null) renderer.color = new Color(1f, 1f, 1f, alpha);
            float rise = bounds.height * .65f + 15f;
            Vector2 position = new Vector2(bounds.x + bounds.width * xFraction, bounds.y + bounds.height * .78f - phase * rise);
            SetItemTransform(smoke.transform, position, bounds);
            SetAmbientActive(smoke, true);
        }

        private GameObject CreateAmbientEffect(string objectName, string prefabName, int sortingOrder, Vector2 size, out SpriteRenderer renderer)
        {
            renderer = null;
            GameObject source = Prefab(prefabName);
            if (source == null) return null;
            GameObject effect = Instantiate(source, propsRoot.transform);
            effect.name = objectName;
            SetLayerRecursively(effect, PropLayer);
            SetSorting(effect, sortingOrder);
            renderer = effect.GetComponentInChildren<SpriteRenderer>(true);
            SetDesiredSize(effect.transform, renderer, size);
            return effect;
        }

        private static void SetAmbientActive(GameObject effect, bool active)
        {
            if (effect != null && effect.activeSelf != active) effect.SetActive(active);
        }

        private void SyncCarriedFood()
        {
            if (carryRoot == null) return;
            var liveCarried = liveCarriedIds; liveCarried.Clear();
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
                int order;
                if (WorkersReady && workerProjection.Grip(worker.Id, out hand, out order))
                {
                    if (go.layer != PropLayer) SetLayerRecursively(go, PropLayer);
                    SpriteRenderer carriedRenderer;
                    if (foodRenderers.TryGetValue(unit.Id, out carriedRenderer) && carriedRenderer != null)
                    {
                        // The unit sits ON the supporting palm. Its height varies by real product artwork.
                        hand.y -= carriedRenderer.bounds.size.y * 50f;
                        carriedRenderer.sortingOrder = order;
                    }
                }
                else if (go.layer != CarryLayer) SetLayerRecursively(go, CarryLayer);
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

        private static string VisualStateName(StreetFoodState state)
        {
            if (state == StreetFoodState.Raw || state == StreetFoodState.Cooking) return "Raw";
            return state == StreetFoodState.Cooked ? "Cooked" : "Passed";
        }

        private static string ArtStateName(string state) => state == "Passed" ? "Cooked" : state;

        private Sprite StateSprite(string name, string state)
        {
            Sprite[] items = Sprites(name);
            // Passed reuses the appetizing cooked frame with a warm tint; the unused black-char frame is never shown.
            string artState = ArtStateName(state);
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].name.EndsWith("_" + artState, StringComparison.OrdinalIgnoreCase)) return items[i];
            int stateIndex = artState == "Raw" ? 0 : artState == "Cooking" ? 1 : 2;
            return items.Length > stateIndex ? items[stateIndex] : (items.Length > 0 ? items[0] : null);
        }

        private static string MeatName(int product) => product == 0 ? "Food_Chori" : product == 1 ? "Food_Paty" : product == 2 ? "Food_Bondiola" : "Food_Vacio";
        private static string SandwichName(int product) => product == 0 ? "Sandwich_Chori" : product == 1 ? "Sandwich_Paty" : product == 2 ? "Sandwich_Bondiola" : "Sandwich_Vacio";
        private static string DrinkName(int product) => product == 4 ? "Drink_Coca" : product == 5 ? "Drink_Fernet" : "Drink_Beer";
        private static string MeatArt(int product) => product == 0 ? "meat-chori" : product == 1 ? "meat-paty" : product == 2 ? "meat-bondiola" : "meat-vacio";
        private static string SandwichArt(int product) => product == 0 ? "sandwich-chori" : product == 1 ? "sandwich-paty" : product == 2 ? "sandwich-bondiola" : "sandwich-vacio";
        private static string DrinkArt(int product) => product == 4 ? "coca" : product == 5 ? "fernet" : "beer";

        private static Vector2 ServingSize(int product, bool carried)
        {
            if (product <= 3)
            {
                Vector2 baseSize = product == 0
                    ? (carried ? new Vector2(31.2f, 18f) : new Vector2(26f, 14f))
                    : (carried ? new Vector2(24f, 18f) : new Vector2(20f, 14f));
                return baseSize * (carried ? CarriedSandwichScale : TableSandwichScale);
            }

            return product == 5 ? new Vector2(18f, 32f) : new Vector2(9f, 20f);
        }

        private void SetStationTransform(Transform target, Rect bounds)
        {
            // The y transform applies to both station anchors and their footprint; scaling only
            // the anchor makes the table/grill gap negative on short 4:3 canvases.
            Vector2 center = new Vector2(bounds.center.x, (bounds.y + bounds.height * .5f) * verticalScale);
            SetCanvasTransform(target, center, 1f);
        }

        private void FitStation(GameObject instance, Rect bounds, bool preserveAspect)
        {
            FitFirstSprite(instance, bounds.width, bounds.height, preserveAspect);
            Vector3 scale = instance.transform.localScale;
            scale.y *= verticalScale;
            instance.transform.localScale = scale;
        }

        private void SetItemTransform(Transform target, Vector2 itemPosition, Rect stationBounds)
        {
            SetCanvasTransform(target, new Vector2(itemPosition.x, itemPosition.y * verticalScale), 1f);
        }

        private void SetCanvasTransform(Transform target, Vector2 canvasPosition, float yScale)
        {
            target.localPosition = new Vector3((canvasPosition.x - StreetSceneLayout.Width * .5f) * .01f,
                (canvasHeight * .5f - canvasPosition.y * yScale) * .01f, 0f);
        }

        private static bool IsBarrelStation(string prefabName) => prefabName == "Station_BeerBarrel" || prefabName == "Station_CocaBarrel";

        private static void FitFirstSprite(GameObject instance, float width, float height, bool preserveAspect)
        {
            SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null) return;
            Vector2 source = sr.sprite.bounds.size;
            if (source.x <= 0 || source.y <= 0) return;
            float scaleX = width * .01f / source.x;
            float scaleY = height * .01f / source.y;
            if (preserveAspect) scaleX = scaleY = Mathf.Min(scaleX, scaleY);
            instance.transform.localScale = new Vector3(scaleX, scaleY, 1f);
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
            if (workerProjection != null) workerProjection.Dispose();
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

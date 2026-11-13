using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class TowerDefenseWorldPresentation
    {
        private readonly Dictionary<int, GameObject> pilgrimStations = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> pilgrimFood = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, PilgrimAmbientGrillMeat> pilgrimAmbientGrillMeat = new Dictionary<int, PilgrimAmbientGrillMeat>();
        private readonly HashSet<int> occupiedPilgrimGrillSlots = new HashSet<int>();
        private readonly HashSet<int> activePilgrimAmbientGrillSlots = new HashSet<int>();
        private readonly Dictionary<int, Sprite[]> pilgrimGrillMeatSprites = new Dictionary<int, Sprite[]>();
        private Sprite pilgrimFernetCupSprite, pilgrimFernetBottleSprite, pilgrimCocaTableBottleSprite, pilgrimFernetContactShadowSprite;
        private static readonly string[] ProductPrefabs = { "Sandwich_Chori", "Sandwich_Paty", "Sandwich_Bondiola", "Sandwich_Vacio", "Drink_Coca", "Drink_Fernet", "Drink_Beer" };
        private static readonly string[] GrillFoodPrefabs = { "Food_Chori", "Food_Paty", "Food_Bondiola", "Food_Vacio" };
        private static readonly string[] AmbientMeatStates = { "Raw", "Cooking", "Cooked" };

        private sealed class PilgrimAmbientGrillMeat
        {
            public int Product, Slot, StateIndex;
            public float NextStateAt;
            public GameObject Object;
            public SpriteRenderer Renderer;
        }

        private void ClearPilgrimStations()
        {
            foreach (var item in pilgrimStations.Values) DestroyPresentationObject(item);
            foreach (var item in pilgrimFood.Values) DestroyPresentationObject(item);
            foreach (var item in pilgrimAmbientGrillMeat.Values)
                if (item.Object != null) DestroyPresentationObject(item.Object);
            pilgrimStations.Clear(); pilgrimFood.Clear(); pilgrimAmbientGrillMeat.Clear();
            occupiedPilgrimGrillSlots.Clear(); activePilgrimAmbientGrillSlots.Clear();
            DestroyPilgrimDrinkSprite(ref pilgrimFernetCupSprite);
            DestroyPilgrimDrinkSprite(ref pilgrimFernetBottleSprite);
            DestroyPilgrimDrinkSprite(ref pilgrimCocaTableBottleSprite);
            if (pilgrimFernetContactShadowSprite != null)
            {
                Texture2D shadowTexture = pilgrimFernetContactShadowSprite.texture;
                DestroyPresentationObject(pilgrimFernetContactShadowSprite);
                if (shadowTexture != null) DestroyPresentationObject(shadowTexture);
                pilgrimFernetContactShadowSprite = null;
            }
        }
        private void SyncPilgrimStations()
        {
            EnsurePilgrimDrinkSprites();
            foreach (TowerDefensePlacedStation station in mode.PlacedStations)
            {
                GameObject root;
                if (pilgrimStations.TryGetValue(station.Id, out root)) continue;
                root = new GameObject("Peregrinación placed " + station.Kind + " " + station.Id);
                root.transform.SetParent(presentationRoot.transform, false);
                root.layer = presentationLayer;
                pilgrimStations.Add(station.Id, root);
                Rect b = station.Bounds;
                int depth = 32 + Mathf.RoundToInt(-b.yMin * 12f);
                string resource = station.Kind == TowerDefenseStationKind.NormalGrill ? "Station_NormalGrill"
                    : station.Kind == TowerDefenseStationKind.PremiumGrill ? "Station_PremiumGrill"
                    : station.Kind == TowerDefenseStationKind.NormalTable ? "Station_NormalTable"
                    : station.Kind == TowerDefenseStationKind.PremiumTable ? "Station_PremiumTable"
                    : station.Kind == TowerDefenseStationKind.FernetTable ? "Station_FernetTable"
                    : station.Kind == TowerDefenseStationKind.BeerBarrel ? "Station_BeerBarrel" : "Station_CocaBarrel";
                CreatePilgrimFurniture(root.transform, resource, station.Kind, b.center, depth);
            }
            SyncPilgrimFood();
        }

        private void EnsurePilgrimDrinkSprites()
        {
            if (pilgrimFernetCupSprite == null)
                pilgrimFernetCupSprite = LoadPilgrimDrinkSprite("fernet-cup-800cc-v1", "fernet-cup-800cc-v1");
            if (pilgrimFernetBottleSprite == null)
                pilgrimFernetBottleSprite = LoadPilgrimDrinkSprite("fernet-bottle-1l-v1", "fernet-bottle-1l-v1");
            if (pilgrimCocaTableBottleSprite == null)
                pilgrimCocaTableBottleSprite = LoadPilgrimDrinkSprite("coca-2500ml-display-v1", "coca-2500ml-display-v1");
            if (pilgrimFernetContactShadowSprite == null)
                pilgrimFernetContactShadowSprite = StreetKitchenRenderer.CreateFernetContactShadowSprite();
        }

        private static Sprite LoadPilgrimDrinkSprite(string resourceName, string spriteName)
        {
            Texture2D texture = Resources.Load<Texture2D>("ModularKitchen/Art/" + resourceName);
            return texture != null ? StreetKitchenRenderer.CreateRuntimeSprite(texture, spriteName) : null;
        }

        private static void DestroyPilgrimDrinkSprite(ref Sprite sprite)
        {
            if (sprite == null) return;
            DestroyPresentationObject(sprite);
            sprite = null;
        }

        private void CreatePilgrimFurniture(Transform parent, string resource, TowerDefenseStationKind kind,
            Vector2 center, int depth)
        {
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + resource);
            if (prefab == null) return;
            GameObject obj = Instantiate(prefab, parent, false);
            obj.name = resource;
            obj.transform.position = new Vector3(center.x, center.y, 0f);
            obj.transform.localScale = Vector3.one;

            if (kind == TowerDefenseStationKind.NormalTable || kind == TowerDefenseStationKind.PremiumTable)
            {
                FitPilgrimSandwichTable(obj, depth);
            }
            else
            {
                Rect sourceBounds = mode.BocaKitchenSourceBounds(kind);
                float fit = mode.BocaKitchenFitScale() * mode.WorldUnitsPerLogicalPixel;
                Vector2 targetSize = sourceBounds.size * fit;
                SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite == null)
                {
                    DestroyPresentationObject(obj);
                    return;
                }

                Vector2 spriteSize = renderer.sprite.bounds.size;
                if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                {
                    DestroyPresentationObject(obj);
                    return;
                }
                float scaleX = targetSize.x / spriteSize.x;
                float scaleY = targetSize.y / spriteSize.y;
                bool preserveAspect = kind == TowerDefenseStationKind.NormalGrill
                    || kind == TowerDefenseStationKind.PremiumGrill
                    || kind == TowerDefenseStationKind.FernetTable;
                if (preserveAspect) scaleX = scaleY = Mathf.Min(scaleX, scaleY);
                obj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }

            SetLayerRecursively(obj, presentationLayer);
            SetSortingRecursively(obj, depth);
            if (kind == TowerDefenseStationKind.NormalTable || kind == TowerDefenseStationKind.PremiumTable)
            {
                Transform tabletop = obj.transform.Find("Tabletop");
                SpriteRenderer tabletopRenderer = tabletop != null ? tabletop.GetComponent<SpriteRenderer>() : null;
                if (tabletopRenderer != null) tabletopRenderer.sortingOrder = depth + 2;
            }
            if (kind == TowerDefenseStationKind.FernetTable)
            {
                StreetKitchenRenderer.EnsureFernetTableDisplay(obj, pilgrimFernetBottleSprite,
                    pilgrimCocaTableBottleSprite, null, pilgrimFernetContactShadowSprite, presentationLayer);
            }
        }

        private void FitPilgrimSandwichTable(GameObject station, int depth)
        {
            Transform tabletop = station.transform.Find("Tabletop");
            Transform leftTrestle = station.transform.Find("Trestle_Left");
            Transform rightTrestle = station.transform.Find("Trestle_Right");
            if (tabletop == null || leftTrestle == null || rightTrestle == null)
            {
                Debug.LogWarning("Peregrinación Boca-style sandwich table is missing a tabletop or trestle.");
                return;
            }

            // Match StreetKitchenRenderer's exact Boca table composition. The source layout is
            // scaled as a whole; only its transparent authored overlap may extend past collision bounds.
            Rect bounds = new Rect(-StreetKitchenLayout.TableWidth * .5f, -40f,
                StreetKitchenLayout.TableWidth, StreetKitchenLayout.SandwichTableHeight);
            Rect visual = StreetKitchenLayout.FoodTableVisualBounds(bounds);
            float topDepth = visual.height * StreetKitchenLayout.SandwichTableTopDepthRatio;
            float supportHeight = visual.height - topDepth + StreetKitchenLayout.SandwichTableSupportOverlap;
            float scale = mode.BocaKitchenFitScale() * mode.WorldUnitsPerLogicalPixel;
            FitTablePart(station, "Tabletop", visual.center.x, visual.yMin + topDepth * .5f,
                bounds.width, topDepth, scale, depth + 2);
            float supportWidth = bounds.width * StreetKitchenLayout.SandwichTableSupportWidthFraction;
            float leftCenterX = visual.xMin + visual.width * StreetKitchenLayout.SandwichTableSupportCenterFraction;
            float rightCenterX = visual.xMax - visual.width * StreetKitchenLayout.SandwichTableSupportCenterFraction;
            float supportCenterY = visual.yMax - supportHeight * .5f;
            FitTablePart(station, "Trestle_Left", leftCenterX, supportCenterY, supportWidth, supportHeight,
                scale, depth);
            FitTablePart(station, "Trestle_Right", rightCenterX, supportCenterY, supportWidth, supportHeight,
                scale, depth);
        }
        private void SyncPilgrimFood()
        {
            staleIds.Clear(); foreach (int id in pilgrimFood.Keys) staleIds.Add(id);
            if (mode.Kitchen == null) return;
            occupiedPilgrimGrillSlots.Clear();
            StreetKitchenLayout layout = TowerDefenseGameMode.LujanBocaKitchenVisualLayout;
            float kitchenScale = mode.BocaKitchenFitScale();
            foreach (StreetFoodUnit unit in mode.Kitchen.Units)
            {
                if (unit.Product < 0 || unit.Product >= ProductPrefabs.Length) continue;
                if (unit.Location == StreetFoodLocation.Grill && unit.Product < 4)
                    occupiedPilgrimGrillSlots.Add(AmbientGrillSlotKey(unit.Product, unit.Slot));
                TowerDefenseStationKind kind = unit.Product < 2
                    ? (unit.Location == StreetFoodLocation.Grill ? TowerDefenseStationKind.NormalGrill : TowerDefenseStationKind.NormalTable) : unit.Product < 4
                    ? (unit.Location == StreetFoodLocation.Grill ? TowerDefenseStationKind.PremiumGrill : TowerDefenseStationKind.PremiumTable) : unit.Product == 4 ? TowerDefenseStationKind.CocaBarrel
                    : unit.Product == 5 ? TowerDefenseStationKind.FernetTable : TowerDefenseStationKind.BeerBarrel;
                TowerDefensePlacedStation station = FindPilgrimStation(kind);
                if (station == null) continue;
                staleIds.Remove(unit.Id);
                GameObject obj;
                float pixel = mode.WorldUnitsPerLogicalPixel;
                bool inGrill = unit.Location == StreetFoodLocation.Grill && unit.Product < 4;
                bool carried = unit.Location == StreetFoodLocation.Carried;
                bool companionAvailable = unit.Product < 4 && (inGrill
                    ? mode.Kitchen.GrillCapacity(unit.Product ^ 1) > 0
                    : mode.Kitchen.TableCapacity(unit.Product ^ 1) > 0);
                Rect sourceBounds = inGrill ? layout.GrillBoundsForProduct(unit.Product) : mode.BocaKitchenSourceBounds(kind);
                Vector2 logicalPosition;
                Vector2 iconSize;
                float rotation = 0f;
                int displayLayer = Mathf.Max(0, unit.Slot);
                if (carried)
                {
                    TowerDefenseWorkerState carrier = null;
                    foreach (var worker in mode.Workers) if (worker.Id == unit.WorkerId) { carrier = worker; break; }
                    if (carrier == null) continue;
                    logicalPosition = Vector2.zero;
                    iconSize = new Vector2(26f, 20f);
                }
                else if (inGrill)
                {
                    int capacity = mode.Kitchen.GrillCapacity(unit.Product);
                    logicalPosition = layout.GrillSlotPosition(unit.Product, unit.Slot, companionAvailable, capacity);
                    iconSize = layout.GrillMeatSize(unit.Product, companionAvailable);
                }
                else if (unit.Product == 5)
                {
                    logicalPosition = StreetKitchenRenderer.FernetDrinkDisplayPosition(layout, unit.Slot);
                    iconSize = StreetKitchenLayout.TableServingSize(unit.Product);
                }
                else if (unit.Product == 4 || unit.Product == 6)
                {
                    logicalPosition = layout.BarrelSlotPosition(unit.Product, unit.Slot);
                    iconSize = new Vector2(8f, 15f) * StreetKitchenRenderer.BarrelDrinkScale;
                    rotation = layout.BarrelSlotRotation(unit.Slot);
                }
                else
                {
                    logicalPosition = layout.TableSlotPosition(unit.Product, unit.Slot, companionAvailable,
                        mode.Kitchen.TableCapacity(unit.Product));
                    iconSize = StreetKitchenLayout.TableServingSize(unit.Product);
                    rotation = StreetKitchenLayout.TableSlotRotation(unit.Product, unit.Slot, companionAvailable);
                }
                if (unit.Location == StreetFoodLocation.Carried)
                {
                    TowerDefenseWorkerState carrier = null;
                    foreach (var worker in mode.Workers) if (worker.Id == unit.WorkerId) { carrier = worker; break; }
                    if (carrier == null) continue;
                    // Carried objects follow their worker in world units rather than table-space.
                    logicalPosition = carrier.Position + new Vector2(18f, 40f) * pixel;
                    sourceBounds = Rect.zero;
                }
                else logicalPosition = MapBocaKitchenPointToStation(logicalPosition, sourceBounds, station, kitchenScale, pixel);
                if (!pilgrimFood.TryGetValue(unit.Id, out obj))
                {
                    string prefab = inGrill ? GrillFoodPrefabs[unit.Product] : ProductPrefabs[unit.Product];
                    obj = CreateProductDisplay(presentationRoot.transform, prefab,
                        "Physical kitchen unit " + unit.Id, logicalPosition, iconSize * (carried ? 1f : kitchenScale), 100);
                    if (obj != null) pilgrimFood.Add(unit.Id, obj);
                }
                if (obj != null)
                {
                    obj.transform.position = logicalPosition;
                    var renderer = obj.GetComponent<SpriteRenderer>();
                    if (unit.Product == 5 && !carried && unit.Location == StreetFoodLocation.Table)
                    {
                        // Boca draws prepared Fernet cups in the station's foreground band.
                        // Keep them above the Fernet counter and its static bottle display
                        // instead of deriving depth from a world-space point.
                        int stationDepth = 32 + Mathf.RoundToInt(-station.Bounds.yMin * 12f);
                        renderer.sortingOrder = stationDepth + 10 + unit.Slot;
                    }
                    else renderer.sortingOrder = 40 + Mathf.RoundToInt(-logicalPosition.y * 12f) + displayLayer;
                    string prefab = inGrill ? GrillFoodPrefabs[unit.Product] : ProductPrefabs[unit.Product];
                    var source = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefab);
                    if (source != null) renderer.sprite = source.GetComponent<SpriteRenderer>().sprite;
                    if (unit.Product == 5 && pilgrimFernetCupSprite != null)
                        renderer.sprite = pilgrimFernetCupSprite;
                    if (inGrill) renderer.sprite = PilgrimAmbientMeatSprite(unit.Product, GrillVisualStateIndex(unit.State));
                    if(renderer.sprite!=null)
                    {
                        Vector2 spriteSize=renderer.sprite.bounds.size;
                        float displayScale = carried ? 1f : kitchenScale;
                        float fit=Mathf.Min(iconSize.x*displayScale*pixel/Mathf.Max(.001f,spriteSize.x),
                            iconSize.y*displayScale*pixel/Mathf.Max(.001f,spriteSize.y));
                        obj.transform.localScale=Vector3.one*fit;
                    }
                    obj.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                    renderer.color = unit.State == StreetFoodState.Cooking ? Color.Lerp(new Color(1f,.75f,.65f), Color.white, unit.Progress) : Color.white;
                }
            }
            foreach (int id in staleIds) { DestroyPresentationObject(pilgrimFood[id]); pilgrimFood.Remove(id); }
            SyncPilgrimAmbientGrillMeat(layout, kitchenScale);
        }

        private TowerDefensePlacedStation FindPilgrimStation(TowerDefenseStationKind kind)
        {
            foreach (TowerDefensePlacedStation station in mode.PlacedStations)
                if (station.Kind == kind) return station;
            return null;
        }

        private static Vector2 MapBocaKitchenPointToStation(Vector2 point, Rect sourceBounds,
            TowerDefensePlacedStation station, float fitScale, float pixelScale)
        {
            Vector2 offset = point - sourceBounds.center;
            return station.Bounds.center + new Vector2(offset.x, -offset.y) * (fitScale * pixelScale);
        }

        private void SyncPilgrimAmbientGrillMeat(StreetKitchenLayout layout, float kitchenScale)
        {
            activePilgrimAmbientGrillSlots.Clear();
            for (int product = 0; product < 4; product++)
            {
                int capacity = mode.Kitchen.GrillCapacity(product);
                if (capacity <= 0) continue;
                bool companionAvailable = mode.Kitchen.GrillCapacity(product ^ 1) > 0;
                TowerDefenseStationKind kind = product < 2 ? TowerDefenseStationKind.NormalGrill : TowerDefenseStationKind.PremiumGrill;
                TowerDefensePlacedStation station = FindPilgrimStation(kind);
                if (station == null) continue;
                Rect sourceBounds = mode.BocaKitchenSourceBounds(kind);
                Vector2 meatSize = layout.GrillMeatSize(product, companionAvailable) * kitchenScale;
                for (int slot = 0; slot < capacity; slot++)
                {
                    int key = AmbientGrillSlotKey(product, slot);
                    activePilgrimAmbientGrillSlots.Add(key);
                    PilgrimAmbientGrillMeat visual;
                    if (!pilgrimAmbientGrillMeat.TryGetValue(key, out visual) || visual.Object == null)
                    {
                        visual = CreatePilgrimAmbientGrillMeat(product, slot, station, sourceBounds, kitchenScale, meatSize);
                        if (visual == null) continue;
                        pilgrimAmbientGrillMeat[key] = visual;
                    }

                    bool visible = !occupiedPilgrimGrillSlots.Contains(key);
                    if (visual.Object.activeSelf != visible) visual.Object.SetActive(visible);
                    if (!visible) continue;
                    if (Time.unscaledTime >= visual.NextStateAt)
                    {
                        visual.StateIndex = (visual.StateIndex + 1) % AmbientMeatStates.Length;
                        visual.NextStateAt = Time.unscaledTime + AmbientMeatStateDuration(product, slot, visual.StateIndex);
                    }
                    visual.Renderer.sprite = PilgrimAmbientMeatSprite(product, visual.StateIndex);
                    visual.Renderer.color = Color.white;
                    FitPilgrimDisplayToLogicalSize(visual.Object.transform, visual.Renderer, meatSize);
                    Vector2 sourcePoint = layout.GrillSlotPosition(product, slot, companionAvailable, capacity);
                    Vector2 worldPosition = MapBocaKitchenPointToStation(sourcePoint, sourceBounds, station,
                        kitchenScale, mode.WorldUnitsPerLogicalPixel);
                    visual.Object.transform.position = worldPosition;
                    visual.Renderer.sortingOrder = 42 + Mathf.RoundToInt(-worldPosition.y * 12f) + slot;
                }
            }
            foreach (KeyValuePair<int, PilgrimAmbientGrillMeat> pair in pilgrimAmbientGrillMeat)
                if (!activePilgrimAmbientGrillSlots.Contains(pair.Key) && pair.Value.Object != null)
                    pair.Value.Object.SetActive(false);
        }

        private PilgrimAmbientGrillMeat CreatePilgrimAmbientGrillMeat(int product, int slot,
            TowerDefensePlacedStation station, Rect sourceBounds, float kitchenScale, Vector2 meatSize)
        {
            Vector2 sourcePoint = TowerDefenseGameMode.LujanBocaKitchenVisualLayout.GrillSlotPosition(product, slot,
                mode.Kitchen.GrillCapacity(product ^ 1) > 0, mode.Kitchen.GrillCapacity(product));
            Vector2 position = MapBocaKitchenPointToStation(sourcePoint, sourceBounds, station, kitchenScale,
                mode.WorldUnitsPerLogicalPixel);
            GameObject visualObject = CreateProductDisplay(presentationRoot.transform, GrillFoodPrefabs[product],
                "Peregrinación Boca ambient grill P" + product + " S" + slot, position, meatSize, 42);
            if (visualObject == null) return null;
            SpriteRenderer renderer = visualObject.GetComponent<SpriteRenderer>();
            if (renderer == null) { DestroyPresentationObject(visualObject); return null; }
            int stateIndex = (product * 2 + slot) % AmbientMeatStates.Length;
            renderer.sprite = PilgrimAmbientMeatSprite(product, stateIndex);
            FitPilgrimDisplayToLogicalSize(visualObject.transform, renderer, meatSize);
            renderer.sortingOrder = 42 + Mathf.RoundToInt(-position.y * 12f) + slot;
            return new PilgrimAmbientGrillMeat
            {
                Product = product,
                Slot = slot,
                StateIndex = stateIndex,
                NextStateAt = Time.unscaledTime + .35f + ((product * 23 + slot * 17) % 60) * .01f,
                Object = visualObject,
                Renderer = renderer
            };
        }

        private Sprite PilgrimAmbientMeatSprite(int product, int stateIndex)
        {
            Sprite[] states;
            if (!pilgrimGrillMeatSprites.TryGetValue(product, out states))
            {
                states = new Sprite[AmbientMeatStates.Length];
                string statePath = product == 2 ? "ModularKitchen/Art/meat-bondiola-whole-v1_"
                    : product == 3 ? "ModularKitchen/Art/meat-vacio-whole-v1_" : string.Empty;
                string art = product == 0 ? "meat-chori" : "meat-paty";
                for (int i = 0; i < states.Length; i++)
                {
                    string name = AmbientMeatStates[i];
                    states[i] = product >= 2
                        ? Resources.Load<Sprite>(statePath + name)
                        : Resources.Load<Sprite>("ModularKitchen/Sprites/" + art + "/" + art + "_" + name);
                }
                pilgrimGrillMeatSprites.Add(product, states);
            }
            int index = Mathf.Clamp(stateIndex, 0, AmbientMeatStates.Length - 1);
            if (states[index] != null) return states[index];
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + GrillFoodPrefabs[product]);
            return prefab != null ? prefab.GetComponent<SpriteRenderer>().sprite : null;
        }

        private static int GrillVisualStateIndex(StreetFoodState state) =>
            state == StreetFoodState.Cooking ? 1 : state == StreetFoodState.Cooked || state == StreetFoodState.Burned ? 2 : 0;

        private static int AmbientGrillSlotKey(int product, int slot) => product * 100 + slot;

        private static float AmbientMeatStateDuration(int product, int slot, int state)
        {
            int phase = (product + 1) * 37 + (slot + 3) * 19 + state * 11;
            return 1.2f + (phase % 80) * .01f;
        }

        private void FitPilgrimDisplayToLogicalSize(Transform root, SpriteRenderer renderer, Vector2 logicalSize)
        {
            if (renderer == null || renderer.sprite == null) return;
            Vector2 size = renderer.sprite.bounds.size;
            if (size.x <= 0f || size.y <= 0f) return;
            float fit = Mathf.Min(logicalSize.x * mode.WorldUnitsPerLogicalPixel / size.x,
                logicalSize.y * mode.WorldUnitsPerLogicalPixel / size.y);
            root.localScale = Vector3.one * (fit / Mathf.Max(.0001f, presentationRoot.transform.lossyScale.x));
        }
    }
}

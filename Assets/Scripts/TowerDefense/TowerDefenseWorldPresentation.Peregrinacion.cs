using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class TowerDefenseWorldPresentation
    {
        private readonly Dictionary<int, GameObject> pilgrimStations = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> pilgrimFood = new Dictionary<int, GameObject>();
        private static readonly string[] ProductPrefabs = { "Sandwich_Chori", "Sandwich_Paty", "Sandwich_Bondiola", "Sandwich_Vacio", "Drink_Coca", "Drink_Fernet", "Drink_Beer" };

        private void ClearPilgrimStations()
        {
            foreach (var item in pilgrimStations.Values) DestroyPresentationObject(item);
            foreach (var item in pilgrimFood.Values) DestroyPresentationObject(item);
            pilgrimStations.Clear(); pilgrimFood.Clear();
        }
        private void SyncPilgrimStations()
        {
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
                CreatePilgrimFurniture(root.transform, resource, b, depth);
                if (station.Kind == TowerDefenseStationKind.FernetTable || station.Kind == TowerDefenseStationKind.CocaBarrel || station.Kind == TowerDefenseStationKind.BeerBarrel)
                {
                    int product = station.Kind == TowerDefenseStationKind.FernetTable ? 5 : station.Kind == TowerDefenseStationKind.BeerBarrel ? 6 : 4;
                    CreateProductDisplay(root.transform, ProductPrefabs[product], "Station product label",
                        b.center + Vector2.up * b.height * .15f, new Vector2(23f, 30f), depth + 2);
                }
            }
            SyncPilgrimFood();
        }
        private void CreatePilgrimFurniture(Transform parent, string resource, Rect target, int depth)
        {
            GameObject prefab = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + resource);
            if (prefab == null) return;
            GameObject obj = Instantiate(prefab, parent, false);
            obj.name = resource;
            var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            bool hasBounds = false; Bounds bounds = default;
            foreach (var renderer in renderers)
            {
                if (renderer.sprite == null) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!hasBounds) { DestroyPresentationObject(obj); return; }
            float x = target.width / Mathf.Max(.0001f, bounds.size.x);
            float y = target.height / Mathf.Max(.0001f, bounds.size.y);
            Vector3 localCenter = obj.transform.InverseTransformPoint(bounds.center);
            obj.transform.localScale = new Vector3(x, y, 1);
            obj.transform.position = new Vector3(target.center.x - localCenter.x * x,
                target.center.y - localCenter.y * y, 0);
            SetLayerRecursively(obj, presentationLayer);
            SetSortingRecursively(obj, depth);
        }
        private void SyncPilgrimFood()
        {
            staleIds.Clear(); foreach (int id in pilgrimFood.Keys) staleIds.Add(id);
            if (mode.Kitchen == null) return;
            foreach (StreetFoodUnit unit in mode.Kitchen.Units)
            {
                if (unit.Product < 0 || unit.Product >= ProductPrefabs.Length) continue;
                TowerDefenseStationKind kind = unit.Product < 2
                    ? (unit.Location == StreetFoodLocation.Grill ? TowerDefenseStationKind.NormalGrill : TowerDefenseStationKind.NormalTable) : unit.Product < 4
                    ? (unit.Location == StreetFoodLocation.Grill ? TowerDefenseStationKind.PremiumGrill : TowerDefenseStationKind.PremiumTable) : unit.Product == 4 ? TowerDefenseStationKind.CocaBarrel
                    : unit.Product == 5 ? TowerDefenseStationKind.FernetTable : TowerDefenseStationKind.BeerBarrel;
                TowerDefensePlacedStation station = null;
                foreach (var candidate in mode.PlacedStations) if (candidate.Kind == kind) { station = candidate; break; }
                if (station == null) continue;
                staleIds.Remove(unit.Id);
                GameObject obj;
                Rect b = station.Bounds;
                float pixel = mode.WorldUnitsPerLogicalPixel;
                // Tables/grills have a real slot for every physical unit, including both
                // products sharing a station. Never fold capacity48 onto four repeated points.
                bool shared = unit.Product < 4;
                int partner = unit.Product % 2 == 0 ? unit.Product + 1 : unit.Product - 1;
                int ownCapacity = unit.Location == StreetFoodLocation.Grill ? mode.Kitchen.GrillCapacity(unit.Product) : mode.Kitchen.TableCapacity(unit.Product);
                int otherCapacity = shared ? (unit.Location == StreetFoodLocation.Grill ? mode.Kitchen.GrillCapacity(partner) : mode.Kitchen.TableCapacity(partner)) : 0;
                int slot = Mathf.Max(0,unit.Slot) + (shared && unit.Product % 2 == 1 ? otherCapacity : 0);
                int capacity = Mathf.Max(1,ownCapacity+otherCapacity);
                float aspect = b.width/Mathf.Max(.001f,b.height);
                int columns = Mathf.Max(1,Mathf.CeilToInt(Mathf.Sqrt(capacity*aspect)));
                int rows = Mathf.Max(1,Mathf.CeilToInt(capacity/(float)columns));
                float cellWidth=b.width/columns, cellHeight=b.height/rows;
                Vector2 position = new Vector2(b.xMin+(slot%columns+.5f)*cellWidth,
                    b.yMax-(slot/columns+.5f)*cellHeight);
                Vector2 iconSize = new Vector2(Mathf.Min(26f,cellWidth/pixel*.85f),Mathf.Min(20f,cellHeight/pixel*.85f));
                if (unit.Location == StreetFoodLocation.Carried)
                {
                    TowerDefenseWorkerState carrier = null;
                    foreach (var worker in mode.Workers) if (worker.Id == unit.WorkerId) { carrier = worker; break; }
                    if (carrier == null) continue;
                    position = carrier.Position + new Vector2(18f, 40f) * pixel;
                    iconSize = new Vector2(26,20);
                }
                if (!pilgrimFood.TryGetValue(unit.Id, out obj))
                {
                    string prefab = unit.Product < 4 && unit.Location == StreetFoodLocation.Grill
                        ? new[] {"Food_Chori", "Food_Paty", "Food_Bondiola", "Food_Vacio"}[unit.Product] : ProductPrefabs[unit.Product];
                    obj = CreateProductDisplay(presentationRoot.transform, prefab,
                        "Physical kitchen unit " + unit.Id, position, iconSize, 100);
                    if (obj != null) pilgrimFood.Add(unit.Id, obj);
                }
                if (obj != null)
                {
                    obj.transform.position = position;
                    var renderer = obj.GetComponent<SpriteRenderer>();
                    renderer.sortingOrder = 40 + Mathf.RoundToInt(-position.y * 12f);
                    string prefab = unit.Product < 4 && unit.Location == StreetFoodLocation.Grill
                        ? new[] {"Food_Chori", "Food_Paty", "Food_Bondiola", "Food_Vacio"}[unit.Product] : ProductPrefabs[unit.Product];
                    var source = Resources.Load<GameObject>("ModularKitchen/Prefabs/" + prefab);
                    if (source != null) renderer.sprite = source.GetComponent<SpriteRenderer>().sprite;
                    if(renderer.sprite!=null)
                    {
                        Vector2 spriteSize=renderer.sprite.bounds.size;
                        float fit=Mathf.Min(iconSize.x*pixel/Mathf.Max(.001f,spriteSize.x),iconSize.y*pixel/Mathf.Max(.001f,spriteSize.y));
                        obj.transform.localScale=Vector3.one*fit;
                    }
                    renderer.color = unit.State == StreetFoodState.Cooking ? Color.Lerp(new Color(1f,.75f,.65f), Color.white, unit.Progress) : Color.white;
                }
            }
            foreach (int id in staleIds) { DestroyPresentationObject(pilgrimFood[id]); pilgrimFood.Remove(id); }
        }
    }
}

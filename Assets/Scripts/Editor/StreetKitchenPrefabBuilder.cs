#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Editor
{
    /// <summary>Imports independent modular art and builds reusable SpriteRenderer prefabs.
    /// Run manually after the approved PNGs are present under Resources/ModularKitchen/Art.</summary>
    public static class StreetKitchenPrefabBuilder
    {
        private const string ArtFolder = "Assets/Resources/ModularKitchen/Art";
        private const string PrefabFolder = "Assets/Resources/ModularKitchen/Prefabs";
        private const string SpriteFolder = "Assets/Resources/ModularKitchen/Sprites";
        private static readonly string[] MeatFiles = { "meat-chori", "meat-paty", "meat-bondiola", "meat-vacio" };
        private static readonly string[] StateNames = { "Raw", "Cooking", "Cooked", "Burned" };

        [MenuItem("Hay Chori y Paty/Modular Kitchen/Build Reusable Prefabs")]
        public static void Build()
        {
            if (!Directory.Exists(ArtFolder)) throw new DirectoryNotFoundException("Missing modular art directory: " + ArtFolder);
            Directory.CreateDirectory(PrefabFolder);
            Directory.CreateDirectory(SpriteFolder);
            AssetDatabase.Refresh();
            ConfigureAllSprites();
            AssetDatabase.Refresh();

            for (int i = 0; i < MeatFiles.Length; i++)
                CreatePrefab("Food_" + ProductName(i), SpriteAt(MeatFiles[i], 0));
            CreatePrefab("Sandwich_Chori", Sprite("sandwich-chori"));
            CreatePrefab("Sandwich_Paty", Sprite("sandwich-paty"));
            CreatePrefab("Sandwich_Bondiola", Sprite("sandwich-bondiola"));
            CreatePrefab("Sandwich_Vacio", Sprite("sandwich-vacio"));
            CreatePrefab("Drink_Coca", Sprite("coca"));
            CreatePrefab("Drink_Beer", Sprite("beer"));
            CreatePrefab("Drink_Fernet", Sprite("fernet"));

            CreatePrefab("Station_NormalGrill", Sprite("empty-grill"));
            CreatePrefab("Station_PremiumGrill", Sprite("empty-grill"));
            CreatePrefab("Station_NormalTable", Sprite("empty-table"));
            CreatePrefab("Station_PremiumTable", Sprite("empty-table"));
            CreatePrefab("Station_FernetTable", Sprite("empty-table"));
            CreateBarrel("Station_BeerBarrel");
            CreateBarrel("Station_CocaBarrel");
            CreatePrefab("Prop_Ice", Sprite("ice"));
            CreatePrefab("Effect_Smoke", Sprite("smoke"));
            CreatePrefab("Effect_Heat", Sprite("heat"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Built modular kitchen SpriteRenderer prefabs from independent source sprites.");
        }

        private static void ConfigureAllSprites()
        {
            string[] files = {
                "empty-grill", "empty-table", "blue-barrel", "sandwich-chori", "sandwich-paty",
                "sandwich-bondiola", "sandwich-vacio", "coca", "beer", "fernet", "ice", "smoke", "heat"
            };
            for (int i = 0; i < files.Length; i++) ConfigureTexture(files[i], false);
            for (int i = 0; i < MeatFiles.Length; i++) ConfigureTexture(MeatFiles[i], true);
        }

        private static void ConfigureTexture(string name, bool fourStates)
        {
            string path = ArtFolder + "/" + name + ".png";
            if (!File.Exists(path)) throw new FileNotFoundException("Expected independent PNG sprite: " + path);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Unity did not import PNG: " + path);
            // Keep PNGs as source textures. Sprite assets below reference trimmed rects, avoiding
            // removed TextureImporter.spritesheet APIs and any runtime dependency on 2D Sprite Editor.
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.maxTextureSize = 4096;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.npotScale = TextureImporterNPOTScale.None; // Preserve source shape; mipmaps avoid sparkle when large food sprites are minified.
            importer.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new InvalidOperationException("Cannot read sprite alpha for " + path);
            if (fourStates && texture.width % 4 != 0)
                throw new InvalidOperationException(name + ".png must be a horizontal strip of four equal-width cooking states.");
            Color32[] pixels = texture.GetPixels32();
            int cellWidth = fourStates ? texture.width / 4 : texture.width;
            int spriteCount = fourStates ? 4 : 1;
            for (int i = 0; i < spriteCount; i++)
            {
                int originX = fourStates ? i * cellWidth : 0;
                Rect alphaBounds = AlphaBounds(pixels, texture.width, texture.height, originX, 0, cellWidth, texture.height);
                string spriteName = fourStates ? name + "_" + StateNames[i] : name;
                SaveSpriteAsset(name, spriteName, texture, alphaBounds);
            }

            importer.isReadable = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static void SaveSpriteAsset(string artName, string spriteName, Texture2D texture, Rect rect)
        {
            string folder = SpriteFolder + "/" + artName;
            Directory.CreateDirectory(folder);
            string assetPath = folder + "/" + spriteName + ".asset";
            Sprite generated = UnityEngine.Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            generated.name = spriteName;
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, assetPath);
            }
            else
            {
                // Copy onto the existing main asset so the .meta GUID and all prefab references survive rebuilds.
                EditorUtility.CopySerialized(generated, existing);
                existing.name = spriteName;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
            }
        }


        private static Rect AlphaBounds(Color32[] pixels, int textureWidth, int textureHeight, int x0, int y0, int width, int height)
        {
            int minX = width, minY = height, maxX = -1, maxY = -1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (pixels[(y0 + y) * textureWidth + x0 + x].a < 8) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            if (maxX < minX || maxY < minY) return new Rect(x0, y0, width, height);
            return new Rect(x0 + minX, y0 + minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void CreateBarrel(string name)
        {
            Sprite barrel = Sprite("blue-barrel");
            Sprite ice = Sprite("ice");
            GameObject root = NewSpriteObject(name, barrel);
            GameObject child = NewSpriteObject("Ice", ice);
            child.transform.SetParent(root.transform, false);
            Vector2 barrelSize = barrel.bounds.size;
            Vector2 iceSize = ice.bounds.size;
            child.transform.localPosition = new Vector3(0, barrelSize.y * .30f, -.01f);
            child.transform.localScale = new Vector3(barrelSize.x * .58f / iceSize.x, barrelSize.y * .19f / iceSize.y, 1f);
            SavePrefab(root, name);
        }

        private static void CreatePrefab(string name, Sprite sprite)
        {
            if (sprite == null) throw new InvalidOperationException("Cannot build prefab " + name + ": source sprite is missing.");
            SavePrefab(NewSpriteObject(name, sprite), name);
        }

        private static GameObject NewSpriteObject(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 0;
            return go;
        }

        private static void SavePrefab(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + name + ".prefab");
            UnityEngine.Object.DestroyImmediate(go);
        }

        private static Sprite Sprite(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteAssetPath(name, name));
            if (sprite == null) throw new InvalidOperationException("Missing sprite asset " + SpriteAssetPath(name, name));
            return sprite;
        }

        private static Sprite SpriteAt(string name, int stateIndex)
        {
            string spriteName = name + "_" + StateNames[stateIndex];
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteAssetPath(name, spriteName));
            if (sprite == null) throw new InvalidOperationException("Missing sliced state sprite " + spriteName + " in " + name);
            return sprite;
        }

        private static string SpriteAssetPath(string artName, string spriteName) => SpriteFolder + "/" + artName + "/" + spriteName + ".asset";

        private static string ProductName(int product) => product == 0 ? "Chori" : product == 1 ? "Paty" : product == 2 ? "Bondiola" : "Vacio";
    }
}
#endif

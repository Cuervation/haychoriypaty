#if UNITY_EDITOR
using System.IO;
using HayChoriYPaty;
using UnityEditor;
using UnityEngine;

namespace HayChoriYPaty.Editor
{
    /// <summary>Creates only the new, modular Tower Defense art/configuration assets.</summary>
    public static class TowerDefenseAssetBuilder
    {
        private const string SourceFencePath = "Assets/Art/TowerDefense/Resources/fence-panel-v1.png";
        private const string ArrowPath = "Assets/Art/TowerDefense/Resources/fence-direction-arrow-v1.png";
        private const string ResourcesFolder = "Assets/Resources/TowerDefense";
        private const string ConfigPath = ResourcesFolder + "/Camioneros-PlazaDeMayo.asset";
        private const string LosRedondosConfigPath = ResourcesFolder + "/LosRedondos-Tandil.asset";
        private const string LaRengaConfigPath = ResourcesFolder + "/LaRenga-AutodromoBuenosAires.asset";

        [MenuItem("Hay Chori y Paty/Tower Defense/Build modular fence assets")]
        public static void Build()
        {
            if (!File.Exists(SourceFencePath))
            {
                Debug.LogError("Tower Defense fence source not found: " + SourceFencePath);
                return;
            }

            ConfigureSpriteImporter(SourceFencePath, 300);
            EnsureDirectionArrow();
            ConfigureSpriteImporter(ArrowPath, 300);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Sprite panel = AssetDatabase.LoadAssetAtPath<Sprite>(SourceFencePath);
            Sprite arrow = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
            if (panel == null || arrow == null)
            {
                Debug.LogError("Tower Defense sprite import failed; no prefabs were created.");
                return;
            }

            EnsureFolder("Assets/Resources", "TowerDefense");
            GameObject straight = EnsurePiecePrefab("FenceStraight.prefab", panel, TowerDefenseFencePieceKind.Straight, false);
            GameObject turn = EnsurePiecePrefab("FenceTurn.prefab", panel, TowerDefenseFencePieceKind.Turn, false);
            GameObject terminal = EnsurePiecePrefab("FenceTerminal.prefab", panel, TowerDefenseFencePieceKind.Terminal, false);
            GameObject direction = EnsurePiecePrefab("FenceDirectionSign.prefab", arrow, TowerDefenseFencePieceKind.DirectionSign, true);
            TowerDefenseLevelConfig config = EnsureConfigAsset();
            if (config != null)
            {
                SerializedObject serialized = new SerializedObject(config);
                serialized.FindProperty("straightFencePrefab").objectReferenceValue = straight;
                serialized.FindProperty("turnFencePrefab").objectReferenceValue = turn;
                serialized.FindProperty("terminalFencePrefab").objectReferenceValue = terminal;
                serialized.FindProperty("directionSignPrefab").objectReferenceValue = direction;
                serialized.FindProperty("directionArrowSprite").objectReferenceValue = arrow;
                serialized.FindProperty("minimumEntrySpacingLogical").floatValue =
                    TowerDefenseLevelConfig.CamionerosMinimumEntrySpacingLogical;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }

            TowerDefenseLevelConfig redondos = EnsureLosRedondosConfig(config);
            if (redondos != null)
            {
                SerializedObject serialized = new SerializedObject(redondos);
                serialized.FindProperty("straightFencePrefab").objectReferenceValue = straight;
                serialized.FindProperty("turnFencePrefab").objectReferenceValue = turn;
                serialized.FindProperty("terminalFencePrefab").objectReferenceValue = terminal;
                serialized.FindProperty("directionSignPrefab").objectReferenceValue = direction;
                serialized.FindProperty("directionArrowSprite").objectReferenceValue = arrow;
                serialized.FindProperty("minimumEntrySpacingLogical").floatValue =
                    TowerDefenseLevelConfig.DefaultMinimumEntrySpacingLogical;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(redondos);
            }

            TowerDefenseLevelConfig laRenga = EnsureLaRengaConfig(redondos);
            if (laRenga != null)
            {
                SerializedObject serialized = new SerializedObject(laRenga);
                serialized.FindProperty("straightFencePrefab").objectReferenceValue = straight;
                serialized.FindProperty("turnFencePrefab").objectReferenceValue = turn;
                serialized.FindProperty("terminalFencePrefab").objectReferenceValue = terminal;
                serialized.FindProperty("directionSignPrefab").objectReferenceValue = direction;
                serialized.FindProperty("directionArrowSprite").objectReferenceValue = arrow;
                serialized.FindProperty("totalFanCount").intValue = TowerDefenseLevelConfig.FansToServe;
                serialized.FindProperty("fanSpeed").floatValue = TowerDefenseLevelConfig.DefaultFanSpeed;
                serialized.FindProperty("minimumEntrySpacingLogical").floatValue = TowerDefenseLevelConfig.CamionerosMinimumEntrySpacingLogical;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(laRenga);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Tower Defense modular assets ready: shared fence pieces and Camioneros, Los Redondos, and La Renga configs.");
        }

        private static TowerDefenseLevelConfig EnsureLaRengaConfig(TowerDefenseLevelConfig source)
        {
            if (source == null) return null;
            TowerDefenseLevelConfig config = AssetDatabase.LoadAssetAtPath<TowerDefenseLevelConfig>(LaRengaConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TowerDefenseLevelConfig>();
                AssetDatabase.CreateAsset(config, LaRengaConfigPath);
            }
            EditorUtility.CopySerialized(source, config);
            config.name = "LaRenga-AutodromoBuenosAires";
            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("modeId").stringValue = "tower-defense";
            serialized.FindProperty("levelId").stringValue = TowerDefenseLevelConfig.LaRengaLevelId;
            serialized.FindProperty("displayName").stringValue = "La Renga – Autódromo Bs. As.";
            serialized.FindProperty("sourceCampaignLevelId").intValue = TowerDefenseLevelConfig.LaRengaCampaignLevelId;
            serialized.FindProperty("backgroundResource").stringValue = "street-background-larenga-autodromo-v1";
            serialized.FindProperty("totalFanCount").intValue = TowerDefenseLevelConfig.FansToServe;
            serialized.FindProperty("fanSpeed").floatValue = TowerDefenseLevelConfig.DefaultFanSpeed;
            serialized.FindProperty("minimumEntrySpacingLogical").floatValue = TowerDefenseLevelConfig.CamionerosMinimumEntrySpacingLogical;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static TowerDefenseLevelConfig EnsureLosRedondosConfig(TowerDefenseLevelConfig source)
        {
            if (source == null) return null;
            TowerDefenseLevelConfig config = AssetDatabase.LoadAssetAtPath<TowerDefenseLevelConfig>(LosRedondosConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TowerDefenseLevelConfig>();
                AssetDatabase.CreateAsset(config, LosRedondosConfigPath);
            }

            // Los Redondos inherits all shared gameplay and route fields from the canonical
            // defense config on every rebuild; only its campaign identity/background differ.
            EditorUtility.CopySerialized(source, config);
            config.name = "LosRedondos-Tandil";

            SerializedObject serialized = new SerializedObject(config);
            serialized.FindProperty("modeId").stringValue = "tower-defense";
            serialized.FindProperty("levelId").stringValue = TowerDefenseLevelConfig.LosRedondosLevelId;
            serialized.FindProperty("displayName").stringValue = "Los Redondos / Tandil";
            serialized.FindProperty("sourceCampaignLevelId").intValue = TowerDefenseLevelConfig.LosRedondosCampaignLevelId;
            serialized.FindProperty("backgroundResource").stringValue = "street-background-losredondos-v1";
            serialized.FindProperty("totalFanCount").intValue = TowerDefenseLevelConfig.FansToServe;
            serialized.FindProperty("fanSpeed").floatValue = TowerDefenseLevelConfig.DefaultFanSpeed;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private static TowerDefenseLevelConfig EnsureConfigAsset()
        {
            var config = AssetDatabase.LoadAssetAtPath<TowerDefenseLevelConfig>(ConfigPath);
            if (config != null) return config;
            config = ScriptableObject.CreateInstance<TowerDefenseLevelConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static GameObject EnsurePiecePrefab(string fileName, Sprite sprite,
            TowerDefenseFencePieceKind kind, bool isDirectionSign)
        {
            string path = ResourcesFolder + "/" + fileName;
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject(Path.GetFileNameWithoutExtension(fileName));
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = isDirectionSign ? 22 : 20;
            if (!isDirectionSign)
            {
                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(sprite.bounds.size.x, 0.12f);
            }
            root.AddComponent<TowerDefenseFencePiece>().Initialize(kind, -1, -1);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void EnsureDirectionArrow()
        {
            if (File.Exists(ArrowPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ArrowPath));
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "fence-direction-arrow-v1",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color(0.18f, 0.11f, 0.09f, 1f);
            Color signFill = new Color(0.77f, 0.31f, 0.12f, 1f);
            Color arrowFill = new Color(1f, 0.91f, 0.69f, 1f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 31.5f;
                float dy = y - 31.5f;
                float distance = dx * dx + dy * dy;
                bool outerCircle = distance <= 30f * 30f;
                bool innerCircle = distance <= 25f * 25f;
                bool arrowOutline = IsArrow(x, y, 5f, 9f);
                bool arrowCore = IsArrow(x, y, 3f, 6f);
                pixels[y * size + x] = arrowCore ? arrowFill
                    : arrowOutline ? outline
                    : innerCircle ? signFill
                    : outerCircle ? outline : clear;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(ArrowPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static bool IsArrow(int x, int y, float halfWidth, float headHalfHeight)
        {
            float centerY = 31.5f;
            bool shaft = x >= 13f && x <= 40f && Mathf.Abs(y - centerY) <= halfWidth;
            bool head = x >= 33f && x <= 52f && Mathf.Abs(y - centerY) <= (52f - x) * headHalfHeight / 19f;
            return shaft || head;
        }

        private static void ConfigureSpriteImporter(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif

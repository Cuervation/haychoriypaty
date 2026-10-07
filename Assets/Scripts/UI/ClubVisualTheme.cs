using System;
using UnityEngine;

namespace HayChoriYPaty
{
    public enum FanAtlasLayoutKind { Grid, PairedColumns }

    /// <summary>Explicit source-frame contract shared by ordinary, walking, and defeat renderers.</summary>
    public sealed class FanAtlasLayout
    {
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public FanAtlasLayoutKind Kind { get; private set; }
        public bool ReverseVariantRows { get; private set; }

        public FanAtlasLayout(int columns, int rows, FanAtlasLayoutKind kind, bool reverseVariantRows = false)
        {
            Columns = columns; Rows = rows; Kind = kind; ReverseVariantRows = reverseVariantRows;
        }

        public Rect SourceRect(int textureWidth, int textureHeight, int variant, int variantsPerRow, int pose = 0)
        {
            if (textureWidth <= 0 || textureHeight <= 0 || Columns <= 0 || Rows <= 0 || variantsPerRow <= 0) return Rect.zero;
            int row = variant / variantsPerRow;
            int column = variant % variantsPerRow;
            if (ReverseVariantRows) row = Rows - 1 - row;
            if (Kind == FanAtlasLayoutKind.PairedColumns) column = column * 2 + (pose & 1);
            if (column >= Columns || row >= Rows) return Rect.zero;
            float cellWidth = textureWidth / (float)Columns, cellHeight = textureHeight / (float)Rows;
            return new Rect(column * cellWidth, textureHeight - (row + 1) * cellHeight, cellWidth, cellHeight);
        }
    }

    public sealed class ClubFanDefinition
    {
        private const string GenericRiotFallback = "street-riot-fans-v1";
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> TextureCache =
            new System.Collections.Generic.Dictionary<string, Texture2D>(System.StringComparer.Ordinal);

        public string FrontFans { get; private set; }
        public string WalkingFans { get; private set; }
        public string RiotFans { get; private set; }
        public int VariantCount { get; private set; }
        public int VariantsPerRow { get; private set; }
        public int RiotVariantsPerRow { get; private set; }
        public int RiotPoses { get; private set; }
        public FanAtlasLayout FrontLayout { get; private set; }
        public FanAtlasLayout WalkingLayout { get; private set; }
        public FanAtlasLayout RiotLayout { get; private set; }
        // Authored body-scale / transparent-foot-padding metrics, indexed by variant and pose.
        // Existing atlases retain their original drawing bounds when no metrics are configured.
        public Vector2[] RiotPoseMetrics { get; private set; }
        public ClubFanDefinition WithRiotPoseMetrics(Vector2[] metrics)
        {
            RiotPoseMetrics = metrics;
            return this;
        }

        public Rect RiotDrawBounds(Rect original, int customerId, int pose)
        {
            if (RiotPoseMetrics == null) return original;
            Vector2 metric = RiotPoseMetrics[GetVariant(customerId) * 2 + (pose & 1)];
            float size = original.height * metric.x;
            return new Rect(original.center.x - size * .5f, original.yMax - size + size * metric.y, size, size);
        }

        public ClubFanDefinition(string front, string walking, string riot, int variants, int variantsPerRow,
            FanAtlasLayout frontLayout, FanAtlasLayout walkingLayout, FanAtlasLayout riotLayout, int riotPoses = 2, int riotVariantsPerRow = 0)
        {
            FrontFans = front; WalkingFans = walking; RiotFans = riot; VariantCount = variants;
            VariantsPerRow = variantsPerRow; FrontLayout = frontLayout; WalkingLayout = walkingLayout;
            RiotLayout = riotLayout; RiotPoses = riotPoses; RiotVariantsPerRow = riotVariantsPerRow > 0 ? riotVariantsPerRow : variantsPerRow;
        }

        public int GetVariant(int customerId)
        {
            if (VariantCount < 1) return 0;
            int variant = ((customerId % VariantCount) + VariantCount) % VariantCount;
            return variant;
        }

        public Rect FrontSourceRect(int width, int height, int customerId) => FrontLayout.SourceRect(width, height, GetVariant(customerId), VariantsPerRow);
        public Rect WalkingSourceRect(int width, int height, int customerId) => WalkingLayout.SourceRect(width, height, GetVariant(customerId), VariantsPerRow, WalkingLayout.Kind == FanAtlasLayoutKind.PairedColumns ? 1 : 0);
        public Rect RiotSourceRect(int width, int height, int customerId, int pose) => RiotLayout.SourceRect(width, height, GetVariant(customerId), RiotVariantsPerRow, pose);

        public Texture2D LoadFrontFans() => Load(FrontFans);
        public Texture2D LoadWalkingFans() => Load(WalkingFans);
        public Texture2D LoadRiotFans() => Load(RiotFans);

        public bool IsComplete(bool checkResources = true)
        {
            if (VariantCount < 1 || VariantsPerRow < 1 || RiotVariantsPerRow < 1 || RiotPoses != 2 || FrontLayout == null || WalkingLayout == null || RiotLayout == null) return false;
            if (RiotLayout.Kind != FanAtlasLayoutKind.PairedColumns) return false;
            if (RiotPoseMetrics != null)
            {
                if (RiotPoseMetrics.Length != VariantCount * RiotPoses) return false;
                foreach (Vector2 metric in RiotPoseMetrics)
                    if (metric.x <= 0f || metric.y < 0f || metric.y >= 1f) return false;
            }
            if (string.IsNullOrEmpty(FrontFans) || string.IsNullOrEmpty(WalkingFans) || string.IsNullOrEmpty(RiotFans) || RiotFans == GenericRiotFallback) return false;
            if (!Capacity(FrontLayout) || !Capacity(WalkingLayout) || !Capacity(RiotLayout)) return false;
            if (!checkResources) return true;
            return ValidTexture(FrontFans, FrontLayout) && ValidTexture(WalkingFans, WalkingLayout) && ValidTexture(RiotFans, RiotLayout);
        }

        private bool Capacity(FanAtlasLayout layout)
        {
            int variantsPerRow = ReferenceEquals(layout, RiotLayout) ? RiotVariantsPerRow : VariantsPerRow;
            int requiredColumns = layout.Kind == FanAtlasLayoutKind.PairedColumns ? variantsPerRow * 2 : variantsPerRow;
            int requiredRows = (VariantCount + variantsPerRow - 1) / variantsPerRow;
            return layout.Columns >= requiredColumns && layout.Rows >= requiredRows;
        }

        private bool ValidTexture(string name, FanAtlasLayout layout)
        {
            Texture2D texture = Load(name);
            return texture != null && texture.width >= layout.Columns && texture.height >= layout.Rows;
        }

        private static Texture2D Load(string resource)
        {
            if (string.IsNullOrEmpty(resource)) return null;
            if (!TextureCache.TryGetValue(resource, out Texture2D texture) || texture == null)
            {
                texture = Resources.Load<Texture2D>(resource);
                TextureCache[resource] = texture;
            }
            return texture;
        }
    }

    /// <summary>
    /// Club palette keyed by the actual runtime level name, not by scene copies or a second level order.
    /// Add one entry here when a club is added to StreetSimulation.LevelNames.
    /// </summary>
    public sealed class ClubVisualTheme
    {
        private static readonly ClubVisualTheme[] ConfiguredThemes =
        {
            new ClubVisualTheme("Floresta / All Boys", new Color32(245, 242, 234, 255), new Color32(0, 0, 0, 255), fans: Grid3("street-allboys-fans-front-v1", "street-allboys-fans-walk-v1", "street-allboys-riot-fans-v1", 9, 3, 6, 3)),
            new ClubVisualTheme("Nueva Chicago", new Color32(0, 122, 61, 255), new Color32(0, 0, 0, 255),
                sceneryTint: new Color(1f, .97f, .92f), muralResource: "street-background-chicago-v1", muralSource: new Rect(0, .0209f, 1, .1225f), fans: Grid3("street-chicago-fans-front-v1", "street-chicago-fans-walk-v1", "street-chicago-riot-fans-v2", 8, 3, 4, 4)),
            new ClubVisualTheme("Liniers - Velez Sarsfield", new Color32(8, 48, 132, 255), new Color32(245, 242, 234, 255),
                sceneryTint: new Color(1f, .99f, .95f), muralResource: "street-mural-velez-master-v1", muralSource: new Rect(0, .22f, 1, .69f), fans: Grid3("street-velez-fans-front-v1", "street-velez-fans-walk-v1", "street-velez-riot-fans-v1", 9, 3, 6, 3)),
            new ClubVisualTheme("Ferro Carril Oeste", new Color32(0, 105, 63, 255), new Color32(245, 242, 234, 255),
                sceneryTint: new Color(.91f, .95f, 1f), muralResource: "street-mural-ferro-master-v1", muralSource: new Rect(0, .26f, 1, .70f), fans: Grid4("street-ferro-fans-front-v2", "street-ferro-fans-walk-v2", "street-ferro-riot-fans-v2", 12)),
            new ClubVisualTheme("Independiente de Avellaneda", new Color32(211, 29, 39, 255), new Color32(245, 242, 234, 255),
                sceneryTint: new Color(1f, .94f, .89f), muralResource: "street-mural-independiente-master-v1", muralSource: new Rect(0, .123f, 1, .768f), fans: Grid4("street-independiente-fans-front-v2", "street-independiente-fans-walk-v2", "street-independiente-riot-fans-v3", 12).WithRiotPoseMetrics(RiotMetrics4())),
            new ClubVisualTheme("Racing Club / Avellaneda", new Color32(70, 174, 223, 255), new Color32(245, 248, 252, 255), new Color32(24, 55, 87, 255),
                muralResource: "street-background-racing-v1", muralSource: new Rect(0, .10f, 1, .193f), fans: Paired("street-racing-fans-paired-v1", "street-racing-riot-fans-v2").WithRiotPoseMetrics(RiotMetrics5())),
            new ClubVisualTheme("San Lorenzo / Boedo", new Color32(18, 47, 104, 255), new Color32(194, 31, 55, 255), new Color32(245, 243, 237, 255),
                muralResource: "street-background-sanlorenzo-v1", muralSource: new Rect(0, .107f, 1, .206f), fans: Paired("street-sanlorenzo-fans-paired-v1", "street-sanlorenzo-riot-fans-v2").WithRiotPoseMetrics(RiotMetrics6())),
            new ClubVisualTheme("River Plate / Núñez", new Color32(210, 28, 42, 255), new Color32(247, 245, 239, 255),
                sceneryTint: Color.white, muralResource: "street-mural-river-master-v1", fans: Paired("street-river-fans-paired-v1", "street-river-riot-fans-v2", true).WithRiotPoseMetrics(RiotMetrics7())),
            new ClubVisualTheme("Boca Juniors / La Boca", new Color32(0, 66, 145, 255), new Color32(255, 197, 0, 255), new Color32(247, 244, 235, 255),
                muralResource: "street-background-boca-v1", muralSource: new Rect(0, .14f, 1, .294f), fans: Paired("street-boca-fans-paired-v1", "street-boca-riot-fans-v2").WithRiotPoseMetrics(RiotMetrics8())),
            new ClubVisualTheme("Sindicato de Camioneros / Plaza de Mayo", new Color32(0, 105, 63, 255), new Color32(245, 242, 234, 255),
                muralResource: "street-background-camioneros-v1", muralSource: new Rect(0, .10f, 1, .42f), fans: Grid4("street-ferro-fans-front-v2", "street-ferro-fans-walk-v2", "street-ferro-riot-fans-v2", 12)),
            new ClubVisualTheme("Los Redondos / Tandil", new Color32(24, 25, 30, 255), new Color32(180, 38, 48, 255), new Color32(220, 171, 58, 255),
                muralResource: "street-background-losredondos-v1", muralSource: new Rect(0, .155f, 1, .242f), fans: Grid4("street-losredondos-fans-front-v1", "street-losredondos-fans-walk-v1", "street-losredondos-riot-fans-v1", 12).WithRiotPoseMetrics(RiotMetrics10()))
        };

        private static readonly ClubVisualTheme UnknownTheme =
            new ClubVisualTheme("Equipo sin configurar", new Color32(235, 230, 218, 255), new Color32(45, 43, 40, 255));

        public string ClubName { get; private set; }
        public Color PrimaryColor { get; private set; }
        public Color SecondaryColor { get; private set; }
        public Color? AccentColor { get; private set; }
        public Color SceneryTint { get; private set; }
        public string MuralResource { get; private set; }
        public Rect MuralSource { get; private set; }
        public ClubFanDefinition Fans { get; private set; }

        private ClubVisualTheme(string clubName, Color primary, Color secondary, Color? accent = null,
            Color? sceneryTint = null, string muralResource = null, Rect? muralSource = null, ClubFanDefinition fans = null)
        {
            ClubName = clubName;
            PrimaryColor = primary;
            SecondaryColor = secondary;
            AccentColor = accent;
            SceneryTint = sceneryTint ?? Color.white;
            MuralResource = muralResource;
            MuralSource = muralSource ?? new Rect(0, 0, 1, 1);
            Fans = fans;
        }

        private static Vector2[] RiotMetrics4() => new[]
        {
            new Vector2(1.3239f,0.1992f), new Vector2(1.2918f,0.1908f),
            new Vector2(1.2780f,0.1907f), new Vector2(1.2548f,0.1863f),
            new Vector2(1.3752f,0.1962f), new Vector2(1.2688f,0.1943f),
            new Vector2(1.3325f,0.1563f), new Vector2(1.2858f,0.1523f),
            new Vector2(1.2929f,0.1563f), new Vector2(1.2780f,0.1484f),
            new Vector2(1.3497f,0.1563f), new Vector2(1.3251f,0.1563f),
            new Vector2(1.2873f,0.1055f), new Vector2(1.2721f,0.1094f),
            new Vector2(1.3429f,0.1055f), new Vector2(1.3103f,0.1055f),
            new Vector2(1.3044f,0.1055f), new Vector2(1.3207f,0.1055f),
            new Vector2(1.2978f,0.0703f), new Vector2(1.2662f,0.0664f),
            new Vector2(1.2667f,0.0703f), new Vector2(1.2362f,0.0586f),
            new Vector2(1.2696f,0.0703f), new Vector2(1.2620f,0.0703f),
        };

        private static Vector2[] RiotMetrics5() => new[]
        {
            new Vector2(1.2503f,0.1950f), new Vector2(1.2877f,0.1953f),
            new Vector2(1.2508f,0.1950f), new Vector2(1.2436f,0.1950f),
            new Vector2(1.2751f,0.1925f), new Vector2(1.2751f,0.1925f),
            new Vector2(1.2380f,0.1406f), new Vector2(1.2380f,0.1445f),
            new Vector2(1.2673f,0.1406f), new Vector2(1.2981f,0.1406f),
            new Vector2(1.2535f,0.1406f), new Vector2(1.2680f,0.1406f),
            new Vector2(1.1781f,0.0898f), new Vector2(1.1850f,0.0898f),
            new Vector2(1.2701f,0.0898f), new Vector2(1.3085f,0.0938f),
            new Vector2(1.1992f,0.0898f), new Vector2(1.2418f,0.0898f),
            new Vector2(1.2217f,0.0469f), new Vector2(1.2217f,0.0508f),
            new Vector2(1.2767f,0.0469f), new Vector2(1.2688f,0.0508f),
            new Vector2(1.2797f,0.0469f), new Vector2(1.3035f,0.0469f),
        };

        private static Vector2[] RiotMetrics6() => new[]
        {
            new Vector2(1.3464f,0.2411f), new Vector2(1.3299f,0.2332f),
            new Vector2(1.2703f,0.2408f), new Vector2(1.2557f,0.2329f),
            new Vector2(1.3917f,0.2412f), new Vector2(1.3829f,0.2334f),
            new Vector2(1.3386f,0.1875f), new Vector2(1.3221f,0.1875f),
            new Vector2(1.3357f,0.1875f), new Vector2(1.3357f,0.1875f),
            new Vector2(1.3541f,0.1875f), new Vector2(1.3376f,0.1875f),
            new Vector2(1.3064f,0.1641f), new Vector2(1.2510f,0.1641f),
            new Vector2(1.3165f,0.1641f), new Vector2(1.3246f,0.1563f),
            new Vector2(1.2955f,0.1641f), new Vector2(1.2797f,0.1641f),
            new Vector2(1.2982f,0.1250f), new Vector2(1.2656f,0.1211f),
            new Vector2(1.3009f,0.1250f), new Vector2(1.3009f,0.1172f),
            new Vector2(1.3283f,0.1289f), new Vector2(1.3199f,0.1211f),
        };

        private static Vector2[] RiotMetrics7() => new[]
        {
            new Vector2(1.3491f,0.0508f), new Vector2(1.3580f,0.0469f),
            new Vector2(1.4041f,0.0508f), new Vector2(1.3491f,0.0469f),
            new Vector2(1.3684f,0.0508f), new Vector2(1.3955f,0.0469f),
            new Vector2(1.3829f,0.1250f), new Vector2(1.3224f,0.1211f),
            new Vector2(1.3807f,0.1250f), new Vector2(1.4261f,0.1250f),
            new Vector2(1.4201f,0.1250f), new Vector2(1.3564f,0.1250f),
            new Vector2(1.3710f,0.1836f), new Vector2(1.3625f,0.1797f),
            new Vector2(1.3518f,0.1836f), new Vector2(1.3518f,0.1797f),
            new Vector2(1.3732f,0.1758f), new Vector2(1.3818f,0.1758f),
            new Vector2(1.3774f,0.2480f), new Vector2(1.3602f,0.2440f),
            new Vector2(1.3488f,0.2503f), new Vector2(1.3163f,0.2423f),
            new Vector2(1.3829f,0.2466f), new Vector2(1.3917f,0.2505f),
        };

        private static Vector2[] RiotMetrics8() => new[]
        {
            new Vector2(1.3299f,0.2670f), new Vector2(1.3464f,0.2670f),
            new Vector2(1.3215f,0.2631f), new Vector2(1.3215f,0.2631f),
            new Vector2(1.3625f,0.2671f), new Vector2(1.3884f,0.2671f),
            new Vector2(1.3274f,0.1836f), new Vector2(1.3964f,0.1836f),
            new Vector2(1.3440f,0.1875f), new Vector2(1.3874f,0.1836f),
            new Vector2(1.3541f,0.1875f), new Vector2(1.3884f,0.1875f),
            new Vector2(1.3257f,0.1680f), new Vector2(1.3342f,0.1641f),
            new Vector2(1.3665f,0.1680f), new Vector2(1.4112f,0.1680f),
            new Vector2(1.3253f,0.1680f), new Vector2(1.3595f,0.1641f),
            new Vector2(1.3415f,0.1211f), new Vector2(1.3415f,0.1211f),
            new Vector2(1.3983f,0.1211f), new Vector2(1.3983f,0.1172f),
            new Vector2(1.3807f,0.1172f), new Vector2(1.3991f,0.1211f),
        };

        private static Vector2[] RiotMetrics10() => new[]
        {
            new Vector2(1.3264f,0.1842f), new Vector2(1.3771f,0.1806f),
            new Vector2(1.2786f,0.1878f), new Vector2(1.3581f,0.1805f),
            new Vector2(1.2542f,0.1838f), new Vector2(1.2693f,0.1800f),
            new Vector2(1.3444f,0.1250f), new Vector2(1.3780f,0.1133f),
            new Vector2(1.3061f,0.1211f), new Vector2(1.2983f,0.1172f),
            new Vector2(1.2865f,0.1211f), new Vector2(1.3098f,0.1094f),
            new Vector2(1.2918f,0.0820f), new Vector2(1.2840f,0.0742f),
            new Vector2(1.2613f,0.0820f), new Vector2(1.2997f,0.0742f),
            new Vector2(1.2777f,0.0781f), new Vector2(1.2622f,0.0742f),
            new Vector2(1.2592f,0.0469f), new Vector2(1.3076f,0.0469f),
            new Vector2(1.1682f,0.0469f), new Vector2(1.3669f,0.0469f),
            new Vector2(1.3143f,0.0469f), new Vector2(1.3397f,0.0391f),
        };
        private static FanAtlasLayout Grid(int columns, int rows, bool reverse = false) => new FanAtlasLayout(columns, rows, FanAtlasLayoutKind.Grid, reverse);
        private static ClubFanDefinition Grid3(string front, string walk, string riot, int variants, int columns, int riotColumns, int riotRows) =>
            new ClubFanDefinition(front, walk, riot, variants, columns, Grid(columns, 3), Grid(columns, 3), new FanAtlasLayout(riotColumns, riotRows, FanAtlasLayoutKind.PairedColumns), 2, riotColumns == 4 ? 2 : 3);
        private static ClubFanDefinition Grid4(string front, string walk, string riot, int variants) =>
            new ClubFanDefinition(front, walk, riot, variants, 3, Grid(3, 4), Grid(3, 4), new FanAtlasLayout(6, 4, FanAtlasLayoutKind.PairedColumns));
        private static ClubFanDefinition Paired(string paired, string riot, bool river = false)
        {
            var layout = new FanAtlasLayout(6, 4, FanAtlasLayoutKind.PairedColumns, river);
            return new ClubFanDefinition(paired, paired, riot, 12, 3, layout, layout, layout);
        }

        public static ClubVisualTheme ForLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= StreetSimulation.LevelNames.Length)
                return UnknownTheme;

            return ForClub(StreetSimulation.LevelNames[levelIndex]);
        }

        public static ClubVisualTheme ForClub(string clubName)
        {
            for (int i = 0; i < ConfiguredThemes.Length; i++)
            {
                if (string.Equals(ConfiguredThemes[i].ClubName, clubName, StringComparison.OrdinalIgnoreCase))
                    return ConfiguredThemes[i];
            }

            return UnknownTheme;
        }

        public Color GetPennantColor(int pennantIndex)
        {
            if (AccentColor.HasValue && pennantIndex >= 0 && pennantIndex % 6 == 4)
                return AccentColor.Value;

            return Mathf.Abs(pennantIndex) % 2 == 0 ? PrimaryColor : SecondaryColor;
        }
    }

    /// <summary>Creates the small shared footer pennant row once per active club palette.</summary>
    public static class ClubPennantRenderer
    {
        public const int PennantCount = 20;
        public const int TextureWidth = 1080;
        public const int TextureHeight = 160;
        public const float LogicalHeight = 80f;

        private static readonly Color32 Outline = new Color32(31, 25, 21, 255);
        private static readonly Color32 RopeHighlight = new Color32(206, 184, 151, 255);

        public static Texture2D CreateTexture(ClubVisualTheme theme, bool keepReadable = false)
        {
            if (theme == null) theme = ClubVisualTheme.ForLevel(-1);

            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, false)
            {
                name = "Club Pennants - " + theme.ClubName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[TextureWidth * TextureHeight];
            Vector2 ropeStart = new Vector2(0f, RopeY(0f));
            for (int i = 1; i <= TextureWidth; i++)
            {
                float t = i / (float)TextureWidth;
                Vector2 next = new Vector2(i, RopeY(t));
                DrawLine(pixels, ropeStart, next, Outline, 5f);
                DrawLine(pixels, ropeStart + new Vector2(0f, -1.5f), next + new Vector2(0f, -1.5f), RopeHighlight, 1.2f);
                ropeStart = next;
            }

            float pitch = TextureWidth / (float)PennantCount;
            for (int i = 0; i < PennantCount; i++)
            {
                float centerX = (i + .5f) * pitch;
                float leftX = centerX - pitch * .47f;
                float rightX = centerX + pitch * .47f;
                var left = new Vector2(leftX, RopeY(leftX / TextureWidth));
                var right = new Vector2(rightX, RopeY(rightX / TextureWidth));
                var tip = new Vector2(centerX, TextureHeight - 2f);
                Color fill = theme.GetPennantColor(i);

                FillTriangle(pixels, left, right, tip, fill);
                DrawLine(pixels, left, right, Outline, 4f);
                DrawLine(pixels, right, tip, Outline, 4f);
                DrawLine(pixels, tip, left, Outline, 4f);

                // A narrow shaded fold keeps the simple generated row in the game's inked, fabric-like style.
                Vector2 foldStart = Vector2.Lerp(left, right, .30f);
                Vector2 foldEnd = Vector2.Lerp(tip, Vector2.Lerp(left, right, .30f), .18f);
                Color fold = Color.Lerp(fill, Outline, .28f);
                fold.a = .65f;
                DrawLine(pixels, foldStart, foldEnd, fold, 2f);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, !keepReadable);
            return texture;
        }

        private static float RopeY(float t)
        {
            // The same shallow, down-curving cord profile as the existing All Boys footer artwork.
            return 9f + 67f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }

        private static void FillTriangle(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c, Color fill)
        {
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, TextureWidth - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, TextureWidth - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, TextureHeight - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, TextureHeight - 1);
            float height = Mathf.Max(1f, maxY - minY);

            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                var p = new Vector2(x + .5f, y + .5f);
                if (!InsideTriangle(p, a, b, c)) continue;

                float depth = Mathf.Clamp01((y - minY) / height);
                float highlight = Mathf.Lerp(.11f, 0f, depth);
                Color shaded = Color.Lerp(fill, Color.white, highlight);
                SetPixel(pixels, x, y, shaded);
            }
        }

        private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Sign(Vector2 p, Vector2 a, Vector2 b)
        {
            return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        }

        private static void DrawLine(Color32[] pixels, Vector2 a, Vector2 b, Color color, float thickness)
        {
            float distance = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance));
            float radius = Mathf.Max(.5f, thickness * .5f);
            Color32 color32 = color;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(a, b, i / (float)steps);
                int cx = Mathf.RoundToInt(point.x), cy = Mathf.RoundToInt(point.y);
                int extent = Mathf.CeilToInt(radius);
                for (int y = cy - extent; y <= cy + extent; y++)
                for (int x = cx - extent; x <= cx + extent; x++)
                {
                    if (x < 0 || x >= TextureWidth || y < 0 || y >= TextureHeight) continue;
                    float dx = x - point.x, dy = y - point.y;
                    if (dx * dx + dy * dy > radius * radius) continue;
                    BlendPixel(pixels, x, y, color32);
                }
            }
        }

        private static void SetPixel(Color32[] pixels, int x, int yFromTop, Color color)
        {
            if (x < 0 || x >= TextureWidth || yFromTop < 0 || yFromTop >= TextureHeight) return;
            Color32 value = color;
            pixels[(TextureHeight - 1 - yFromTop) * TextureWidth + x] = value;
        }

        private static void BlendPixel(Color32[] pixels, int x, int yFromTop, Color32 source)
        {
            int index = (TextureHeight - 1 - yFromTop) * TextureWidth + x;
            Color32 destination = pixels[index];
            float alpha = source.a / 255f;
            float inverse = 1f - alpha;
            pixels[index] = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.r * alpha + destination.r * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.g * alpha + destination.g * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.b * alpha + destination.b * inverse), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(source.a + destination.a * inverse), 0, 255));
        }
    }
}

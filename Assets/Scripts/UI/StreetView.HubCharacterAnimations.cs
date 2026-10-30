using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayChoriYPaty
{
    public sealed partial class StreetView
    {
        private const string HubCharacterResourceRoot = "StreetHubCharacters/";
        private HubCharacterScene hubCharacterScene;
#if UNITY_EDITOR
        private bool bypassHubCharactersForTesting;
#endif

        private void DrawHubCharacterAnimations()
        {
#if UNITY_EDITOR
            if (bypassHubCharactersForTesting) return;
#endif
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (hubCharacterScene == null) hubCharacterScene = new HubCharacterScene(this);
            hubCharacterScene.Draw();
        }

        partial void ReleaseHubCharacterAnimationsResources();

        partial void ReleaseHubCharacterAnimationsResources()
        {
            if (hubCharacterScene != null) hubCharacterScene.Dispose();
            hubCharacterScene = null;
        }

        // Character art must keep its source-space size beyond the safe canvas. ScaleAndCrop
        // clips it at the physical screen; clamping here would squash feet into the safe area.
        private static Rect HubArtworkRectToLayoutUnclamped(Rect artRect, Vector2 artSize, Vector2 screenSize, Rect safeArea)
        {
            if (artSize.x <= 0f || artSize.y <= 0f || screenSize.x <= 0f || screenSize.y <= 0f) return Rect.zero;
            Rect viewport = CanvasViewport(screenSize, safeArea);
            Rect artwork = HubArtworkScreenBounds(artSize, screenSize);
            if (viewport.width <= 0f || viewport.height <= 0f || artwork.width <= 0f || artwork.height <= 0f) return Rect.zero;
            float artScale = artwork.width / artSize.x;
            Rect screenRect = new Rect(artwork.xMin + artRect.x * artScale,
                artwork.yMin + artRect.y * artScale, artRect.width * artScale, artRect.height * artScale);
            float scaleX = viewport.width / W, scaleY = viewport.height / H;
            return new Rect((screenRect.xMin - viewport.xMin) / scaleX,
                (screenRect.yMin - viewport.yMin) / scaleY,
                screenRect.width / scaleX, screenRect.height / scaleY);
        }

        internal static Rect HubCharacterLayoutBoundsForTesting(Rect artBounds, Vector2 screenSize, Rect safeArea)
        {
            return HubArtworkRectToLayoutUnclamped(artBounds, HubArtworkSize, screenSize, safeArea);
        }

#if UNITY_EDITOR
        private void SetSellerWaveAngleForTesting(float angle)
        {
            if (hubCharacterScene == null) hubCharacterScene = new HubCharacterScene(this);
            hubCharacterScene.SetForcedSellerAngle(angle);
        }

        private void SetHubCharactersBypassedForTesting(bool bypass)
        {
            bypassHubCharactersForTesting = bypass;
        }

        private void SetHubCharacterPoseForTesting(string character, string action, float progress)
        {
            if (hubCharacterScene == null) hubCharacterScene = new HubCharacterScene(this);
            hubCharacterScene.SetForcedPose(character, action, progress);
        }

        private void SetHubCharacterSeedForTesting(int seed)
        {
            if (hubCharacterScene == null) hubCharacterScene = new HubCharacterScene(this);
            hubCharacterScene.SetSeed(seed);
        }

        private void NotifyHubCharacterEventForTesting(string eventName)
        {
            if (hubCharacterScene == null) hubCharacterScene = new HubCharacterScene(this);
            hubCharacterScene.Notify(eventName);
        }
#endif

        [Serializable]
        private sealed class HubCharacterUv
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class HubCharacterPartManifest
        {
            public string name;
            public HubCharacterUv uv;
        }

        [Serializable]
        private sealed class HubCharacterManifest
        {
            public int width;
            public int height;
            public HubCharacterPartManifest[] parts;
        }

        private sealed class HubCharacterGroup
        {
            public readonly string name;
            public readonly Texture2D patch;
            public readonly Texture2D atlas;
            public readonly Rect[] parts;
            private static readonly Dictionary<string, int> NameToIndex = new Dictionary<string, int>();

            public HubCharacterGroup(string name)
            {
                this.name = name;
                patch = Resources.Load<Texture2D>(HubCharacterResourceRoot + name + "/BackgroundPatch");
                atlas = Resources.Load<Texture2D>(HubCharacterResourceRoot + name + "/PartsAtlas");
                TextAsset json = Resources.Load<TextAsset>(HubCharacterResourceRoot + name + "/PartsAtlasBounds");
                if (patch == null || atlas == null || json == null) throw new InvalidOperationException("Missing HUB character resources for " + name);
                HubCharacterManifest manifest = JsonUtility.FromJson<HubCharacterManifest>(json.text);
                // UVs are normalized against the source atlas and remain valid when Unity
                // downsizes/compresses that texture for the active build target.
                if (manifest == null || manifest.width <= 0 || manifest.height <= 0 ||
                    manifest.parts == null || manifest.parts.Length == 0 || atlas.width <= 0 || atlas.height <= 0)
                    throw new InvalidOperationException("Invalid HUB character atlas manifest for " + name +
                        " (manifest=" + (manifest == null ? "null" : manifest.width + "x" + manifest.height + ", parts=" + (manifest.parts == null ? "null" : manifest.parts.Length.ToString())) +
                        "; texture=" + atlas.width + "x" + atlas.height + ")");
                parts = new Rect[manifest.parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    HubCharacterUv item = manifest.parts[i].uv;
                    if (item == null) throw new InvalidOperationException("Missing atlas UV for " + name + "/" + manifest.parts[i].name);
                    Rect uv = new Rect(item.x, item.y, item.width, item.height);
                    if (uv.width <= 0f || uv.height <= 0f || uv.x < 0f || uv.y < 0f || uv.xMax > 1f || uv.yMax > 1f)
                        throw new InvalidOperationException("Out-of-range atlas UV for " + name + "/" + manifest.parts[i].name);
                    parts[i] = uv;
                    NameToIndex[name + ":" + manifest.parts[i].name] = i;
                }
            }

            public int Part(string key)
            {
                if (!NameToIndex.TryGetValue(name + ":" + key, out int index))
                    throw new InvalidOperationException("Missing part " + name + "/" + key);
                return index;
            }
        }

        private sealed class HubCharacterScene
        {
            private readonly StreetView owner;
            private HubCharacterGroup seller, police, store, boy, girl;
            private bool loadAttempted, resourcesValid;
            private HubCharacterBehavior behavior;
            private int lastUpdatedFrame = -1;
#if UNITY_EDITOR
            private float forcedSellerAngle = float.NaN;
            private readonly Dictionary<HubCharacterId, HubCharacterState> forcedStates = new Dictionary<HubCharacterId, HubCharacterState>();
#endif

            public HubCharacterScene(StreetView owner)
            {
                this.owner = owner;
                behavior = NewBehavior(unchecked((int)DateTime.UtcNow.Ticks ^ Environment.TickCount));
            }

            public void Draw()
            {
                if (!loadAttempted) LoadResources();
                if (!resourcesValid) return; // Keep the untouched master rather than expose empty cutout holes.
                if (owner.settingsActive) return;
                AdvanceBehavior();

                // Clean every plate first so no patch can erase another overlapping rig.
                DrawPatch(seller); DrawPatch(police); DrawPatch(store); DrawPatch(boy); DrawPatch(girl);
                DrawPolice();
                DrawSeller();
                DrawStore();
                DrawBoy();
                DrawGirl();
            }

            private void LoadResources()
            {
                loadAttempted = true;
                try
                {
                    seller = new HubCharacterGroup("Seller");
                    police = new HubCharacterGroup("Police");
                    store = new HubCharacterGroup("StoreCharacter");
                    boy = new HubCharacterGroup("Boy");
                    girl = new HubCharacterGroup("Girl");
                    foreach (string part in new[] { "head", "torso", "seated_legs", "arm_left", "thumb_arm", "arm_right", "thumb_alt" }) seller.Part(part);
                    foreach (string part in new[] { "head_left", "head_center", "head_right", "torso_left", "torso_center", "torso_right", "legs_left", "legs_center", "legs_right" }) police.Part(part);
                    foreach (string part in new[] { "head", "torso_bag", "paired_legs", "arm_left", "arm_extended", "arm_right", "arm_down" }) store.Part(part);
                    foreach (string part in new[] { "head", "torso", "shorts", "leg_left", "leg_right", "arm_left", "forearm_down", "arm_right", "arm_raised" }) boy.Part(part);
                    foreach (string part in new[] { "hair", "backpack", "torso", "arm_left", "arm_pointing", "arm_right", "arm_down", "hips", "calves" }) girl.Part(part);
                    resourcesValid = true;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("HUB character replacement disabled; original master remains visible. " + exception.Message);
                    resourcesValid = false;
                }
            }

            private HubCharacterBehavior NewBehavior(int seed) => new HubCharacterBehavior(seed);

            private void AdvanceBehavior()
            {
                if (lastUpdatedFrame == Time.frameCount) return;
                lastUpdatedFrame = Time.frameCount;
                behavior.Tick(Mathf.Clamp(Time.unscaledDeltaTime, 0f, .1f), owner.hubActive && !owner.settingsActive);
            }

            private HubCharacterState State(HubCharacterId id)
            {
#if UNITY_EDITOR
                if (forcedStates.TryGetValue(id, out HubCharacterState forced)) return forced;
#endif
                return behavior.GetState(id);
            }

            private static float Pulse(HubCharacterState s) => s.Envelope;

            private void DrawPatch(HubCharacterGroup group)
            {
                Rect source = PatchSourceBounds(group.name);
                Rect target = ToLayout(source);
                if (target.width <= 0f || target.height <= 0f) return;
                Color previous = GUI.color;
                try { GUI.color = Color.white; GUI.DrawTexture(target, group.patch, ScaleMode.StretchToFill, true); }
                finally { GUI.color = previous; }
            }

            private static Rect PatchSourceBounds(string name)
            {
                switch (name)
                {
                    case "Seller": return new Rect(547f, 959f, 192f, 189f);
                    case "Police": return new Rect(506f, 630f, 230f, 180f);
                    case "StoreCharacter": return new Rect(166f, 1414f, 144f, 251f);
                    case "Boy": return new Rect(449f, 1477f, 183f, 312f);
                    case "Girl": return new Rect(601f, 1477f, 201f, 307f);
                    default: return Rect.zero;
                }
            }

            private Rect ToLayout(Rect source) => owner.LayoutRect(HubArtworkRectToLayoutUnclamped(
                source, HubArtworkSize, new Vector2(Screen.width, Screen.height), Screen.safeArea));

            private void DrawPart(HubCharacterGroup group, int part, Rect sourceRect, float angle = 0f, Vector2? pivot = null)
            {
                Rect bounds = ToLayout(sourceRect);
                if (bounds.width <= 0f || bounds.height <= 0f) return;
                Matrix4x4 before = GUI.matrix;
                Color color = GUI.color;
                try
                {
                    if (Mathf.Abs(angle) > .01f)
                    {
                        Vector2 point = pivot.HasValue ? pivot.Value : sourceRect.center;
                        Vector2 layoutPivot = ToLayout(new Rect(point.x, point.y, 0f, 0f)).position;
                        GUI.matrix = before * Matrix4x4.Translate(layoutPivot) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle)) * Matrix4x4.Translate(-layoutPivot);
                    }
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(bounds, group.atlas, group.parts[part], true);
                }
                finally { GUI.matrix = before; GUI.color = color; }
            }

            private void DrawSeller()
            {
                HubCharacterState state = State(HubCharacterId.Seller); float e = Pulse(state);
                DrawPart(seller, seller.Part("seated_legs"), new Rect(581,1066,125,74), -2f * e, new Vector2(639,1078));
                DrawPart(seller, seller.Part("torso"), new Rect(580,1002,114,91), 1.4f * Mathf.Sin(state.Elapsed * 1.5f));
                DrawPart(seller, seller.Part("head"), new Rect(592,966,74,68), state.Action == HubCharacterAction.Look ? 3.2f * e : 0f, new Vector2(630,1017));
                float armAngle = 0f;
#if UNITY_EDITOR
                if (!float.IsNaN(forcedSellerAngle)) armAngle = forcedSellerAngle;
                else
#endif
                if (state.Action == HubCharacterAction.Wave) armAngle = -32f * e;
                else if (state.Action == HubCharacterAction.Cheer) armAngle = -42f * e;
                if (state.Action == HubCharacterAction.Approve)
                    DrawPart(seller, seller.Part("thumb_arm"), new Rect(652,1017,51,63), -8f * e, new Vector2(662,1031));
                else if (state.Action == HubCharacterAction.Wave || state.Action == HubCharacterAction.Cheer)
                    DrawPart(seller, seller.Part("thumb_alt"), new Rect(650,1015,51,65), armAngle, new Vector2(661,1030));
                else
                    DrawPart(seller, seller.Part("arm_right"), new Rect(651,1020,49,60), armAngle, new Vector2(663,1034));
                DrawPart(seller, seller.Part("arm_left"), new Rect(568,1020,47,60));
            }

            private void DrawPolice()
            {
                HubCharacterId[] ids = { HubCharacterId.PoliceLeft, HubCharacterId.PoliceCenter, HubCharacterId.PoliceRight };
                string[] sides = { "left", "center", "right" };
                Rect[] heads = { new Rect(518,640,32,27), new Rect(604,641,37,30), new Rect(672,654,34,29) };
                Rect[] torsos = { new Rect(512,665,48,44), new Rect(598,670,53,47), new Rect(668,684,50,49) };
                Rect[] legs = { new Rect(512,705,45,53), new Rect(602,714,47,54), new Rect(671,731,45,54) };
                for (int i = 0; i < ids.Length; i++)
                {
                    HubCharacterState state = State(ids[i]); float e = Pulse(state);
                    DrawPart(police, police.Part("legs_" + sides[i]), legs[i], state.Action == HubCharacterAction.Shift ? (i == 1 ? 2f : -2f) * e : 0f);
                    Rect torso = torsos[i]; torso.y += state.Action == HubCharacterAction.Shift ? -1.5f * e : 0f;
                    DrawPart(police, police.Part("torso_" + sides[i]), torso);
                    DrawPart(police, police.Part("head_" + sides[i]), heads[i], state.Action == HubCharacterAction.Look ? 3f * e : 0f);
                }
            }

            private void DrawStore()
            {
                HubCharacterState state = State(HubCharacterId.Store); float e = Pulse(state);
                Rect legs = new Rect(205,1552,76,81); if (state.Action == HubCharacterAction.Step) legs.y -= 2f * e;
                DrawPart(store, store.Part("paired_legs"), legs);
                DrawPart(store, store.Part("torso_bag"), new Rect(201,1475,80,86), state.Action == HubCharacterAction.Interact ? -1.5f * e : 0f);
                DrawPart(store, store.Part("head"), new Rect(205,1419,75,72), state.Action == HubCharacterAction.Look ? 4f * e : 0f, new Vector2(241,1475));
                DrawPart(store, store.Part("arm_down"), new Rect(186,1485,30,55), state.Action == HubCharacterAction.Interact ? 22f * e : 0f, new Vector2(208,1495));
                DrawPart(store, store.Part("arm_right"), new Rect(265,1480,26,52));
            }

            private void DrawBoy()
            {
                HubCharacterState state = State(HubCharacterId.Boy); float e = Pulse(state);
                float step = state.Action == HubCharacterAction.Walk ? Mathf.Sin(state.Progress01 * Mathf.PI * 4f) * 3f * e : 0f;
                DrawPart(boy, boy.Part("leg_left"), new Rect(467,1650 + step,45,88));
                DrawPart(boy, boy.Part("leg_right"), new Rect(520,1650 - step,43,88));
                DrawPart(boy, boy.Part("shorts"), new Rect(475,1623,77,55));
                DrawPart(boy, boy.Part("torso"), new Rect(474,1551,84,87), state.Action == HubCharacterAction.Cheer ? -3f * e : 0f);
                DrawPart(boy, boy.Part("head"), new Rect(481,1497,74,68), state.Action == HubCharacterAction.Look ? 3f * e : 0f, new Vector2(516,1552));
                DrawPart(boy, boy.Part("arm_left"), new Rect(460,1556,39,70), state.Action == HubCharacterAction.Cheer ? -16f * e : 0f, new Vector2(489,1568));
                if (state.Action == HubCharacterAction.Cheer)
                    // Keep the original striped sleeve attached to the cheering arm.
                    DrawPart(boy, boy.Part("arm_right"), new Rect(548,1542,40,66), -58f * e, new Vector2(554,1567));
                else
                    DrawPart(boy, boy.Part("arm_right"), new Rect(548,1557,40,66));
            }

            private void DrawGirl()
            {
                HubCharacterState state = State(HubCharacterId.Girl); float e = Pulse(state);
                float step = state.Action == HubCharacterAction.Walk ? Mathf.Sin(state.Progress01 * Mathf.PI * 4f + Mathf.PI) * 3f * e : 0f;
                DrawPart(girl, girl.Part("calves"), new Rect(685,1637 + step,72,100));
                DrawPart(girl, girl.Part("hips"), new Rect(672,1599,76,71));
                DrawPart(girl, girl.Part("backpack"), new Rect(682,1542,59,77));
                DrawPart(girl, girl.Part("torso"), new Rect(675,1539,79,85));
                // Hair sits behind the arm rig; drawing it last masks the pointing hand.
                DrawPart(girl, girl.Part("hair"), new Rect(664,1493,94,88), state.Action == HubCharacterAction.Look ? -3.2f * e : 1.2f * Mathf.Sin(state.Elapsed * 3f));
                if (state.Action == HubCharacterAction.Cheer)
                    DrawPart(girl, girl.Part("arm_pointing"), new Rect(729,1538,65,83), -8f * e, new Vector2(746,1555));
                else
                    DrawPart(girl, girl.Part("arm_right"), new Rect(730,1548,48,72), state.Action == HubCharacterAction.Walk ? 7f * Mathf.Sin(state.Progress01 * Mathf.PI * 4f) * e : 0f, new Vector2(746,1560));
                DrawPart(girl, girl.Part("arm_left"), new Rect(653,1550,40,72), state.Action == HubCharacterAction.Walk ? -7f * Mathf.Sin(state.Progress01 * Mathf.PI * 4f) * e : 0f, new Vector2(683,1560));
            }

#if UNITY_EDITOR
            public void SetForcedSellerAngle(float angle) => forcedSellerAngle = angle;

            public void SetForcedPose(string name, string action, float progress)
            {
                if (!Enum.TryParse(name, true, out HubCharacterId id) || !Enum.TryParse(action, true, out HubCharacterAction pose)) return;
                forcedStates[id] = new HubCharacterState { Action = pose, Progress01 = Mathf.Clamp01(progress), Envelope = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(progress)), 2f), Elapsed = Mathf.Clamp01(progress) };
            }

            public void SetSeed(int seed) { behavior = NewBehavior(seed); forcedStates.Clear(); }
            public void Notify(string eventName)
            {
                if (Enum.TryParse(eventName, true, out HubCharacterEvent hubEvent)) behavior.Notify(hubEvent);
            }
#endif

            public void Dispose()
            {
                // Resources.Load textures are shared imported assets; drop references without destroying them.
                seller = police = store = boy = girl = null;
                behavior = null;
            }
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace HayChoriYPaty.Tests
{
    /// <summary>
    /// Pins the Editor Game view to a fixed portrait resolution for UI tests that inject pointer input
    /// or compare captures, and restores the previous selection afterwards. The game itself must run at any
    /// resolution; this only makes the test environment deterministic regardless of how the Editor was opened.
    /// </summary>
    internal sealed class PortraitGameView
    {
        // Deliberately smaller than the 1080x1920 reference so tests also cover a non-reference portrait size.
        public const int DefaultWidth = 720, DefaultHeight = 1280;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        readonly int width, height;
        UnityEditor.EditorWindow window;
        PropertyInfo selectedSize;
        object sizeGroup;
        int previousSize = -1, addedSize = -1;

        public int Width => width;
        public int Height => height;

        public PortraitGameView(int width = DefaultWidth, int height = DefaultHeight)
        {
            this.width = width;
            this.height = height;
        }

        public void Begin()
        {
            Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
            window = UnityEditor.EditorWindow.GetWindow(editor.GetType("UnityEditor.GameView"));
            selectedSize = window.GetType().GetProperty("selectedSizeIndex", Flags);
            previousSize = (int)selectedSize.GetValue(window);
            Type registryType = editor.GetType("UnityEditor.GameViewSizes");
            object registry = registryType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy).GetValue(null);
            object groupKind = registryType.GetProperty("currentGroupType", Flags).GetValue(registry);
            sizeGroup = registryType.GetMethod("GetGroup", Flags).Invoke(registry, new[] { groupKind });
            Type kind = editor.GetType("UnityEditor.GameViewSizeType");
            object size = Activator.CreateInstance(editor.GetType("UnityEditor.GameViewSize"),
                new object[] { Enum.Parse(kind, "FixedResolution"), width, height, "Temporary test portrait " + width + "x" + height });
            sizeGroup.GetType().GetMethod("AddCustomSize", Flags).Invoke(sizeGroup, new[] { size });
            addedSize = (int)sizeGroup.GetType().GetMethod("GetTotalCount", Flags).Invoke(sizeGroup, null) - 1;
            selectedSize.SetValue(window, addedSize);
            window.Focus();
            window.Repaint();
        }

        /// <summary>Waits (bounded) until the Game view reports the requested resolution.</summary>
        public IEnumerator WaitUntilApplied(float timeoutSeconds = 3f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            do yield return null;
            while ((Screen.width != width || Screen.height != height) && Time.realtimeSinceStartup < deadline);
        }

        public void End()
        {
            if (window == null || selectedSize == null) return;
            if (previousSize >= 0) selectedSize.SetValue(window, previousSize);
            if (addedSize >= 0 && sizeGroup != null)
                sizeGroup.GetType().GetMethod("RemoveCustomSize", Flags)?.Invoke(sizeGroup, new object[] { addedSize });
            window.Repaint();
            window = null;
        }
    }
}
#endif

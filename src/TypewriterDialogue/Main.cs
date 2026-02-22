using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace TypewriterDialogue
{
    public static class Main
    {
        // ── UMM references ──────────────────────────────────────────────
        private static UnityModManager.ModEntry _mod;

        // ── Reflection cache ────────────────────────────────────────────
        internal static Type TmpTextType;
        internal static PropertyInfo TextProp;
        internal static PropertyInfo MaxVisibleCharsProp;

        // ── Phase A state ───────────────────────────────────────────────
        private static float _scanTimer;
        private static readonly Dictionary<string, string> _lastLoggedText =
            new Dictionary<string, string>();

        // ================================================================
        //  Entry point (called by UMM)
        // ================================================================
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            _mod = modEntry;

            try
            {
                InitReflection();

                modEntry.OnUpdate = OnUpdate;
                modEntry.OnGUI = OnGUI;

                // Initialize Phase B controller
                TypewriterController.Initialize(_mod, TmpTextType, TextProp, MaxVisibleCharsProp);

                _mod.Logger.Log("[TypewriterDialogue] Loaded OK. DebugMode=" + Config.DebugMode);
                return true;
            }
            catch (Exception ex)
            {
                modEntry.Logger.LogException("[TypewriterDialogue] Load FAILED", ex);
                return false;
            }
        }

        // ================================================================
        //  Reflection bootstrap
        // ================================================================
        private static void InitReflection()
        {
            // Find the TMPro assembly among all currently loaded assemblies
            Assembly tmpAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Unity.TextMeshPro");

            if (tmpAssembly == null)
                throw new Exception("Assembly 'Unity.TextMeshPro' not loaded. " +
                    "Make sure the game has finished initializing.");

            TmpTextType = tmpAssembly.GetType("TMPro.TextMeshProUGUI");
            if (TmpTextType == null)
                throw new Exception("Type TMPro.TextMeshProUGUI not found in assembly.");

            TextProp = TmpTextType.GetProperty("text",
                BindingFlags.Public | BindingFlags.Instance);

            MaxVisibleCharsProp = TmpTextType.GetProperty("maxVisibleCharacters",
                BindingFlags.Public | BindingFlags.Instance);

            if (TextProp == null)
                throw new Exception("Property 'text' not found on TextMeshProUGUI.");

            if (MaxVisibleCharsProp == null)
                throw new Exception("Property 'maxVisibleCharacters' not found on TextMeshProUGUI.");

            _mod.Logger.Log("[TypewriterDialogue] Reflection OK — " +
                "TMPro type and properties cached.");
        }

        // ================================================================
        //  Per-frame update
        // ================================================================
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            try
            {
                // Phase A: periodic scan
                if (Config.DebugMode)
                {
                    _scanTimer += deltaTime;
                    if (_scanTimer >= Config.ScanInterval)
                    {
                        _scanTimer = 0f;
                        ScanTextComponents();
                    }
                }

                // Phase B: typewriter (runs every frame)
                TypewriterController.Update();
            }
            catch (Exception ex)
            {
                modEntry.Logger.LogException("[TypewriterDialogue] OnUpdate error", ex);
            }
        }

        // ================================================================
        //  Phase A — scan all active TMPro text components
        // ================================================================
        private static void ScanTextComponents()
        {
            UnityEngine.Object[] components;
            try
            {
                components = UnityEngine.Object.FindObjectsOfType(TmpTextType);
            }
            catch (Exception ex)
            {
                _mod.Logger.LogException("[PhaseA] FindObjectsOfType failed", ex);
                return;
            }

            foreach (var comp in components)
            {
                try
                {
                    var behaviour = comp as MonoBehaviour;
                    if (behaviour == null) continue;
                    if (!behaviour.gameObject.activeInHierarchy) continue;

                    var text = TextProp.GetValue(behaviour, null) as string;
                    if (string.IsNullOrEmpty(text)) continue;
                    if (text.Length <= Config.MinTextLen) continue;

                    string path = GetGameObjectPath(behaviour.gameObject);

                    // De-duplicate: key includes InstanceID so clones with
                    // identical paths (e.g. LocationName(Clone)/Label) are
                    // tracked individually and don't spam the log.
                    string dedupKey = path + "|" + behaviour.GetInstanceID();
                    string prev;
                    if (_lastLoggedText.TryGetValue(dedupKey, out prev) && prev == text)
                        continue;

                    _lastLoggedText[dedupKey] = text;

                    _mod.Logger.Log(string.Format(
                        "[PhaseA] len={0}, name={1}, path={2}",
                        text.Length,
                        behaviour.gameObject.name,
                        path));
                }
                catch
                {
                    // Swallow per-component errors to keep scanning
                }
            }
        }

        // ================================================================
        //  UMM GUI — settings panel
        // ================================================================
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginVertical("box");

            // Debug toggle
            Config.DebugMode = GUILayout.Toggle(Config.DebugMode,
                " Phase A — Debug / Discovery logging");

            // Target path
            GUILayout.Label("Target path filter (Phase B, empty = disabled):");
            Config.TargetPathContains = GUILayout.TextField(
                Config.TargetPathContains, GUILayout.Width(500));

            // Speed slider
            GUILayout.Label(string.Format(
                "Typewriter speed: {0:F0} chars/sec", Config.CharsPerSecond));
            Config.CharsPerSecond = GUILayout.HorizontalSlider(
                Config.CharsPerSecond, 10f, 200f, GUILayout.Width(300));

            GUILayout.EndVertical();
        }

        // ================================================================
        //  Utility
        // ================================================================
        internal static string GetGameObjectPath(GameObject obj)
        {
            var parts = new List<string>();
            var current = obj.transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}

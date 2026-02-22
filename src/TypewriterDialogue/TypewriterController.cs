using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace TypewriterDialogue
{
    /// <summary>
    /// Phase B — typewriter character-reveal effect.
    /// Caches a single target TMPro component, polls for text changes,
    /// and animates maxVisibleCharacters.
    /// </summary>
    public static class TypewriterController
    {
        // ── Dependencies (set once from Main.Load) ──────────────────────
        private static UnityModManager.ModEntry _mod;
        private static Type _tmpTextType;
        private static PropertyInfo _textProp;
        private static PropertyInfo _maxVisProp;

        // ── Target tracking ─────────────────────────────────────────────
        private static MonoBehaviour _target;
        private static string _lastText = "";
        private static float _charIndex;
        private static bool _animating;
        private static int _totalChars;

        // ── Cache invalidation ──────────────────────────────────────────
        private static int _lastSceneIndex = -1;
        private static float _rescanCooldown;
        private const float RescanInterval = 0.5f; // don't rescan more than 2x/sec

        // ================================================================
        public static void Initialize(
            UnityModManager.ModEntry mod,
            Type tmpTextType,
            PropertyInfo textProp,
            PropertyInfo maxVisProp)
        {
            _mod = mod;
            _tmpTextType = tmpTextType;
            _textProp = textProp;
            _maxVisProp = maxVisProp;
        }

        // ================================================================
        //  Called every frame from Main.OnUpdate
        // ================================================================
        public static void Update()
        {
            // Fail-safe: Phase B is disabled when no target path is configured
            if (string.IsNullOrEmpty(Config.TargetPathContains))
                return;

            try
            {
                float dt = Time.unscaledDeltaTime;

                DetectSceneChange();
                EnsureTarget(dt);

                if (_target != null)
                {
                    PollTextChange();
                }

                if (_animating)
                {
                    if (CheckSkipInput())
                    {
                        FinishImmediately();
                    }
                    else
                    {
                        Animate(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                _mod.Logger.LogException("[PhaseB] Update error", ex);
            }
        }

        // ================================================================
        //  Scene-change detection → invalidate cache
        // ================================================================
        private static void DetectSceneChange()
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx != _lastSceneIndex)
            {
                _lastSceneIndex = idx;
                InvalidateTarget("scene changed");
            }
        }

        // ================================================================
        //  Ensure we have a valid cached target
        // ================================================================
        private static void EnsureTarget(float dt)
        {
            // Already have a valid target?
            if (_target != null && IsTargetValid())
                return;

            // Rate-limit rescans
            _rescanCooldown -= dt;
            if (_rescanCooldown > 0f)
                return;
            _rescanCooldown = RescanInterval;

            FindTarget();
        }

        private static bool IsTargetValid()
        {
            try
            {
                if (_target == null) return false;
                // Unity overloads == for destroyed objects; also check native side
                if (!_target) return false;
                if (!_target.gameObject.activeInHierarchy) return false;

                // Quick reflection probe — if it throws, object is gone
                _textProp.GetValue(_target, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void FindTarget()
        {
            try
            {
                var components = UnityEngine.Object.FindObjectsOfType(_tmpTextType);

                foreach (var comp in components)
                {
                    var behaviour = comp as MonoBehaviour;
                    if (behaviour == null) continue;
                    if (!behaviour.gameObject.activeInHierarchy) continue;

                    string path = Main.GetGameObjectPath(behaviour.gameObject);
                    if (!path.Contains(Config.TargetPathContains)) continue;

                    _target = behaviour;
                    _lastText = GetText() ?? "";
                    _animating = false;

                    _mod.Logger.Log("[PhaseB] Target acquired: " + path);
                    return;
                }
            }
            catch (Exception ex)
            {
                _mod.Logger.LogException("[PhaseB] FindTarget error", ex);
            }
        }

        // ================================================================
        //  Poll for text changes
        // ================================================================
        private static void PollTextChange()
        {
            try
            {
                string current = GetText();
                if (current == null) return;

                if (current != _lastText)
                {
                    _lastText = current;
                    _totalChars = current.Length;

                    if (_totalChars > 0)
                    {
                        // Start typewriter animation
                        _charIndex = 0f;
                        _animating = true;
                        SetMaxVisibleChars(0);

                        if (Config.DebugMode)
                        {
                            _mod.Logger.Log(string.Format(
                                "[PhaseB] New text, starting typewriter: len={0}",
                                _totalChars));
                        }
                    }
                    else
                    {
                        _animating = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _mod.Logger.LogException("[PhaseB] PollTextChange error", ex);
            }
        }

        // ================================================================
        //  Animation
        // ================================================================
        private static void Animate(float dt)
        {
            try
            {
                _charIndex += Config.CharsPerSecond * dt;
                int visible = Mathf.FloorToInt(_charIndex);

                if (visible >= _totalChars)
                {
                    FinishImmediately();
                    return;
                }

                SetMaxVisibleChars(visible);
            }
            catch (Exception ex)
            {
                _mod.Logger.LogException("[PhaseB] Animate error", ex);
                _animating = false;
            }
        }

        private static void FinishImmediately()
        {
            _animating = false;
            SetMaxVisibleChars(99999); // TMPro treats large values as "show all"

            if (Config.DebugMode)
                _mod.Logger.Log("[PhaseB] Reveal complete (or skipped).");
        }

        // ================================================================
        //  Skip input
        // ================================================================
        private static bool CheckSkipInput()
        {
            try
            {
                return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            }
            catch
            {
                return false;
            }
        }

        // ================================================================
        //  Reflection helpers
        // ================================================================
        private static string GetText()
        {
            try
            {
                return _textProp.GetValue(_target, null) as string;
            }
            catch
            {
                InvalidateTarget("text read failed");
                return null;
            }
        }

        private static void SetMaxVisibleChars(int count)
        {
            try
            {
                _maxVisProp.SetValue(_target, count, null);
            }
            catch
            {
                InvalidateTarget("maxVisibleCharacters write failed");
            }
        }

        private static void InvalidateTarget(string reason)
        {
            if (_target != null && Config.DebugMode)
                _mod.Logger.Log("[PhaseB] Target invalidated: " + reason);

            _target = null;
            _animating = false;
            _lastText = "";
        }
    }
}

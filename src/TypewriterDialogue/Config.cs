namespace TypewriterDialogue
{
    /// <summary>
    /// Runtime-editable configuration. Changed via UMM GUI (Ctrl+F10) or by editing defaults here.
    /// </summary>
    public static class Config
    {
        // ── Phase A (Discovery) ─────────────────────────────────────────

        /// <summary>Enable Phase A scanning and logging.</summary>
        public static bool DebugMode = false;

        /// <summary>Seconds between Phase A scans. 0.333 = 3 scans/sec.</summary>
        public static float ScanInterval = 0.333f;

        /// <summary>Ignore text components with fewer characters than this.</summary>
        public static int MinTextLen = 20;

        // ── Phase B (Typewriter) ────────────────────────────────────────

        /// <summary>
        /// Typewriter reveal speed in characters per second.
        /// </summary>
        public static float CharsPerSecond = 45f;

        /// <summary>
        /// Substring that must appear in the GameObject hierarchy path for
        /// Phase B to target a component. If empty, Phase B does nothing (fail-safe).
        /// Set this to a value found in Phase A logs, e.g. "DialogBox/AnswerText".
        /// </summary>
        public static string TargetPathContains = "DialogPCView/Body/View/Scroll View/Viewport/Content/CueView/Text";
    }
}

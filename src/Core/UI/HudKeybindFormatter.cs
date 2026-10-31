using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using MasterBuilderHeeler.Configuration;
using MasterBuilderHeeler.Utilities;

namespace MasterBuilderHeeler.Core.UI
{
    public static class HudKeybindFormatter
    {
        private static string? _cachedForwardNudgeLabel;
        private static string? _cachedSideNudgeLabel;
        private static string? _cachedVerticalNudgeLabel;

        public static int KeybindVersion { get; private set; }

        public static void Initialize()
        {
            Cleanup();
            IReadOnlyList<ConfigEntry<KeyboardShortcut>> shortcuts = KeyboardMouseConfig.AllShortcuts;
            for (int index = 0; index < shortcuts.Count; index++)
            {
                shortcuts[index].SettingChanged += OnShortcutSettingChanged;
            }
            HudTheme.ThemeChanged += ClearFormattedNudgeLabels;
            InvalidateCache();
        }

        public static void Cleanup()
        {
            HudTheme.ThemeChanged -= ClearFormattedNudgeLabels;
            IReadOnlyList<ConfigEntry<KeyboardShortcut>> shortcuts = KeyboardMouseConfig.AllShortcuts;
            for (int index = 0; index < shortcuts.Count; index++)
            {
                shortcuts[index].SettingChanged -= OnShortcutSettingChanged;
            }
        }

        public static string FormatForwardNudge()
        {
            if (_cachedForwardNudgeLabel == null)
            {
                _cachedForwardNudgeLabel = FormatNudgePair(
                    KeyboardMouseConfig.NudgeForwardKey,
                    HudGlyphs.TriangleUpSolid,
                    KeyboardMouseConfig.NudgeBackwardKey,
                    HudGlyphs.TriangleDownSolid);
            }
            return _cachedForwardNudgeLabel;
        }

        public static string FormatSideNudge()
        {
            if (_cachedSideNudgeLabel == null)
            {
                _cachedSideNudgeLabel = FormatNudgePair(
                    KeyboardMouseConfig.NudgeLeftKey,
                    HudGlyphs.TriangleLeftSolid,
                    KeyboardMouseConfig.NudgeRightKey,
                    HudGlyphs.TriangleRightSolid);
            }
            return _cachedSideNudgeLabel;
        }

        public static string FormatVerticalNudge()
        {
            if (_cachedVerticalNudgeLabel == null)
            {
                _cachedVerticalNudgeLabel = FormatNudgePair(
                    KeyboardMouseConfig.NudgeUpKey,
                    HudGlyphs.TriangleUpHollow,
                    KeyboardMouseConfig.NudgeDownKey,
                    HudGlyphs.TriangleDownHollow);
            }
            return _cachedVerticalNudgeLabel;
        }

        private static string FormatNudgePair(
            ConfigEntry<KeyboardShortcut> firstKey,
            string firstGlyph,
            ConfigEntry<KeyboardShortcut> secondKey,
            string secondGlyph) =>
            $"{KeyLabelFormatter.Format(firstKey)} {firstGlyph}{HudTheme.PipeSeparator}{KeyLabelFormatter.Format(secondKey)} {secondGlyph}";

        private static void OnShortcutSettingChanged(object sender, EventArgs eventArgs) => InvalidateCache();

        private static void ClearFormattedNudgeLabels()
        {
            _cachedForwardNudgeLabel = null;
            _cachedSideNudgeLabel = null;
            _cachedVerticalNudgeLabel = null;
        }

        private static void InvalidateCache()
        {
            KeyLabelFormatter.ClearCache();
            ClearFormattedNudgeLabels();
            KeybindVersion++;
        }
    }
}

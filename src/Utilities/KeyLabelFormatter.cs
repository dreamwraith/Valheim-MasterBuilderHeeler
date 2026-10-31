using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Utilities
{
    public static class KeyLabelFormatter
    {
        private static readonly StringBuilder SharedBuilder = new StringBuilder(64);
        private static readonly Dictionary<ConfigEntryBase, string> LabelCache = new Dictionary<ConfigEntryBase, string>(32);

        public static void ClearCache() => LabelCache.Clear();

        public static string Format(ConfigEntry<KeyboardShortcut> entry)
        {
            if (LabelCache.TryGetValue(entry, out string cachedLabel))
            {
                return cachedLabel;
            }

            string formatted = Format(entry.Value);
            LabelCache[entry] = formatted;
            return formatted;
        }

        public static string Format(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
            {
                return "Unbound";
            }

            SharedBuilder.Clear();
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (modifier != KeyCode.None)
                {
                    if (SharedBuilder.Length > 0)
                    {
                        SharedBuilder.Append('+');
                    }
                    SharedBuilder.Append(FormatKeyCode(modifier));
                }
            }

            if (SharedBuilder.Length > 0)
            {
                SharedBuilder.Append('+');
            }
            SharedBuilder.Append(FormatKeyCode(shortcut.MainKey));

            return SharedBuilder.ToString();
        }

        public static string FormatKeyCode(KeyCode keyCode) =>
            GetKeyDisplayName(keyCode).Replace("Page ", "Pg");

        private static string GetKeyDisplayName(KeyCode keyCode)
        {
            // KeyCodeToDisplayName gives back "$KeyCode some random bullshit goes here" for unmapped keys and theres no localization for those, cool
            string displayName = ZInput.KeyCodeToDisplayName(keyCode);
            if (string.IsNullOrEmpty(displayName) || displayName.StartsWith("$KeyCode", StringComparison.Ordinal))
            {
                return keyCode.ToString();
            }

            if (displayName[0] == '$')
            {
                return Localization.instance.Localize(displayName);
            }

            return displayName;
        }
    }
}

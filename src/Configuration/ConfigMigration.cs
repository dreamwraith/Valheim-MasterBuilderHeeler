using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using UnityEngine;

namespace MasterBuilderHeeler.Configuration
{
    // one-time migration for a config saved by 1.2.1 or older. jotunn puts the number of a section into its name,
    // so after 2.0 moved and renamed sections the saved values sit under names nothing binds any more
    // same trick as MiniMapHeeler. drop this in a later version
    public static class ConfigMigration
    {
        private const string NotSaved = "<not saved>";
        private const string BackupSuffix = ".pre-2.0.bak";

        // section name without its number, to the full names its settings were saved under before 2.0
        // a section that kept its name and its number isnt in here. gizmo settings has two because it took the gizmo
        // colors section and the three gizmo settings that were in general
        private static readonly Dictionary<string, string[]> LegacySections = new Dictionary<string, string[]>(12, StringComparer.Ordinal)
        {
            ["Gizmo Settings"] = new[] { "13 - Gizmo Colors", "1 - General" },
            ["Grid Settings"] = new[] { "3 - Grid Settings" },
            ["Build Camera"] = new[] { "4 - Build Camera" },
            ["Build Camera Light"] = new[] { "5 - Build Camera Light" },
            ["Build Camera Extras"] = new[] { "6 - Build Camera Demister & AutoPickup" },
            ["HUD & Info Panels"] = new[] { "7 - HUD & Info Panels" },
            ["Global Panel"] = new[] { "8 - Global Panel" },
            ["Precision Panel"] = new[] { "9 - Precision Panel" },
            ["Hover Panel"] = new[] { "10 - Hover Panel" },
            ["Placement Panel"] = new[] { "11 - Placement Panel" },
            ["Keyboard & Mouse Bindings"] = new[] { "12 - Keybindings (Keyboard & Mouse)" },
            ["HUD Colors"] = new[] { "14 - HUD Colors" }
        };

        // the 1.2.1 default of every setting that got a new default in 2.0. the file saves a setting even when nobody
        // touched it, so a saved value thats still the old default was never picked and gets dropped for the new one
        private static readonly Dictionary<string, object> LegacyDefaults = new Dictionary<string, object>(9, StringComparer.Ordinal)
        {
            ["GizmoRendererStyle"] = GizmoStyle.ProceduralWireframe,
            ["GizmoPitchRingColor"] = new Color(1.0f, 0.25f, 0.25f, 1.0f),
            ["GizmoYawRingColor"] = new Color(0.25f, 1.0f, 0.25f, 1.0f),
            ["GizmoRollRingColor"] = new Color(0.25f, 0.65f, 1.0f, 1.0f),
            ["GizmoRightArrowColor"] = new Color(1.0f, 0.35f, 0.35f, 0.85f),
            ["GizmoUpArrowColor"] = new Color(0.35f, 1.0f, 0.35f, 0.85f),
            ["GizmoForwardArrowColor"] = new Color(0.35f, 0.75f, 1.0f, 0.85f),
            ["TerrainGridHeight"] = 0.075f,
            ["WorldFlatGridHeight"] = 0.02f
        };

        // goes over the bound settings so it has to run after the last bind. ModConfig saves the file when its done
        public static void MigrateMovedSections(ConfigFile config)
        {
            int legacyCount = 0;
            int migratedCount = 0;
            foreach (ConfigDefinition definition in config.Keys)
            {
                int separatorIndex = definition.Section.IndexOf(" - ", StringComparison.Ordinal);
                if (separatorIndex < 0 ||
                    !LegacySections.TryGetValue(definition.Section.Substring(separatorIndex + 3), out string[] legacySections) ||
                    !TryReadSavedValue(config, legacySections, definition.Key, out string savedValue))
                {
                    continue;
                }

                legacyCount++;
                ConfigEntryBase entry = config[definition];
                if (IsLegacyDefault(entry, savedValue))
                {
                    continue;
                }

                entry.SetSerializedValue(savedValue);
                migratedCount++;
            }

            if (legacyCount == 0)
            {
                return;
            }

            DropLeftoverValues(config);
            Plugin.LogInfo($"[ConfigMigration] Moved {migratedCount} saved settings over from the 1.2.1 section names. The old file is saved as {Path.GetFileName(config.ConfigFilePath)}{BackupSuffix}.");
        }

        // bepinex writes back every saved value that no setting claimed, so dead keys from older versions never leave
        // the file. reloading forgets them, empty file gives it nothing to read back, save in ModConfig
        // writes only new stuff as a result. backup kept just in case
        private static void DropLeftoverValues(ConfigFile config)
        {
            File.Copy(config.ConfigFilePath, config.ConfigFilePath + BackupSuffix, overwrite: true);
            File.WriteAllText(config.ConfigFilePath, string.Empty);
            config.Reload();
        }

        // a color comes back from the file rounded to whole bytes and wouldnt equal the default as a value, so this
        // compares the text bepinex wrote for that default back then
        private static bool IsLegacyDefault(ConfigEntryBase entry, string savedValue) =>
            LegacyDefaults.TryGetValue(entry.Definition.Key, out object legacyDefault) &&
            string.Equals(savedValue, TomlTypeConverter.ConvertToString(legacyDefault, entry.SettingType), StringComparison.Ordinal);

        // the config file hands a saved value it has no setting for to whatever binds that name next, so binding
        // the old name as text is how the old value comes back out
        // the default tells an old value that was saved empty (an unbound key) from no old value at all
        private static bool TryReadSavedValue(ConfigFile config, string[] legacySections, string key, out string savedValue)
        {
            for (int index = 0; index < legacySections.Length; index++)
            {
                ConfigDefinition legacyDefinition = new ConfigDefinition(legacySections[index], key);
                savedValue = config.Bind(legacyDefinition, NotSaved).Value;
                config.Remove(legacyDefinition);
                if (!string.Equals(savedValue, NotSaved, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            savedValue = NotSaved;
            return false;
        }
    }
}

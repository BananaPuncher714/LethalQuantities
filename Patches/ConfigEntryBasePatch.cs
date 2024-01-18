using BepInEx.Configuration;
using HarmonyLib;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace LethalQuantities.Patches
{

    internal class ConfigEntryBasePatch
    {
        public static ConditionalWeakTable<ConfigEntryBase, BaseEntry> entries { get; } = new ConditionalWeakTable<ConfigEntryBase, BaseEntry>();

        [HarmonyPatch(typeof(ConfigEntryBase), nameof(ConfigEntryBase.WriteDescription))]
        [HarmonyPrefix]
        private static bool onWriteDescriptionPrefix(ConfigEntryBase __instance, ref StreamWriter writer)
        {
            // Check if the instance is a custom managed entry that needs custom descriptions to be written
            BaseEntry entry;
            if (entries.TryGetValue(__instance, out entry))
            {
                if (!string.IsNullOrEmpty(__instance.Description.Description))
                {
                    writer.WriteLine($"## {__instance.Description.Description.Replace("\n", "\n## ")}");
                }
                else
                {
                    writer.WriteLine($"## No description: {__instance.Description.Description.Replace("\n", "\n## ")}");
                }

                writer.WriteLine($"# Type of setting: {entry.SettingType().Name}");

                if (entry.hasDefault())
                {
                    writer.WriteLine($"# Default value: {entry.DefaultString()}");
                }

                if (__instance.Description.AcceptableValues != null)
                {
                    writer.WriteLine(__instance.Description.AcceptableValues.ToDescriptionString());
                }
                else if (entry.SettingType().IsEnum)
                {
                    writer.WriteLine($"# Acceptable values: {string.Join(", ", Enum.GetNames(entry.SettingType()))}");

                    if (entry.SettingType().GetCustomAttributes(typeof(FlagsAttribute), true).Any())
                        writer.WriteLine("# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)");
                }

                return false;
            }
            return true;
        }
    }
}

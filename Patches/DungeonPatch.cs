using DunGen;
using HarmonyLib;
using LethalQuantities.Json;
using LethalQuantities.Objects;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class DungeonPatch
    {
        [HarmonyPatch(typeof(RuntimeDungeon), nameof(RuntimeDungeon.Generate))]
        [HarmonyPriority(300)]
        [HarmonyPrefix]
        static void onDungeonGenerate(RuntimeDungeon __instance)
        {
            RoundState state = Plugin.getRoundState(RoundManager.Instance.currentLevel);
            if (state != null)
            {
                string name = __instance.Generator.DungeonFlow.name;
                LevelPreset preset = state.getPreset();
                if (preset.dungeonFlows.isSet())
                {
                    if (preset.dungeonFlows.value.TryGetValue(name, out LevelPresetDungeonFlow flowPreset))
                    {
                        flowPreset.factorySizeMultiplier.update(ref RoundManager.Instance.mapSizeMultiplier);

                        // Must be the same across all players to avoid desync
                        __instance.Generator.LengthMultiplier = RoundManager.Instance.mapSizeMultiplier * RoundManager.Instance.currentLevel.factorySizeMultiplier;

                        MiniLogger.LogInfo($"Found dungeon flow {name}, using a length multiplier of {__instance.Generator.LengthMultiplier}");
                    }
                }
            }
        }
    }
}

using DunGen;
using HarmonyLib;
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
            RoundState state = getRoundState();
            if (state != null)
            {
                if (state.levelConfiguration.dungeon.enabled.Value)
                {
                    string name = __instance.Generator.DungeonFlow.name;
                    if (state.levelConfiguration.dungeon.dungeonFlowConfigurations.TryGetValue(name, out DungeonFlowConfiguration config))
                    {
                        config.factorySizeMultiplier.Set(ref RoundManager.Instance.currentLevel.factorySizeMultiplier);

                        __instance.Generator.LengthMultiplier = RoundManager.Instance.mapSizeMultiplier * RoundManager.Instance.currentLevel.factorySizeMultiplier;
                    }
                }
                else if (state.globalConfiguration.dungeonConfiguration.enabled.Value && !state.globalConfiguration.dungeonConfiguration.isDefault())
                {
                    string name = __instance.Generator.DungeonFlow.name;
                    if (state.globalConfiguration.dungeonConfiguration.dungeonFlowConfigurations.TryGetValue(name, out GlobalDungeonFlowConfiguration config))
                    {
                        config.factorySizeMultiplier.Set(ref RoundManager.Instance.currentLevel.factorySizeMultiplier);

                        __instance.Generator.LengthMultiplier = RoundManager.Instance.mapSizeMultiplier * RoundManager.Instance.currentLevel.factorySizeMultiplier;
                    }
                }
            }
        }

        private static RoundState getRoundState()
        {
            GameObject obj = GameObject.Find("LevelModifier");
            if (obj != null)
            {
                return obj.GetComponent<RoundState>();
            }
            return null;
        }
    }
}

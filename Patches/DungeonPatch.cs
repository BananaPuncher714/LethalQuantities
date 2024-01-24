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
            RoundState state = Plugin.getRoundState();
            if (state != null)
            {
                if (state.getValidDungeonGenerationConfiguration(out DungeonGenerationConfiguration configuration))
                {
                    string name = __instance.Generator.DungeonFlow.name;
                    if (configuration.dungeonFlowConfigurations.TryGetValue(name, out DungeonFlowConfiguration config))
                    {
                        config.factorySizeMultiplier.Set(ref RoundManager.Instance.currentLevel.factorySizeMultiplier);

                        __instance.Generator.LengthMultiplier = RoundManager.Instance.mapSizeMultiplier * RoundManager.Instance.currentLevel.factorySizeMultiplier;
                    }
                }
            }
        }
    }
}

using HarmonyLib;
using LethalQuantities.Objects;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        [HarmonyPriority(Priority.First)]
        [HarmonyPrefix]
        static void onLoadNewLevelPrefix(ref SelectableLevel newLevel)
        {
            if (!RoundManager.Instance.IsServer)
            {
                return;
            }
            RoundState state = getRoundState();
            if (state != null)
            {
                Plugin.LETHAL_LOGGER.LogInfo("RoundState found, modifying level before loading");

                if (state.levelConfiguration.enemies.enabled.Value)
                {
                    newLevel.maxEnemyPowerCount = state.levelConfiguration.enemies.maxPowerCount.Value;
                    newLevel.enemySpawnChanceThroughoutDay = state.levelConfiguration.enemies.spawnAmountCurve.Value;
                    newLevel.spawnProbabilityRange = state.levelConfiguration.enemies.spawnAmountRange.Value;
                    newLevel.Enemies.Clear();
                    newLevel.Enemies.AddRange(state.enemies);
                }

                if (state.levelConfiguration.daytimeEnemies.enabled.Value)
                {
                    newLevel.maxDaytimeEnemyPowerCount = state.levelConfiguration.daytimeEnemies.maxPowerCount.Value;
                    newLevel.daytimeEnemySpawnChanceThroughDay = state.levelConfiguration.daytimeEnemies.spawnAmountCurve.Value;
                    newLevel.daytimeEnemiesProbabilityRange = state.levelConfiguration.daytimeEnemies.spawnAmountRange.Value;
                    newLevel.DaytimeEnemies.Clear();
                    newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);
                }

                if (state.levelConfiguration.outsideEnemies.enabled.Value)
                {
                    newLevel.maxOutsideEnemyPowerCount = state.levelConfiguration.outsideEnemies.maxPowerCount.Value;
                    newLevel.outsideEnemySpawnChanceThroughDay = state.levelConfiguration.outsideEnemies.spawnAmountCurve.Value;
                    // Nothing for outside enemy spawn range probability
                    newLevel.OutsideEnemies.Clear();
                    newLevel.OutsideEnemies.AddRange(state.outsideEnemies);
                }
            }
        }

        // TODO Rewrite spawning algorithm for normal, day, and outside enemies
        // TODO Account for weather, so use minEnemySpawnChances and whatnot

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

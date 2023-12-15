using HarmonyLib;
using System;
using LethalQuantities.Objects;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.BeginEnemySpawning))]
        [HarmonyPrefix]
        static void onBeginEnemySpawningPrefix(RoundManager __instance)
        {
            if (!__instance.IsServer)
            {
                return;
            }
            RoundState state = getRoundState();
            if (state != null)
            {
                Plugin.LETHAL_LOGGER.LogInfo("RoundState found, modifying level before enemy spawning");

                SelectableLevel level = __instance.currentLevel;
                if (state.levelConfiguration.enemies.enabled.Value)
                {
                    level.maxEnemyPowerCount = state.levelConfiguration.enemies.maxPowerCount.Value;
                    level.enemySpawnChanceThroughoutDay = state.levelConfiguration.enemies.spawnAmountCurve.Value;
                    level.spawnProbabilityRange = state.levelConfiguration.enemies.spawnAmountRange.Value;
                    level.Enemies.Clear();
                    level.Enemies.AddRange(state.enemies);
                }

                if (state.levelConfiguration.daytimeEnemies.enabled.Value)
                {
                    level.maxDaytimeEnemyPowerCount = state.levelConfiguration.daytimeEnemies.maxPowerCount.Value;
                    level.daytimeEnemySpawnChanceThroughDay = state.levelConfiguration.daytimeEnemies.spawnAmountCurve.Value;
                    level.daytimeEnemiesProbabilityRange = state.levelConfiguration.daytimeEnemies.spawnAmountRange.Value;
                    level.DaytimeEnemies.Clear();
                    level.DaytimeEnemies.AddRange(state.daytimeEnemies);
                }

                if (state.levelConfiguration.outsideEnemies.enabled.Value)
                {
                    level.maxOutsideEnemyPowerCount = state.levelConfiguration.outsideEnemies.maxPowerCount.Value;
                    level.outsideEnemySpawnChanceThroughDay = state.levelConfiguration.outsideEnemies.spawnAmountCurve.Value;
                    // Nothing for outside enemy spawn range probability
                    level.OutsideEnemies.Clear();
                    level.OutsideEnemies.AddRange(state.outsideEnemies);
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

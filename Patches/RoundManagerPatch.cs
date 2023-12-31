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
        private static void onLoadNewLevelPrefix(RoundManager __instance, ref SelectableLevel newLevel)
        {
            if (!__instance.IsServer)
            {
                return;
            }
            RoundState state = getRoundState();
            if (state != null)
            {
                Plugin.LETHAL_LOGGER.LogInfo("RoundState found, modifying level before loading");

                if (state.levelConfiguration.enemies.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing inside enemy values");
                    newLevel.maxEnemyPowerCount = state.levelConfiguration.enemies.maxPowerCount.Value;
                    newLevel.enemySpawnChanceThroughoutDay = state.levelConfiguration.enemies.spawnAmountCurve.Value;
                    newLevel.spawnProbabilityRange = state.levelConfiguration.enemies.spawnAmountRange.Value;
                    newLevel.Enemies.Clear();
                    newLevel.Enemies.AddRange(state.enemies);
                }

                if (state.levelConfiguration.daytimeEnemies.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing daytime enemy values");
                    newLevel.maxDaytimeEnemyPowerCount = state.levelConfiguration.daytimeEnemies.maxPowerCount.Value;
                    newLevel.daytimeEnemySpawnChanceThroughDay = state.levelConfiguration.daytimeEnemies.spawnAmountCurve.Value;
                    newLevel.daytimeEnemiesProbabilityRange = state.levelConfiguration.daytimeEnemies.spawnAmountRange.Value;
                    newLevel.DaytimeEnemies.Clear();
                    newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);
                }

                if (state.levelConfiguration.outsideEnemies.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing outside enemy values");
                    newLevel.maxOutsideEnemyPowerCount = state.levelConfiguration.outsideEnemies.maxPowerCount.Value;
                    newLevel.outsideEnemySpawnChanceThroughDay = state.levelConfiguration.outsideEnemies.spawnAmountCurve.Value;
                    // Nothing for outside enemy spawn range probability
                    newLevel.OutsideEnemies.Clear();
                    newLevel.OutsideEnemies.AddRange(state.outsideEnemies);
                }

                if (state.levelConfiguration.scrap.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing scrap values");
                    newLevel.minScrap = state.levelConfiguration.scrap.minScrap.Value;
                    newLevel.maxScrap = state.levelConfiguration.scrap.maxScrap.Value;
                    __instance.scrapAmountMultiplier = state.levelConfiguration.scrap.scrapAmountMultiplier.Value;
                    __instance.scrapValueMultiplier = state.levelConfiguration.scrap.scrapValueMultiplier.Value;

                    newLevel.spawnableScrap.Clear();
                    foreach (ItemConfiguration item in state.levelConfiguration.scrap.scrapRarities)
                    {
                        SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                        newItem.spawnableItem = item.item;
                        newItem.rarity = item.rarity.Value;
                        if (item is ScrapItemConfiguration)
                        {
                            ScrapItemConfiguration scrapItem = item as ScrapItemConfiguration;
                            newItem.spawnableItem.maxValue = scrapItem.maxValue.Value;
                            newItem.spawnableItem.minValue = scrapItem.minValue.Value;
                        }
                        newLevel.spawnableScrap.Add(newItem);
                    }
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
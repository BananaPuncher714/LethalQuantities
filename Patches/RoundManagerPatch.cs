using HarmonyLib;
using LethalQuantities.Objects;
using System.Collections.Generic;
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
                    state.levelConfiguration.enemies.maxPowerCount.Set(ref newLevel.maxEnemyPowerCount);
                    state.levelConfiguration.enemies.spawnAmountCurve.Set(ref newLevel.enemySpawnChanceThroughoutDay);
                    state.levelConfiguration.enemies.spawnAmountRange.Set(ref newLevel.spawnProbabilityRange);
                    newLevel.Enemies.Clear();
                    newLevel.Enemies.AddRange(state.enemies);
                }
                else if (state.globalConfiguration.enemyConfiguration.enabled.Value && !state.globalConfiguration.enemyConfiguration.isDefault())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing inside enemy values based on the global config");
                    state.globalConfiguration.enemyConfiguration.maxPowerCount.Set(ref newLevel.maxEnemyPowerCount);
                    state.globalConfiguration.enemyConfiguration.spawnAmountCurve.Set(ref newLevel.enemySpawnChanceThroughoutDay);
                    state.globalConfiguration.enemyConfiguration.spawnAmountRange.Set(ref newLevel.spawnProbabilityRange);
                    newLevel.Enemies.Clear();
                    newLevel.Enemies.AddRange(state.enemies);
                }

                if (state.levelConfiguration.daytimeEnemies.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing daytime enemy values");
                    state.levelConfiguration.daytimeEnemies.maxPowerCount.Set(ref newLevel.maxDaytimeEnemyPowerCount);
                    state.levelConfiguration.daytimeEnemies.spawnAmountCurve.Set(ref newLevel.daytimeEnemySpawnChanceThroughDay);
                    state.levelConfiguration.daytimeEnemies.spawnAmountRange.Set(ref newLevel.daytimeEnemiesProbabilityRange);
                    newLevel.DaytimeEnemies.Clear();
                    newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);
                }
                else if (state.globalConfiguration.daytimeEnemyConfiguration.enabled.Value && !state.globalConfiguration.daytimeEnemyConfiguration.isDefault())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing daytime values based on the global config");
                    state.globalConfiguration.daytimeEnemyConfiguration.maxPowerCount.Set(ref newLevel.maxDaytimeEnemyPowerCount);
                    state.globalConfiguration.daytimeEnemyConfiguration.spawnAmountCurve.Set(ref newLevel.daytimeEnemySpawnChanceThroughDay);
                    state.globalConfiguration.daytimeEnemyConfiguration.spawnAmountRange.Set(ref newLevel.daytimeEnemiesProbabilityRange);
                    newLevel.DaytimeEnemies.Clear();
                    newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);
                }

                if (state.levelConfiguration.outsideEnemies.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing outside enemy values");
                    state.levelConfiguration.outsideEnemies.maxPowerCount.Set(ref newLevel.maxOutsideEnemyPowerCount);
                    state.levelConfiguration.outsideEnemies.spawnAmountCurve.Set(ref newLevel.outsideEnemySpawnChanceThroughDay);
                    // Nothing for outside enemy spawn range probability, since it's hardcoded to 3 internally
                    newLevel.OutsideEnemies.Clear();
                    newLevel.OutsideEnemies.AddRange(state.outsideEnemies);
                }
                else if (state.globalConfiguration.outsideEnemyConfiguration.enabled.Value && !state.globalConfiguration.outsideEnemyConfiguration.isDefault())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing outside enemy values based on the global config");
                    state.globalConfiguration.outsideEnemyConfiguration.maxPowerCount.Set(ref newLevel.maxOutsideEnemyPowerCount);
                    state.globalConfiguration.outsideEnemyConfiguration.spawnAmountCurve.Set(ref newLevel.outsideEnemySpawnChanceThroughDay);
                    newLevel.OutsideEnemies.Clear();
                    newLevel.OutsideEnemies.AddRange(state.outsideEnemies);
                }

                if (state.levelConfiguration.scrap.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing scrap values");
                    state.levelConfiguration.scrap.minScrap.Set(ref newLevel.minScrap);
                    state.levelConfiguration.scrap.maxScrap.Set(ref newLevel.maxScrap);
                    state.levelConfiguration.scrap.scrapAmountMultiplier.Set(ref __instance.scrapAmountMultiplier);
                    state.levelConfiguration.scrap.scrapValueMultiplier.Set(ref __instance.scrapValueMultiplier);

                    Dictionary<Item, int> defaultRarities = new Dictionary<Item, int>();
                    foreach (SpawnableItemWithRarity item in newLevel.spawnableScrap)
                    {
                        defaultRarities.Add(item.spawnableItem, item.rarity);
                    }

                    newLevel.spawnableScrap.Clear();
                    foreach (ItemConfiguration item in state.levelConfiguration.scrap.scrapRarities)
                    {
                        SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                        newItem.spawnableItem = item.item;
                        // Get the default item rarity value, if it already exists somewhere, otherwise use the default value, or else use the value set in the config
                        newItem.rarity = defaultRarities.GetValueOrDefault(item.item, 0);
                        item.rarity.Set(ref newItem.rarity);
                        if (newItem.rarity > 0)
                        {
                            if (item is ScrapItemConfiguration)
                            {
                                ScrapItemConfiguration scrapItem = item as ScrapItemConfiguration;
                                scrapItem.minValue.Set(ref newItem.spawnableItem.minValue);
                                scrapItem.maxValue.Set(ref newItem.spawnableItem.maxValue);
                            }
                            newLevel.spawnableScrap.Add(newItem);
                        }
                    }
                }
                else if (state.globalConfiguration.scrapConfiguration.enabled.Value && !state.globalConfiguration.scrapConfiguration.isDefault())
                {
                    state.globalConfiguration.scrapConfiguration.minScrap.Set(ref newLevel.minScrap);
                    state.globalConfiguration.scrapConfiguration.maxScrap.Set(ref newLevel.maxScrap);
                    state.globalConfiguration.scrapConfiguration.scrapAmountMultiplier.Set(ref __instance.scrapAmountMultiplier);
                    state.globalConfiguration.scrapConfiguration.scrapValueMultiplier.Set(ref __instance.scrapValueMultiplier);

                    Dictionary<Item, int> defaultRarities = new Dictionary<Item, int>();
                    foreach (SpawnableItemWithRarity item in newLevel.spawnableScrap)
                    {
                        defaultRarities.Add(item.spawnableItem, item.rarity);
                    }

                    newLevel.spawnableScrap.Clear();
                    foreach (var item in state.globalConfiguration.scrapConfiguration.itemConfigurations)
                    {
                        Item type = item.Key;
                        GlobalItemConfiguration globalItemConfiguration = item.Value;
                        if (!globalItemConfiguration.isDefault())
                        {
                            int rarity = defaultRarities[type];
                            globalItemConfiguration.rarity.Set(ref rarity);
                            if (rarity > 0)
                            {
                                SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                                newItem.spawnableItem = type;
                                newItem.rarity = rarity;
                                newLevel.spawnableScrap.Add(newItem);

                                if (globalItemConfiguration is GlobalItemScrapConfiguration)
                                {
                                    GlobalItemScrapConfiguration scrapConfig = globalItemConfiguration as GlobalItemScrapConfiguration;

                                    scrapConfig.minValue.Set(ref type.minValue);
                                    scrapConfig.maxValue.Set(ref type.maxValue);
                                }
                            }
                        }
                        else if (defaultRarities.ContainsKey(type))
                        {
                            SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                            newItem.spawnableItem = type;
                            newItem.rarity = defaultRarities[type];
                            newLevel.spawnableScrap.Add(newItem);
                        }
                    }

                    foreach (SpawnableItemWithRarity item in newLevel.spawnableScrap)
                    {
                        GlobalItemScrapConfiguration scrapConfig = state.globalConfiguration.scrapConfiguration.itemConfigurations[item.spawnableItem];
                        if (!scrapConfig.isDefault())
                        {
                            scrapConfig.minValue.Set(ref item.spawnableItem.minValue);
                            scrapConfig.maxValue.Set(ref item.spawnableItem.maxValue);
                        }
                    }
                }
            }
        }

        // TODO Rewrite spawning algorithm for normal, day, and outside enemies
        // TODO Account for weather, so use minEnemySpawnChances and whatnot, maybe?

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
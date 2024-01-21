using DunGen.Graph;
using HarmonyLib;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class RoundManagerPatch
    {
        //[HarmonyPatch(typeof(RoundManager), "Start")]
        //[HarmonyPriority(200)]
        //[HarmonyPostfix]
        // TODO Determine whether or not to use this
        private static void onStartPostfix(RoundManager __instance)
        {
            if (!__instance.IsServer || Plugin.INSTANCE.configInitialized)
            {
                return;
            }
            StartOfRound instance = StartOfRound.Instance;
            GlobalInformation globalInfo = new GlobalInformation(Plugin.GLOBAL_SAVE_DIR, Plugin.LEVEL_SAVE_DIR);

            // Get all enemy, item and dungeon flows
            // Filter out any potentially "fake" enemy types that might have been added by other mods
            HashSet<string> addedEnemyTypes = new HashSet<string>();
            globalInfo.allEnemyTypes.AddRange(Resources.FindObjectsOfTypeAll<EnemyType>().Where(type => {
                if (type.enemyPrefab == null || addedEnemyTypes.Contains(type.name))
                {
                    return false;
                }
                else
                {
                    addedEnemyTypes.Add(type.name);
                    return true;
                }
            }));
            HashSet<string> addedItems = new HashSet<string>();
            globalInfo.allItems.AddRange(Resources.FindObjectsOfTypeAll<Item>().Where(type => {
                if (type.spawnPrefab == null || addedItems.Contains(type.name))
                {
                    return false;
                }
                else
                {
                    addedItems.Add(type.name);
                    return true;
                }
            }));

            globalInfo.allSelectableLevels.AddRange(Resources.FindObjectsOfTypeAll<SelectableLevel>());
            globalInfo.allDungeonFlows.AddRange(Resources.FindObjectsOfTypeAll<DungeonFlow>());
            globalInfo.sortData();

            Plugin.LETHAL_LOGGER.LogInfo("Loading global configuration");
            GlobalConfiguration configuration = new GlobalConfiguration(globalInfo);

            Plugin.INSTANCE.configuration = configuration;
            Plugin.INSTANCE.configInitialized = true;

            Plugin.LETHAL_LOGGER.LogInfo("Inserting missing dungeon flows into the RoundManager");
            // Not very good, but for each dungeon flow, add it to the RoundManager if it isn't already there
            List<DungeonFlow> flows = new List<DungeonFlow>(RoundManager.Instance.dungeonFlowTypes);
            foreach (DungeonFlow flow in globalInfo.allDungeonFlows)
            {
                int index = -1;
                for (int i = 0; i < flows.Count; i++)
                {
                    if (flows[i] == flow)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    // Not added, so add it now
                    Plugin.LETHAL_LOGGER.LogWarning($"Did not find dungeon flow {flow.name} in the global list of dungeon flows. Adding it now.");
                    flows.Add(flow);
                }
            }
            RoundManager.Instance.dungeonFlowTypes = flows.ToArray();

            // Set some global options here
            if (configuration.scrapConfiguration.enabled.Value)
            {
                Plugin.LETHAL_LOGGER.LogInfo("Setting custom item weight values");
                foreach (Item item in globalInfo.allItems)
                {
                    GlobalItemConfiguration itemConfig = configuration.scrapConfiguration.itemConfigurations[item];
                    itemConfig.weight.Set(ref item.weight);
                }
            }
            Plugin.LETHAL_LOGGER.LogInfo("Done configuring LethalQuantities");
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        // Keep it at an acceptable priority level, so that if some other mod has an urgent need to modify anything before us, they can
        [HarmonyPriority(100)]
        [HarmonyPrefix]
        // Attempt to set the various level variables before other mods, since this should serve as the "base" for level information.
        // Values set by this mod are meant to be overwritten, especially by mods that add events, or varying changes.
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
                        if (!defaultRarities.TryAdd(item.spawnableItem, item.rarity))
                        {
                            Plugin.LETHAL_LOGGER.LogWarning($"{newLevel.name} has a duplicate spawnable scrap item: {item.spawnableItem.name}");
                            defaultRarities[item.spawnableItem] += item.rarity;
                        }
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
                            item.conductive.Set(ref newItem.spawnableItem.isConductiveMetal);
                            if (item is ScrapItemConfiguration)
                            {
                                ScrapItemConfiguration scrapItem = item as ScrapItemConfiguration;

                                int minValue = newItem.spawnableItem.minValue;
                                int maxValue = newItem.spawnableItem.maxValue;

                                scrapItem.minValue.Set(ref minValue);
                                scrapItem.maxValue.Set(ref maxValue);

                                newItem.spawnableItem.minValue = Math.Max(minValue, maxValue);
                                newItem.spawnableItem.maxValue = Math.Max(minValue, maxValue);
                            }
                            newLevel.spawnableScrap.Add(newItem);
                        }
                    }
                }
                else if (state.globalConfiguration.scrapConfiguration.enabled.Value && !state.globalConfiguration.scrapConfiguration.isDefault())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing scrap values based on the global config");
                    state.globalConfiguration.scrapConfiguration.minScrap.Set(ref newLevel.minScrap);
                    state.globalConfiguration.scrapConfiguration.maxScrap.Set(ref newLevel.maxScrap);
                    state.globalConfiguration.scrapConfiguration.scrapAmountMultiplier.Set(ref __instance.scrapAmountMultiplier);
                    state.globalConfiguration.scrapConfiguration.scrapValueMultiplier.Set(ref __instance.scrapValueMultiplier);

                    Dictionary<Item, int> defaultRarities = new Dictionary<Item, int>();
                    foreach (SpawnableItemWithRarity item in newLevel.spawnableScrap)
                    {
                        if (!defaultRarities.TryAdd(item.spawnableItem, item.rarity))
                        {
                            Plugin.LETHAL_LOGGER.LogWarning($"{newLevel.name} has a duplicate spawnable scrap item: {item.spawnableItem.name}");
                            defaultRarities[item.spawnableItem] += item.rarity;
                        }
                    }

                    newLevel.spawnableScrap.Clear();
                    foreach (var item in state.globalConfiguration.scrapConfiguration.itemConfigurations)
                    {
                        Item type = item.Key;
                        GlobalItemConfiguration globalItemConfiguration = item.Value;
                        if (!globalItemConfiguration.isDefault())
                        {
                            int rarity = defaultRarities.GetValueOrDefault(type, 0);
                            globalItemConfiguration.rarity.Set(ref rarity);
                            if (rarity > 0)
                            {
                                SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                                newItem.spawnableItem = type;
                                newItem.rarity = rarity;
                                newLevel.spawnableScrap.Add(newItem);

                                globalItemConfiguration.conductive.Set(ref type.isConductiveMetal);
                                if (globalItemConfiguration is GlobalItemScrapConfiguration)
                                {
                                    GlobalItemScrapConfiguration scrapConfig = globalItemConfiguration as GlobalItemScrapConfiguration;

                                    int minValue = type.minValue;
                                    int maxValue = type.maxValue;

                                    scrapConfig.minValue.Set(ref minValue);
                                    scrapConfig.maxValue.Set(ref maxValue);

                                    type.minValue = Math.Max(minValue, maxValue);
                                    type.maxValue = Math.Max(minValue, maxValue);
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
                }

                if (state.levelConfiguration.dungeon.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing dungeon flow values");
                    state.levelConfiguration.dungeon.mapSizeMultiplier.Set(ref __instance.mapSizeMultiplier);
                    Dictionary<string, int> flows = new Dictionary<string, int>();
                    foreach (IntWithRarity entry in newLevel.dungeonFlowTypes)
                    {
                        flows.TryAdd(__instance.dungeonFlowTypes[entry.id].name, entry.rarity);
                    }
                    foreach (var item in state.levelConfiguration.dungeon.dungeonFlowConfigurations)
                    {
                        string name = item.Key;
                        DungeonFlowConfiguration config = item.Value;

                        int originalRarity = flows.GetValueOrDefault(name, 0);
                        config.rarity.Set(ref originalRarity);
                        flows[name] = originalRarity;
                    }
                    newLevel.dungeonFlowTypes = __instance.ConvertToDungeonFlowArray(flows);
                }
                else if (state.globalConfiguration.dungeonConfiguration.enabled.Value && !state.globalConfiguration.dungeonConfiguration.isDefault())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing dungeon flow values based on the global config");
                    state.globalConfiguration.dungeonConfiguration.mapSizeMultiplier.Set(ref __instance.mapSizeMultiplier);
                    Dictionary<string, int> flows = new Dictionary<string, int>();
                    foreach (IntWithRarity entry in newLevel.dungeonFlowTypes)
                    {
                        flows.TryAdd(__instance.dungeonFlowTypes[entry.id].name, entry.rarity);
                    }
                    foreach (var item in state.globalConfiguration.dungeonConfiguration.dungeonFlowConfigurations)
                    {
                        string name = item.Key;
                        GlobalDungeonFlowConfiguration config = item.Value;

                        int originalRarity = flows.GetValueOrDefault(name, 0);
                        config.rarity.Set(ref originalRarity);
                        flows[name] = originalRarity;
                    }
                    newLevel.dungeonFlowTypes = __instance.ConvertToDungeonFlowArray(flows);
                }

                // TODO Should really reduce the amount of repetition that occurs here
                if (state.levelConfiguration.trap.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing interior trap spawn amounts");
                    Dictionary<GameObject, AnimationCurve> defaultSpawnableMapObjects = new Dictionary<GameObject, AnimationCurve>();
                    foreach (SpawnableMapObject obj in newLevel.spawnableMapObjects)
                    {
                        defaultSpawnableMapObjects.TryAdd(obj.prefabToSpawn, obj.numberToSpawn);
                    }
                    List<SpawnableMapObject> newMapObjects = new List<SpawnableMapObject>();
                    foreach (var item in state.levelConfiguration.trap.traps)
                    {
                        GameObject obj = item.Key;
                        SpawnableMapObjectConfiguration config = item.Value;

                        AnimationCurve curve = defaultSpawnableMapObjects.GetValueOrDefault(obj, new AnimationCurve());
                        config.numberToSpawn.Set(ref curve);

                        SpawnableMapObject spawnableObj = new SpawnableMapObject();
                        spawnableObj.prefabToSpawn = obj;
                        spawnableObj.numberToSpawn = curve;
                        spawnableObj.spawnFacingAwayFromWall = config.spawnableObject.faceAwayFromWall;

                        newMapObjects.Add(spawnableObj);
                    }
                    newLevel.spawnableMapObjects = newMapObjects.ToArray();
                }
                else if (state.globalConfiguration.trapConfiguration.enabled.Value && !state.globalConfiguration.trapConfiguration.isDefault())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing interior trap spawn amounts based on the global config");
                    Dictionary<GameObject, AnimationCurve> defaultSpawnableMapObjects = new Dictionary<GameObject, AnimationCurve>();
                    foreach (SpawnableMapObject obj in newLevel.spawnableMapObjects)
                    {
                        defaultSpawnableMapObjects.TryAdd(obj.prefabToSpawn, obj.numberToSpawn);
                    }
                    List<SpawnableMapObject> newMapObjects = new List<SpawnableMapObject>();
                    foreach (var item in state.globalConfiguration.trapConfiguration.traps)
                    {
                        GameObject obj = item.Key;
                        GlobalSpawnableMapObjectConfiguration config = item.Value;

                        AnimationCurve curve = defaultSpawnableMapObjects.GetValueOrDefault(obj, new AnimationCurve());
                        config.numberToSpawn.Set(ref curve);

                        SpawnableMapObject spawnableObj = new SpawnableMapObject();
                        spawnableObj.prefabToSpawn = obj;
                        spawnableObj.numberToSpawn = curve;
                        spawnableObj.spawnFacingAwayFromWall = config.spawnableObj.faceAwayFromWall;

                        newMapObjects.Add(spawnableObj);
                    }
                    newLevel.spawnableMapObjects = newMapObjects.ToArray();
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

    public static class RoundManagerExtensions
    {
        public static IntWithRarity[] ConvertToDungeonFlowArray(this RoundManager manager, Dictionary<string, int> entries)
        {
            List<IntWithRarity> res = new List<IntWithRarity>();
            foreach (var entry in entries)
            {
                int rarity = entry.Value;
                if (rarity > 0)
                {
                    int id = -1;
                    for (int i = 0; i < manager.dungeonFlowTypes.Length && id < 0; i++)
                    {
                        if (manager.dungeonFlowTypes[i].name == entry.Key)
                        {
                            id = i;
                        }
                    }

                    if (id != -1)
                    {
                        IntWithRarity weight = new IntWithRarity();
                        weight.id = id;
                        weight.rarity = entry.Value;
                        res.Add(weight);
                    }
                    else
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Could not find dungeon flow {entry.Key}!");
                    }
                }
            }

            return res.ToArray();
        }
    }
}
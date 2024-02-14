using DunGen.Graph;
using HarmonyLib;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class RoundManagerPatch
    {
        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPriority(250)]
        [HarmonyPostfix]
        private static void onStartPrefix(RoundManager __instance)
        {
            if (!Plugin.INSTANCE.configInitialized)
            {
                NetworkManager manager = UnityEngine.Object.FindObjectOfType<NetworkManager>();
                GlobalInformation globalInfo = new GlobalInformation(Plugin.GLOBAL_SAVE_DIR, Plugin.LEVEL_SAVE_DIR);
                // Get all enemy, item and dungeon flows
                // Filter out any potentially "fake" enemy types that might have been added by other mods
                Plugin.LETHAL_LOGGER.LogInfo("Fetching all enemy types");
                HashSet<string> addedEnemyTypes = new HashSet<string>();
                globalInfo.allEnemyTypes.AddRange(Resources.FindObjectsOfTypeAll<EnemyType>().Where(type =>
                {
                    if (type.enemyPrefab == null)
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Enemy type {type.name} is missing prefab! Perhaps another mod has removed it? Some default values in the config may not be correct.");
                    }
                    else if (!manager.NetworkConfig.Prefabs.Contains(type.enemyPrefab))
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Enemy type {type.name} is not a real object! Ignoring type");
                    }
                    else if (addedEnemyTypes.Contains(type.name))
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Enemy type {type.name} was found twice! Perhaps another mod has added it? Some default values in the config may not be correct.");
                    }
                    else
                    {
                        addedEnemyTypes.Add(type.name);
                        return true;
                    }
                    return false;
                }));
                Plugin.LETHAL_LOGGER.LogInfo("Fetching all items");
                HashSet<string> addedItems = new HashSet<string>();
                globalInfo.allItems.AddRange(Resources.FindObjectsOfTypeAll<Item>().Where(type =>
                {
                    if (type.spawnPrefab == null)
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Item {type.name} is missing prefab! Perhaps another mod has removed it? Some default values in the config may not be correct.");
                    }
                    else if (!manager.NetworkConfig.Prefabs.Contains(type.spawnPrefab))
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Item {type.name} is not a real item! Ignoring item");
                    }
                    else if (addedItems.Contains(type.name))
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Item {type.name} was found twice! Perhaps another mod has added it? Some default values in the config may not be correct.");
                    }
                    else
                    {
                        addedItems.Add(type.name);
                        return true;
                    }
                    return false;
                }));

                Plugin.LETHAL_LOGGER.LogInfo("Fetching all moon prices");
                CompatibleNoun[] nouns = Resources.FindObjectsOfTypeAll<TerminalKeyword>().First(w => w.word != null && w.word.ToLower() == "route").compatibleNouns;
                foreach (SelectableLevel level in Resources.FindObjectsOfTypeAll<SelectableLevel>())
                {
                    GenericLevelInformation genericInfo = new GenericLevelInformation();
                    bool found = false;
                    foreach (CompatibleNoun noun in nouns)
                    {
                        TerminalNode result = noun.result;
                        if (result.terminalOptions == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Route subcommand {result.name} does not have any valid terminal options!");
                            continue;
                        }

                        CompatibleNoun confirmNoun = result.terminalOptions.First(n => n.noun.word.ToLower() == "confirm");
                        if (confirmNoun == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Unable to find a confirm option for route command {result.name}");
                            continue;
                        }
                        TerminalNode confirm = confirmNoun.result;

                        if (confirm == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Found a confirm option for route command {result.name}, but it has no result node!");
                            continue;
                        }

                        int levelId = confirm.buyRerouteToMoon;

                        if (level.levelID == confirm.buyRerouteToMoon)
                        {
                            Plugin.LETHAL_LOGGER.LogInfo($"Found the price {confirm.itemCost} for {level.name}");
                            genericInfo.price = confirm.itemCost;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Unable to find price of {level.name}({level.PlanetName})");
                    }

                    globalInfo.allSelectableLevels.Add(level, genericInfo);
                }

                Plugin.LETHAL_LOGGER.LogInfo("Fetching all traps");
                Dictionary<GameObject, DirectionalSpawnableMapObject> uniqueMapObjects = new Dictionary<GameObject, DirectionalSpawnableMapObject>();
                // Keep track of added objects, try to make sure we don't add the same one twice
                HashSet<string> addedTraps = new HashSet<string>();
                foreach (SelectableLevel level in globalInfo.allSelectableLevels.Keys)
                {
                    foreach (SpawnableMapObject spawnableObject in level.spawnableMapObjects)
                    {
                        GameObject prefab = spawnableObject.prefabToSpawn;
                        if (!addedTraps.Contains(prefab.name))
                        {
                            // Only add the prefab if it is a networked object
                            if (manager.NetworkConfig.Prefabs.Contains(prefab))
                            {
                                addedTraps.Add(prefab.name);
                                uniqueMapObjects.TryAdd(prefab, new DirectionalSpawnableMapObject(prefab, spawnableObject.spawnFacingAwayFromWall));
                            }
                        }
                    }
                }
                globalInfo.allSpawnableMapObjects.AddRange(uniqueMapObjects.Values);

                Plugin.LETHAL_LOGGER.LogInfo("Fetching all dungeon flows");
                globalInfo.allDungeonFlows.AddRange(Resources.FindObjectsOfTypeAll<DungeonFlow>());
                globalInfo.manager = __instance;
                globalInfo.sortData();

                GlobalConfiguration configuration;
                if (!Plugin.INSTANCE.configInitialized)
                {
                    // The purpose for this segment below is to save the vanilla information, before any mods change any values
                    Plugin.LETHAL_LOGGER.LogInfo("Loading global configuration");
                    configuration = new GlobalConfiguration(globalInfo);

                    Plugin.INSTANCE.configuration = configuration;
                    Plugin.INSTANCE.configInitialized = true;

                    Plugin.LETHAL_LOGGER.LogInfo("Done configuring LethalQuantities");
                }
                else
                {
                    configuration = Plugin.INSTANCE.configuration;
                }

                // Set some global options here
                if (configuration.scrapConfiguration.enabled.Value)
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Setting custom item weight values");
                    foreach (Item item in globalInfo.allItems)
                    {
                        ItemConfiguration itemConfig = configuration.scrapConfiguration.items[item];
                        if (itemConfig is IWeightConfigurable)
                        {
                            (itemConfig as IWeightConfigurable).weight.Set(ref item.weight);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        private static void onStartPostfix(RoundManager __instance)
        {
            StartOfRoundPatch.updateMoonPrices(StartOfRound.Instance.currentLevel);
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        // Keep it at an acceptable priority level, so that if some other mod has an urgent need to modify anything before us, they can
        [HarmonyPriority(100)]
        [HarmonyPrefix]
        // Attempt to set the various level variables before other mods, since this should serve as the "base" for level information.
        // Values set by this mod are meant to be overwritten, especially by mods that add events, or varying changes.
        private static void onLoadNewLevelPrefix(RoundManager __instance, ref SelectableLevel newLevel)
        {
            RoundState state = Plugin.getRoundState(newLevel);
            if (state != null)
            {
                if (__instance.IsServer)
                {
                    Plugin.LETHAL_LOGGER.LogInfo($"RoundState found for level {state.level.name}, modifying level before loading");
                    int minimumSpawnProbabilityRange = (int)Math.Ceiling(Math.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 3.2);
                    {
                        if (state.getValidEnemyConfiguration(out EnemyConfiguration<EnemyTypeConfiguration> config))
                        {
                            Plugin.LETHAL_LOGGER.LogInfo("Changing inside enemy values");
                            config.maxPowerCount.Set(ref newLevel.maxEnemyPowerCount);
                            config.spawnAmountCurve.Set(ref newLevel.enemySpawnChanceThroughoutDay);
                            config.spawnAmountRange.Set(ref newLevel.spawnProbabilityRange);
                            // Make sure the game does not break if the spawn probability range is too small
                            if (newLevel.spawnProbabilityRange < minimumSpawnProbabilityRange)
                            {
                                Plugin.LETHAL_LOGGER.LogWarning($"Interior enemy spawn amount range is too small({newLevel.spawnProbabilityRange}), setting to {minimumSpawnProbabilityRange}");
                                newLevel.spawnProbabilityRange = minimumSpawnProbabilityRange;
                            }
                            newLevel.Enemies.Clear();
                            newLevel.Enemies.AddRange(state.enemies);
                        }
                    }

                    {
                        if (state.getValidOutsideEnemyConfiguration(out OutsideEnemyConfiguration<EnemyTypeConfiguration> config))
                        {
                            Plugin.LETHAL_LOGGER.LogInfo("Changing outside enemy values");
                            config.maxPowerCount.Set(ref newLevel.maxOutsideEnemyPowerCount);
                            config.spawnAmountCurve.Set(ref newLevel.outsideEnemySpawnChanceThroughDay);
                            newLevel.OutsideEnemies.Clear();
                            newLevel.OutsideEnemies.AddRange(state.outsideEnemies);
                        }
                    }

                    {
                        if (state.getValidDaytimeEnemyConfiguration(out EnemyConfiguration<DaytimeEnemyTypeConfiguration> config))
                        {
                            Plugin.LETHAL_LOGGER.LogInfo("Changing daytime enemy values");
                            config.maxPowerCount.Set(ref newLevel.maxDaytimeEnemyPowerCount);
                            config.spawnAmountCurve.Set(ref newLevel.daytimeEnemySpawnChanceThroughDay);
                            config.spawnAmountRange.Set(ref newLevel.daytimeEnemiesProbabilityRange);
                            // Make sure the game does not break if the spawn probability range is too small
                            if (newLevel.daytimeEnemiesProbabilityRange < minimumSpawnProbabilityRange)
                            {
                                Plugin.LETHAL_LOGGER.LogWarning($"Interior enemy spawn amount range is too small({newLevel.daytimeEnemiesProbabilityRange}), setting to {minimumSpawnProbabilityRange}");
                                newLevel.daytimeEnemiesProbabilityRange = minimumSpawnProbabilityRange;
                            }
                            newLevel.DaytimeEnemies.Clear();
                            newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);
                        }
                    }

                    {
                        if (state.getValidScrapConfiguration(out ScrapConfiguration configuration))
                        {
                            Plugin.LETHAL_LOGGER.LogInfo("Changing scrap values");
                            configuration.minScrap.Set(ref newLevel.minScrap);
                            configuration.maxScrap.Set(ref newLevel.maxScrap);
                            configuration.scrapAmountMultiplier.Set(ref __instance.scrapAmountMultiplier);
                            configuration.scrapValueMultiplier.Set(ref __instance.scrapValueMultiplier);

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
                            foreach (ItemConfiguration item in configuration.items.Values)
                            {
                                SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                                newItem.spawnableItem = item.item;
                                // Get the default item rarity value, if it already exists somewhere, otherwise use the default value, or else use the value set in the config
                                newItem.rarity = defaultRarities.GetValueOrDefault(item.item, 0);
                                item.rarity.Set(ref newItem.rarity);
                                if (newItem.rarity > 0)
                                {
                                    item.conductive.Set(ref newItem.spawnableItem.isConductiveMetal);
                                    if (item is IScrappableConfiguration)
                                    {
                                        IScrappableConfiguration scrapItem = item as IScrappableConfiguration;

                                        int minValue = newItem.spawnableItem.minValue;
                                        int maxValue = newItem.spawnableItem.maxValue;

                                        scrapItem.minValue.Set(ref minValue);
                                        scrapItem.maxValue.Set(ref maxValue);

                                        newItem.spawnableItem.minValue = Math.Min(minValue, maxValue);
                                        // Add 1 since the random method is upper bound exclusive
                                        newItem.spawnableItem.maxValue = Math.Max(minValue, maxValue) + 1;
                                    }
                                    newLevel.spawnableScrap.Add(newItem);
                                }
                            }
                        }
                    }

                    {
                        if (state.getValidTrapConfiguration(out TrapConfiguration configuration))
                        {
                            Plugin.LETHAL_LOGGER.LogInfo("Changing interior trap spawn amounts");
                            Dictionary<GameObject, AnimationCurve> defaultSpawnableMapObjects = new Dictionary<GameObject, AnimationCurve>();
                            foreach (SpawnableMapObject obj in newLevel.spawnableMapObjects)
                            {
                                defaultSpawnableMapObjects.TryAdd(obj.prefabToSpawn, obj.numberToSpawn);
                            }
                            List<SpawnableMapObject> newMapObjects = new List<SpawnableMapObject>();
                            foreach (var item in configuration.traps)
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
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
        [HarmonyPriority(200)]
        [HarmonyPrefix]
        private static void onGenerateNewFloorPrefix(RoundManager __instance)
        {
            {
                Plugin.LETHAL_LOGGER.LogInfo("Inserting missing dungeon flows into the RoundManager");
                // Not very good, but for each dungeon flow, add it to the RoundManager if it isn't already there
                // Only add dungeon flows whos default rarity is greater than 0, so if the user doesn't enable
                // any custom flows, they won't get added
                List<DungeonFlow> flows = __instance.dungeonFlowTypes.ToList();
                DungeonFlow[] allDungeonFlows = Resources.FindObjectsOfTypeAll<DungeonFlow>();
                foreach (DungeonFlow flow in allDungeonFlows)
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

                    bool used = false;
                    foreach (LevelConfiguration levelConfig in Plugin.INSTANCE.configuration.levelConfigs.Values)
                    {
                        // Check if the rarity for this flow is set in any moons, and if so, then add it to the array of dungeon flows
                        if (levelConfig.dungeon.enabled.Value && !levelConfig.dungeon.dungeonFlowConfigurations[flow.name].rarity.isUnset())
                        {
                            used = true;
                            break;
                        }
                    }
                    // Otherwise check if it is enabled in the global config
                    if (!used && Plugin.INSTANCE.configuration.dungeonConfiguration.enabled.Value)
                    {
                        if (!Plugin.INSTANCE.configuration.dungeonConfiguration.dungeonFlowConfigurations[flow.name].rarity.isUnset())
                        {
                            used = true;
                        }
                    }

                    if (index == -1 && used)
                    {
                        // Not added, so add it now
                        Plugin.LETHAL_LOGGER.LogWarning($"Did not find dungeon flow {flow.name} in the global list of dungeon flows. Adding it now.");
                        flows.Add(flow);
                    }
                }
                __instance.dungeonFlowTypes = flows.ToArray();
            }

            SelectableLevel level = __instance.currentLevel;
            RoundState state = Plugin.getRoundState(level);
            if (state != null)
            {
                if (state.getValidDungeonGenerationConfiguration(out DungeonGenerationConfiguration configuration))
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing dungeon flow values");
                    configuration.mapSizeMultiplier.Set(ref __instance.mapSizeMultiplier);

                    Dictionary<string, int> flows = new Dictionary<string, int>();
                    foreach (IntWithRarity entry in level.dungeonFlowTypes)
                    {
                        flows.TryAdd(__instance.dungeonFlowTypes[entry.id].name, entry.rarity);
                    }
                    foreach (var item in configuration.dungeonFlowConfigurations)
                    {
                        string name = item.Key;
                        DungeonFlowConfiguration config = item.Value;

                        int originalRarity = flows.GetValueOrDefault(name, 0);
                        config.rarity.Set(ref originalRarity);
                        flows[name] = originalRarity;

                        Plugin.LETHAL_LOGGER.LogInfo($"Set dungeon flow rarity for {name} to {originalRarity}");
                    }
                    level.dungeonFlowTypes = __instance.ConvertToDungeonFlowArray(flows);
                }
            }
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
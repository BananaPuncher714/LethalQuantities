using DunGen.Graph;
using HarmonyLib;
using LethalQuantities.Json;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
                try
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

                        globalInfo.allSelectableLevels.Add(SelectableLevelCache.getGuid(level), genericInfo);
                    }

                    Plugin.LETHAL_LOGGER.LogInfo("Fetching all traps");
                    Dictionary<GameObject, DirectionalSpawnableMapObject> uniqueMapObjects = new Dictionary<GameObject, DirectionalSpawnableMapObject>();
                    // Keep track of added objects, try to make sure we don't add the same one twice
                    HashSet<string> addedTraps = new HashSet<string>();
                    foreach (Guid levelGuid in globalInfo.allSelectableLevels.Keys)
                    {
                        SelectableLevel level = SelectableLevelCache.getLevel(levelGuid);
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

                    // Create an exportable object
                    Plugin.INSTANCE.defaultInformation = new ExportData(__instance, globalInfo);
                    Plugin.INSTANCE.exportData();

                    // Load the data from file
                    Plugin.INSTANCE.loadData(globalInfo);
                }
                catch (Exception e)
                {
                    Plugin.LETHAL_LOGGER.LogError("Encountered an error while trying to load the configuration");
                    Plugin.LETHAL_LOGGER.LogError($"Please report this error to the mod developers: {e}");
                }
            }
        }

        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPostfix]
        private static void onStartPostfix(RoundManager __instance)
        {
            StartOfRoundPatch.updateMoonPrices(StartOfRound.Instance.currentLevel);

            // All levels should have been fully loaded by now, so remove any unused idenfitiers
            SelectableLevelCache.cleanIdentifiers();
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        // Keep it at an acceptable priority level, so that if some other mod has an urgent need to modify anything before us, they can
        [HarmonyPriority(100)]
        [HarmonyPrefix]
        // Attempt to set the various level variables before other mods, since this should serve as the "base" for level information.
        // Values set by this mod are meant to be overwritten, especially by mods that add events, or varying changes.
        private static void onLoadNewLevelPrefix(RoundManager __instance, ref SelectableLevel newLevel)
        {
            try
            {
                RoundState state = Plugin.getRoundState(newLevel);
                if (state != null)
                {
                    if (__instance.IsServer)
                    {
                        LevelPreset preset = state.preset;


                        Plugin.LETHAL_LOGGER.LogInfo($"RoundState found for level {state.level.name}, modifying level before loading");
                        int minimumSpawnProbabilityRange = (int)Math.Ceiling(Math.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 3.2);
                        {
                            Plugin.LETHAL_LOGGER.LogInfo("Changing inside enemy values");
                            preset.maxPowerCount.update(ref newLevel.maxEnemyPowerCount);
                            preset.spawnCurve.update(ref newLevel.enemySpawnChanceThroughoutDay);
                            preset.spawnProbabilityRange.update(ref newLevel.spawnProbabilityRange);
                            if (newLevel.spawnProbabilityRange < minimumSpawnProbabilityRange)
                            {
                                Plugin.LETHAL_LOGGER.LogWarning($"Interior enemy spawn amount range is too small({newLevel.spawnProbabilityRange}), setting to {minimumSpawnProbabilityRange}");
                                newLevel.spawnProbabilityRange = minimumSpawnProbabilityRange;
                            }
                            newLevel.Enemies.Clear();
                            newLevel.Enemies.AddRange(state.enemies);

                            Plugin.LETHAL_LOGGER.LogInfo("Changing daytime enemy values");
                            preset.maxDaytimePowerCount.update(ref newLevel.maxDaytimeEnemyPowerCount);
                            preset.daytimeSpawnCurve.update(ref newLevel.daytimeEnemySpawnChanceThroughDay);
                            preset.daytimeSpawnProbabilityRange.update(ref newLevel.daytimeEnemiesProbabilityRange);
                            if (newLevel.daytimeEnemiesProbabilityRange < minimumSpawnProbabilityRange)
                            {
                                Plugin.LETHAL_LOGGER.LogWarning($"Interior enemy spawn amount range is too small({newLevel.daytimeEnemiesProbabilityRange}), setting to {minimumSpawnProbabilityRange}");
                                newLevel.daytimeEnemiesProbabilityRange = minimumSpawnProbabilityRange;
                            }
                            newLevel.DaytimeEnemies.Clear();
                            newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);

                            Plugin.LETHAL_LOGGER.LogInfo("Changing outside enemy values");
                            preset.maxOutsidePowerCount.update(ref newLevel.maxOutsideEnemyPowerCount);
                            preset.outsideSpawnCurve.update(ref newLevel.outsideEnemySpawnChanceThroughDay);
                            newLevel.OutsideEnemies.Clear();
                            newLevel.OutsideEnemies.AddRange(state.outsideEnemies);

                            Plugin.LETHAL_LOGGER.LogInfo("Changing scrap values");
                            preset.minScrap.update(ref newLevel.minScrap);
                            preset.maxScrap.update(ref newLevel.maxScrap);
                            preset.scrapAmountMultiplier.update(ref __instance.scrapAmountMultiplier);
                            preset.scrapValueMultiplier.update(ref __instance.scrapValueMultiplier);

                            preset.mapSizeMultiplier.update(ref newLevel.factorySizeMultiplier);
                        }

                        {
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
                            if (preset.scrap.isSet())
                            {
                                foreach (var entry in preset.scrap.value)
                                {
                                    Item item = entry.Key;
                                    LevelPresetItem itemPreset = entry.Value;

                                    SpawnableItemWithRarity newItem = new SpawnableItemWithRarity();
                                    newItem.spawnableItem = item;
                                    // Get the default item rarity value, if it already exists somewhere, otherwise use the default value, or else use the value set in the config
                                    newItem.rarity = defaultRarities.GetValueOrDefault(item, 0);
                                    itemPreset.rarity.update(ref newItem.rarity);
                                    if (newItem.rarity > 0)
                                    {
                                        itemPreset.conductive.update(ref item.isConductiveMetal);
                                        itemPreset.weight.update(ref item.weight);

                                        int minValue = item.minValue;
                                        int maxValue = item.maxValue;

                                        itemPreset.minValue.update(ref minValue);
                                        itemPreset.maxValue.update(ref maxValue);

                                        item.minValue = Math.Min(minValue, maxValue);
                                        // Add 1 since the random method is upper bound exclusive
                                        item.maxValue = Math.Max(minValue, maxValue);
                                        newLevel.spawnableScrap.Add(newItem);
                                    }
                                }
                            }
                        }

                        {
                            if (preset.traps.isSet())
                            {
                                Plugin.LETHAL_LOGGER.LogInfo("Changing interior trap spawn amounts");
                                Dictionary<GameObject, AnimationCurve> defaultSpawnableMapObjects = new Dictionary<GameObject, AnimationCurve>();
                                foreach (SpawnableMapObject obj in newLevel.spawnableMapObjects)
                                {
                                    defaultSpawnableMapObjects.TryAdd(obj.prefabToSpawn, obj.numberToSpawn);
                                }
                                List<SpawnableMapObject> newMapObjects = new List<SpawnableMapObject>();
                                foreach (var entry in preset.traps.value)
                                {
                                    DirectionalSpawnableMapObject obj = entry.Key;
                                    LevelPresetTrap presetTrap = entry.Value;

                                    AnimationCurve curve = defaultSpawnableMapObjects.GetValueOrDefault(obj.obj, new AnimationCurve(new Keyframe(0, 0)));
                                    presetTrap.spawnCurve.update(ref curve);

                                    SpawnableMapObject spawnableObj = new SpawnableMapObject();
                                    spawnableObj.prefabToSpawn = obj.obj;
                                    spawnableObj.numberToSpawn = curve;
                                    spawnableObj.spawnFacingAwayFromWall = obj.faceAwayFromWall;

                                    newMapObjects.Add(spawnableObj);
                                }
                                newLevel.spawnableMapObjects = newMapObjects.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.LETHAL_LOGGER.LogError("Encountered an error while trying to modify the level");
                Plugin.LETHAL_LOGGER.LogError($"Please report this error to the mod developers: {e}");
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
        [HarmonyPriority(200)]
        [HarmonyPrefix]
        private static void onGenerateNewFloorPrefix(RoundManager __instance)
        {
            DungeonFlow[] allDungeonFlows = Resources.FindObjectsOfTypeAll<DungeonFlow>();
            {
                Plugin.LETHAL_LOGGER.LogInfo("Inserting missing dungeon flows into the RoundManager");
                // Not very good, but for each dungeon flow, add it to the RoundManager if it isn't already there
                // Only add dungeon flows whos default rarity is greater than 0, so if the user doesn't enable
                // any custom flows, they won't get added
                List<DungeonFlow> flows = __instance.dungeonFlowTypes.ToList();
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
                        if (levelConfig.dungeon.enabled.Value)
                        {
                            if (levelConfig.dungeon.dungeonFlowConfigurations.TryGetValue(flow.name, out DungeonFlowConfiguration config))
                            {
                                if (!config.rarity.isUnset())
                                {
                                    used = true;
                                    break;
                                }
                            }
                        }
                    }
                    // Otherwise check if it is enabled in the global config
                    if (!used && Plugin.INSTANCE.configuration.dungeonConfiguration.enabled.Value)
                    {
                        if (Plugin.INSTANCE.configuration.dungeonConfiguration.dungeonFlowConfigurations.TryGetValue(flow.name, out DungeonFlowConfiguration config))
                        {
                            if (!config.rarity.isUnset())
                            {
                                used = true;
                            }
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
                LevelPreset preset = state.preset;
                if (preset.dungeonFlows.isSet())
                {
                    Plugin.LETHAL_LOGGER.LogInfo("Changing dungeon flow values");
                    Dictionary<string, int> flows = new Dictionary<string, int>();
                    foreach (IntWithRarity entry in level.dungeonFlowTypes)
                    {
                        flows.TryAdd(__instance.dungeonFlowTypes[entry.id].name, entry.rarity);
                    }

                    Dictionary<string, int> updateFlows = new Dictionary<string, int>();
                    foreach (var entry in preset.dungeonFlows.value)
                    {
                        string name = entry.Key;
                        LevelPresetDungeonFlow presetFlow = entry.Value;

                        int rarity = flows.GetValueOrDefault(name, 0);
                        presetFlow.rarity.update(ref rarity);
                        updateFlows.TryAdd(name, rarity);

                        Plugin.LETHAL_LOGGER.LogInfo($"Set dungeon flow rarity for {name} to {rarity}");
                    }
                    level.dungeonFlowTypes = __instance.ConvertToDungeonFlowArray(updateFlows);
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
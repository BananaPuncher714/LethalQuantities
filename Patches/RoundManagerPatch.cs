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
                    MiniLogger.LogInfo("Fetching all enemy types");
                    HashSet<string> addedEnemyTypes = new HashSet<string>();
                    globalInfo.allEnemyTypes.AddRange(Resources.FindObjectsOfTypeAll<EnemyType>().Where(type =>
                    {
                        if (type.enemyPrefab == null)
                        {
                            //MiniLogger.LogWarning($"Enemy type {type.name} is missing prefab! Perhaps another mod has removed it? Some default values in the config may not be correct.");
                        }
                        else if (!manager.NetworkConfig.Prefabs.Contains(type.enemyPrefab))
                        {
                            //MiniLogger.LogWarning($"Enemy type {type.name} is not a real object! Ignoring type");
                        }
                        else if (addedEnemyTypes.Contains(type.name))
                        {
                            //MiniLogger.LogWarning($"Enemy type {type.name} was found twice! Perhaps another mod has added it? Some default values in the config may not be correct.");
                        }
                        else
                        {
                            addedEnemyTypes.Add(type.name);
                            return true;
                        }
                        return false;
                    }));
                    MiniLogger.LogInfo("Fetching all items");
                    HashSet<string> addedItems = new HashSet<string>();
                    globalInfo.allItems.AddRange(Resources.FindObjectsOfTypeAll<Item>().Where(type =>
                    {
                        if (type.spawnPrefab == null)
                        {
                            //MiniLogger.LogWarning($"Item {type.name} is missing prefab! Perhaps another mod has removed it? Some default values in the config may not be correct.");
                        }
                        else if (!manager.NetworkConfig.Prefabs.Contains(type.spawnPrefab))
                        {
                            //MiniLogger.LogWarning($"Item {type.name} is not a real item! Ignoring item");
                        }
                        else if (addedItems.Contains(type.name))
                        {
                            //MiniLogger.LogWarning($"Item {type.name} was found twice! Perhaps another mod has added it? Some default values in the config may not be correct.");
                        }
                        else
                        {
                            addedItems.Add(type.name);
                            return true;
                        }
                        return false;
                    }));

                    MiniLogger.LogInfo("Fetching all moon prices");
                    CompatibleNoun[] nouns = Resources.FindObjectsOfTypeAll<TerminalKeyword>().FirstOrDefault(w => w.name == "Route")?.compatibleNouns;
                    foreach (SelectableLevel level in Resources.FindObjectsOfTypeAll<SelectableLevel>())
                    {
                        GenericLevelInformation genericInfo = new GenericLevelInformation();
                        if (nouns != null)
                        {
                            bool found = false;
                            foreach (CompatibleNoun noun in nouns)
                            {
                                TerminalNode result = noun.result;
                                if (result.terminalOptions == null)
                                {
                                    MiniLogger.LogError($"Route subcommand {result.name} does not have any valid terminal options!");
                                    continue;
                                }

                                CompatibleNoun confirmNoun = result.terminalOptions.FirstOrDefault(n => n.noun.name == "Confirm");
                                if (confirmNoun == null)
                                {
                                    MiniLogger.LogError($"Unable to find a confirm option for route command {result.name}");
                                    continue;
                                }
                                TerminalNode confirm = confirmNoun.result;

                                if (confirm == null)
                                {
                                    MiniLogger.LogError($"Found a confirm option for route command {result.name}, but it has no result node!");
                                    continue;
                                }

                                int levelId = confirm.buyRerouteToMoon;

                                if (level.levelID == confirm.buyRerouteToMoon)
                                {
                                    MiniLogger.LogInfo($"Found the price {confirm.itemCost} for {level.name}");
                                    genericInfo.price = confirm.itemCost;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                MiniLogger.LogWarning($"Unable to find price of {level.name}({level.PlanetName})");
                            }
                        }
                        else
                        {
                            MiniLogger.LogError("Unable to find Route TerminalKeyword! Cannot fetch moon prices");
                        }

                        if (!globalInfo.allSelectableLevels.TryAdd(SelectableLevelCache.getGuid(level), genericInfo))
                        {
                            MiniLogger.LogWarning($"Attempted to set the price for {level} with the price {genericInfo.price} again");
                        }
                    }

                    MiniLogger.LogInfo("Fetching all traps");
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

                    MiniLogger.LogInfo("Fetching all dungeon flows");
                    globalInfo.allDungeonFlows.AddRange(Resources.FindObjectsOfTypeAll<DungeonFlow>());
                    globalInfo.manager = __instance;
                    globalInfo.sortData();

                    GlobalConfiguration configuration;
                    if (!Plugin.INSTANCE.configInitialized)
                    {
                        // The purpose for this segment below is to save the vanilla information, before any mods change any values
                        MiniLogger.LogInfo("Loading global configuration");
                        configuration = new GlobalConfiguration(globalInfo);

                        Plugin.INSTANCE.configuration = configuration;
                        Plugin.INSTANCE.configInitialized = true;

                        MiniLogger.LogInfo("Done configuring LethalQuantities");
                    }
                    else
                    {
                        configuration = Plugin.INSTANCE.configuration;
                    }

                    // Set some global options here
                    if (configuration.scrapConfiguration.enabled.Value)
                    {
                        MiniLogger.LogInfo("Setting custom item weight values");
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
                    MiniLogger.LogError("Encountered an error while trying to load the configuration");
                    MiniLogger.LogError($"Please report this error to the LethalQuantities mod developers: {e}");
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
        [HarmonyPriority(800)]
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
                    LevelPreset preset = state.getPreset();
                    // Don't need to worry about modifying these things client side only, since it probably hurts more not to
                    //if (__instance.IsServer)
                    {
                        MiniLogger.LogInfo($"RoundState found for level {state.level.name}, modifying level before loading");
                        int minimumSpawnProbabilityRange = (int)Math.Ceiling(Math.Abs(TimeOfDay.Instance.daysUntilDeadline - 3) / 3.2);
                        {
                            MiniLogger.LogInfo("Changing inside enemy values");
                            preset.maxPowerCount.update(ref newLevel.maxEnemyPowerCount);
                            preset.spawnCurve.update(ref newLevel.enemySpawnChanceThroughoutDay);
                            preset.spawnProbabilityRange.update(ref newLevel.spawnProbabilityRange);
                            if (newLevel.spawnProbabilityRange < minimumSpawnProbabilityRange)
                            {
                                MiniLogger.LogWarning($"Interior enemy spawn amount range is too small({newLevel.spawnProbabilityRange}), setting to {minimumSpawnProbabilityRange}");
                                newLevel.spawnProbabilityRange = minimumSpawnProbabilityRange;
                            }
                            if (preset.enemies.set)
                            {
                                newLevel.Enemies.Clear();
                                newLevel.Enemies.AddRange(state.enemies);
                            }

                            MiniLogger.LogInfo("Changing daytime enemy values");
                            preset.maxDaytimePowerCount.update(ref newLevel.maxDaytimeEnemyPowerCount);
                            preset.daytimeSpawnCurve.update(ref newLevel.daytimeEnemySpawnChanceThroughDay);
                            preset.daytimeSpawnProbabilityRange.update(ref newLevel.daytimeEnemiesProbabilityRange);
                            if (newLevel.daytimeEnemiesProbabilityRange < minimumSpawnProbabilityRange)
                            {
                                MiniLogger.LogWarning($"Daytime enemy spawn amount range is too small({newLevel.daytimeEnemiesProbabilityRange}), setting to {minimumSpawnProbabilityRange}");
                                newLevel.daytimeEnemiesProbabilityRange = minimumSpawnProbabilityRange;
                            }
                            if (preset.daytimeEnemies.set)
                            {
                                newLevel.DaytimeEnemies.Clear();
                                newLevel.DaytimeEnemies.AddRange(state.daytimeEnemies);
                            }

                            MiniLogger.LogInfo("Changing outside enemy values");
                            preset.maxOutsidePowerCount.update(ref newLevel.maxOutsideEnemyPowerCount);
                            preset.outsideSpawnCurve.update(ref newLevel.outsideEnemySpawnChanceThroughDay);
                            if (preset.outsideEnemies.set)
                            {
                                newLevel.OutsideEnemies.Clear();
                                newLevel.OutsideEnemies.AddRange(state.outsideEnemies);
                            }

                            MiniLogger.LogInfo("Changing scrap values");
                            preset.minScrap.update(ref newLevel.minScrap);
                            preset.maxScrap.update(ref newLevel.maxScrap);
                            preset.scrapAmountMultiplier.update(ref __instance.scrapAmountMultiplier);
                            preset.scrapValueMultiplier.update(ref __instance.scrapValueMultiplier);
                        }

                        if (preset.scrap.isSet())
                        {
                            Dictionary<Item, int> defaultRarities = new Dictionary<Item, int>();
                            foreach (SpawnableItemWithRarity item in newLevel.spawnableScrap)
                            {
                                if (!defaultRarities.TryAdd(item.spawnableItem, item.rarity))
                                {
                                    MiniLogger.LogWarning($"{newLevel.name} has a duplicate spawnable scrap item: {item.spawnableItem.name}");
                                    defaultRarities[item.spawnableItem] += item.rarity;
                                }
                            }

                            List<SpawnableItemWithRarity> spawnableItems = new List<SpawnableItemWithRarity>();
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
                                    spawnableItems.Add(newItem);
                                }
                            }
                            if (spawnableItems.Count > 0)
                            {
                                newLevel.spawnableScrap.Clear();
                                newLevel.spawnableScrap.AddRange(spawnableItems);
                            }
                            else
                            {
                                MiniLogger.LogWarning($"Preset for level {newLevel.name} has no scrap assigned! No changes have been applied.");
                            }
                        }

                        if (preset.traps.isSet())
                        {
                            MiniLogger.LogInfo("Changing interior trap spawn amounts");
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

                    preset.mapSizeMultiplier.update(ref newLevel.factorySizeMultiplier);
                }
            }
            catch (Exception e)
            {
                MiniLogger.LogError("Encountered an error while trying to modify the level");
                MiniLogger.LogError($"Please report this error to the LethalQuantities mod developers: {e}");
            }
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewFloor))]
        [HarmonyPriority(200)]
        [HarmonyPrefix]
        private static void onGenerateNewFloorPrefix(RoundManager __instance)
        {
            DungeonFlow[] allDungeonFlows = Resources.FindObjectsOfTypeAll<DungeonFlow>();
            try
            {
                MiniLogger.LogInfo("Inserting missing dungeon flows into the RoundManager");
                // Not very good, but for each dungeon flow, add it to the RoundManager if it isn't already there
                // Only add dungeon flows whos default rarity is greater than 0, so if the user doesn't enable
                // any custom flows, they won't get added
                List<IndoorMapType> flows = __instance.dungeonFlowTypes.ToList();
                foreach (DungeonFlow flow in allDungeonFlows)
                {
                    int index = -1;
                    for (int i = 0; i < flows.Count; i++)
                    {
                        if (flows[i].dungeonFlow == flow)
                        {
                            index = i;
                            break;
                        }
                    }

                    bool used = false;
                    foreach (LevelPreset preset in Plugin.INSTANCE.presets.Values)
                    {
                        if (preset.dungeonFlows.isSet())
                        {
                            foreach (string presetName in preset.dungeonFlows.value.Keys)
                            {
                                if (presetName == flow.name)
                                {
                                    used = true;
                                    goto usedEnd;
                                }
                            }
                        }
                    }
                    usedEnd:

                    if (index == -1 && used)
                    {
                        // Not added, so add it now
                        MiniLogger.LogWarning($"Did not find dungeon flow {flow.name} in the global list of dungeon flows. Adding it now.");
                        flows.Add(flows[index]);
                    }
                }
                __instance.dungeonFlowTypes = flows.ToArray();
            }
            catch (Exception e)
            {
                MiniLogger.LogError("Encountered an error while trying to insert missing dungeonflows");
                MiniLogger.LogError($"Please report this error to the LethalQuantities mod developers: {e}");
            }

            try
            {
                SelectableLevel level = __instance.currentLevel;
                RoundState state = Plugin.getRoundState(level);
                if (state != null)
                {
                    LevelPreset preset = state.getPreset();
                    if (preset.dungeonFlows.isSet())
                    {
                        MiniLogger.LogInfo("Changing dungeon flow values");
                        Dictionary<string, int> flows = new Dictionary<string, int>();
                        foreach (IntWithRarity entry in level.dungeonFlowTypes)
                        {
                            flows.TryAdd(__instance.dungeonFlowTypes[entry.id].dungeonFlow.name, entry.rarity);
                        }

                        Dictionary<string, int> updateFlows = new Dictionary<string, int>();
                        foreach (var entry in preset.dungeonFlows.value)
                        {
                            string name = entry.Key;
                            LevelPresetDungeonFlow presetFlow = entry.Value;

                            int rarity = flows.GetValueOrDefault(name, 0);
                            presetFlow.rarity.update(ref rarity);
                            updateFlows.TryAdd(name, rarity);

                            MiniLogger.LogInfo($"Set dungeon flow rarity for {name} to {rarity}");
                        }
                        level.dungeonFlowTypes = __instance.ConvertToDungeonFlowArray(updateFlows);
                        preset.mapSizeMultiplier.update(ref level.factorySizeMultiplier);
                    }
                }
            }
            catch (Exception e)
            {
                MiniLogger.LogError("Encountered an error while trying to update level dungeonflows");
                MiniLogger.LogError($"Please report this error to the LethalQuantities mod developers: {e}");
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
                        if (manager.dungeonFlowTypes[i].dungeonFlow.name == entry.Key)
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
                        MiniLogger.LogWarning($"Could not find dungeon flow {entry.Key}!");
                    }
                }
            }

            return res.ToArray();
        }
    }
}
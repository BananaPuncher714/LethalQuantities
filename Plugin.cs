using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using LethalQuantities.Objects;
using System.IO;
using LethalQuantities.Patches;
using DunGen.Graph;
using System.Linq;
using Unity.Netcode;

namespace LethalQuantities
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static readonly string GLOBAL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Global");
        internal static readonly string LEVEL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Moons");

        public static Plugin INSTANCE { get; private set; }

        public static ManualLogSource LETHAL_LOGGER { get; private set; }

        internal GlobalConfiguration configuration;

        private Harmony _harmony;
        internal bool configInitialized = false;

        private void Awake()
        {
            if (INSTANCE != null && INSTANCE != this)
            {
                Destroy(this);
            }
            else
            {
                INSTANCE = this;
            }

            LETHAL_LOGGER = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

            TomlTypeConverter.AddConverter(typeof(AnimationCurve), new AnimationCurveTypeConverter());

            _harmony = new Harmony("LethalQuantities");
            _harmony.PatchAll(typeof(RoundManagerPatch));
            _harmony.PatchAll(typeof(ObjectPatch));
            _harmony.PatchAll(typeof(DungeonPatch));
            _harmony.PatchAll(typeof(ConfigEntryBasePatch));

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "SampleSceneRelay" && !configInitialized)
            {
                // The purpose for this segment below is to save the vanilla information, before any mods change any values
                StartOfRound instance = StartOfRound.Instance;
                GlobalInformation globalInfo = new GlobalInformation(GLOBAL_SAVE_DIR, LEVEL_SAVE_DIR);

                // Get all enemy, item and dungeon flows
                // Filter out any potentially "fake" enemy types that might have been added by other mods
                HashSet<string> addedEnemyTypes = new HashSet<string>();
                globalInfo.allEnemyTypes.AddRange(Resources.FindObjectsOfTypeAll<EnemyType>().Where(type => {
                    if (type.enemyPrefab == null)
                    {
                        LETHAL_LOGGER.LogWarning($"Enemy type {type.name} is missing prefab! Perhaps another mod has removed it? Some default values in the config may not be correct.");
                    }
                    else if (addedEnemyTypes.Contains(type.name))
                    {
                        LETHAL_LOGGER.LogWarning($"Enemy type {type.name} was found twice! Perhaps another mod has added it? Some default values in the config may not be correct.");
                    }
                    else
                    {
                        addedEnemyTypes.Add(type.name);
                        return true;
                    }
                    return false;
                }));
                HashSet<string> addedItems = new HashSet<string>();
                globalInfo.allItems.AddRange(Resources.FindObjectsOfTypeAll<Item>().Where(type => {
                    if (type.spawnPrefab == null)
                    {
                        LETHAL_LOGGER.LogWarning($"Item {type.name} is missing prefab! Perhaps another mod has removed it? Some default values in the config may not be correct.");
                    }
                    else if (addedItems.Contains(type.name))
                    {
                        LETHAL_LOGGER.LogWarning($"Item {type.name} was found twice! Perhaps another mod has added it? Some default values in the config may not be correct.");
                    }
                    else
                    {
                        addedItems.Add(type.name);
                        return true;
                    }
                    return false;
                }));

                globalInfo.allSelectableLevels.AddRange(Resources.FindObjectsOfTypeAll<SelectableLevel>());
                Dictionary<GameObject, DirectionalSpawnableMapObject> uniqueMapObjects = new Dictionary<GameObject, DirectionalSpawnableMapObject>();
                // Keep track of added objects, try to make sure we don't add the same one twice
                HashSet<string> addedTraps = new HashSet<string>();
                foreach (SelectableLevel level in globalInfo.allSelectableLevels)
                {
                    foreach (SpawnableMapObject spawnableObject in level.spawnableMapObjects)
                    {
                        GameObject prefab = spawnableObject.prefabToSpawn;
                        if (!addedTraps.Contains(prefab.name))
                        {
                            // Only add the prefab if it looks like a real game object
                            if (prefab.GetComponent<NetworkObject>() != null)
                            {
                                addedTraps.Add(prefab.name);
                                uniqueMapObjects.TryAdd(prefab, new DirectionalSpawnableMapObject(prefab, spawnableObject.spawnFacingAwayFromWall));
                            }
                        }
                    }
                }
                globalInfo.allSpawnableMapObjects.AddRange(uniqueMapObjects.Values);

                globalInfo.allDungeonFlows.AddRange(Resources.FindObjectsOfTypeAll<DungeonFlow>());
                globalInfo.sortData();

                LETHAL_LOGGER.LogInfo("Loading global configuration");
                configuration = new GlobalConfiguration(globalInfo);

                configInitialized = true;

                LETHAL_LOGGER.LogInfo("Inserting missing dungeon flows into the RoundManager");
                // Not very good, but for each dungeon flow, add it to the RoundManager if it isn't already there
                // Only add dungeon flows whos default rarity is greater than 0, so if the user doesn't enable
                // any custom flows, they won't get added
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

                    bool used = false;
                    foreach (LevelConfiguration levelConfig in configuration.levelConfigs.Values)
                    {
                        // Check if the rarity for this flow is set in any moons, and if so, then add it to the array of dungeon flows
                        if (levelConfig.dungeon.enabled.Value && levelConfig.dungeon.dungeonFlowConfigurations[flow.name].rarity.isUnset())
                        {
                            used = true;
                            break;
                        }
                    }
                    if (!used && configuration.dungeonConfiguration.enabled.Value)
                    {
                        if (!configuration.dungeonConfiguration.dungeonFlowConfigurations[flow.name].rarity.isDefault())
                        {
                            used = true;
                        }
                    }

                    if (index == -1 && used)
                    {
                        // Not added, so add it now
                        LETHAL_LOGGER.LogWarning($"Did not find dungeon flow {flow.name} in the global list of dungeon flows. Adding it now.");
                        flows.Add(flow);
                    }
                }
                RoundManager.Instance.dungeonFlowTypes = flows.ToArray();

                // Set some global options here
                if (configuration.scrapConfiguration.enabled.Value)
                {
                    LETHAL_LOGGER.LogInfo("Setting custom item weight values");
                    foreach (Item item in globalInfo.allItems)
                    {
                        GlobalItemConfiguration itemConfig = configuration.scrapConfiguration.itemConfigurations[item];
                        itemConfig.weight.Set(ref item.weight);
                    }
                }
                LETHAL_LOGGER.LogInfo("Done configuring LethalQuantities");
            }
            else
            {
                if (RoundManager.Instance != null && RoundManager.Instance.IsServer)
                {
                    SelectableLevel level = RoundManager.Instance.currentLevel;
                    if (level != null && configuration.levelConfigs.ContainsKey(RoundManager.Instance.currentLevel.name))
                    {
                        LETHAL_LOGGER.LogInfo($"Found level {level.name}, modifying enemy spawns");
                        // Add a manager to keep track of all objects
                        GameObject levelModifier = new GameObject("LevelModifier");
                        SceneManager.MoveGameObjectToScene(levelModifier, scene);

                        RoundState state = levelModifier.AddComponent<RoundState>();
                        state.plugin = this;

                        state.setData(scene, configuration);
                        state.initialize(level);
                    }
                }
            }
        }
    }
}
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

namespace LethalQuantities
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static readonly string GLOBAL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Global");
        private static readonly string LEVEL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Moons");

        public static Plugin INSTANCE { get; private set; }

        public static ManualLogSource LETHAL_LOGGER { get; private set; }

        private GlobalConfiguration configuration;

        private Harmony _harmony;
        private bool configInitialized = false;

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

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "SampleSceneRelay" && !configInitialized)
            {
                StartOfRound instance = StartOfRound.Instance;
                GlobalInformation globalInfo = new GlobalInformation(GLOBAL_SAVE_DIR, LEVEL_SAVE_DIR);

                // Get all enemy and item types
                globalInfo.allEnemyTypes.UnionWith(Resources.FindObjectsOfTypeAll<EnemyType>());
                globalInfo.allItems.UnionWith(Resources.FindObjectsOfTypeAll<Item>());
                globalInfo.allSelectableLevels.UnionWith(instance.levels);
                globalInfo.allDungeonFlows.UnionWith(Resources.FindObjectsOfTypeAll<DungeonFlow>());

                configuration = new GlobalConfiguration(globalInfo);

                LETHAL_LOGGER.LogInfo("Printing out default moon info");
                foreach (var level in instance.levels)
                {
                    LETHAL_LOGGER.LogInfo("\tName: " + level.name);
                    LETHAL_LOGGER.LogInfo("\tPlanet Name: " + level.PlanetName);
                    LETHAL_LOGGER.LogInfo("\tMax enemy power count: " + level.maxEnemyPowerCount);
                    LETHAL_LOGGER.LogInfo("\tMax daytime enemy power count: " + level.maxDaytimeEnemyPowerCount);
                    LETHAL_LOGGER.LogInfo("\tMax outside enemy power count: " + level.maxOutsideEnemyPowerCount);
                    LETHAL_LOGGER.LogInfo("\tSpawn probability range: " + level.spawnProbabilityRange);
                    LETHAL_LOGGER.LogInfo("\tDaytime spawn probability range: " + level.daytimeEnemiesProbabilityRange);
                    LETHAL_LOGGER.LogInfo("\tEnemy spawn curve:");
                    PrintAnimationCurve(level.enemySpawnChanceThroughoutDay);
                    LETHAL_LOGGER.LogInfo("\tDaytime enemy spawn curve:");
                    PrintAnimationCurve(level.daytimeEnemySpawnChanceThroughDay);
                    LETHAL_LOGGER.LogInfo("\tOutside enemy spawn curve:");
                    PrintAnimationCurve(level.outsideEnemySpawnChanceThroughDay);
                    LETHAL_LOGGER.LogInfo("\tEnemy spawn info:");
                    PrintEnemySpawnTypes(level.Enemies);
                    LETHAL_LOGGER.LogInfo("\tDaytime enemy spawn info:");
                    PrintEnemySpawnTypes(level.DaytimeEnemies);
                    LETHAL_LOGGER.LogInfo("\tOutside nemy spawn info:");
                    PrintEnemySpawnTypes(level.OutsideEnemies);
                    LETHAL_LOGGER.LogInfo("\tLevel size multiplier: " + level.factorySizeMultiplier);
                    LETHAL_LOGGER.LogInfo("\tScrap item info:");
                    PrintItemTypes(level.spawnableScrap);
                }

                LETHAL_LOGGER.LogInfo("Printing out default enemy info:");
                foreach (var enemyType in globalInfo.allEnemyTypes)
                {
                    LETHAL_LOGGER.LogInfo("\tEnemy: " + enemyType.enemyName);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy max count: " + enemyType.MaxCount);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy power level: " + enemyType.PowerLevel);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy spawn curve:");
                    PrintAnimationCurve(enemyType.probabilityCurve);
                    LETHAL_LOGGER.LogInfo("\t\tEnemy spawn falloff curve: " + enemyType.useNumberSpawnedFalloff);
                    PrintAnimationCurve(enemyType.numberSpawnedFalloff);
                }

                // TODO Change max ship capacity
                // TODO 

                configInitialized = true;

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
                        flows.Add(flow);
                    }
                }
                RoundManager.Instance.dungeonFlowTypes = flows.ToArray();

                // Set some global options here
                if (configuration.scrapConfiguration.enabled.Value)
                {
                    foreach (Item item in globalInfo.allItems)
                    {
                        GlobalItemConfiguration itemConfig = configuration.scrapConfiguration.itemConfigurations[item];
                        itemConfig.weight.Set(ref item.weight);
                    }
                }
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

        static void PrintAnimationCurve(AnimationCurve curve)
        {
            int i = 0;
            foreach (var frame in curve.GetKeys())
            {
                LETHAL_LOGGER.LogInfo("\t\tFrame " + i++ + ": " + frame.m_Time + "\t" + frame.m_Value);
            }
        }

        static void PrintEnemySpawnTypes(List<SpawnableEnemyWithRarity> enemies)
        {
            foreach (var enemy in enemies)
            {
                EnemyType enemyType = enemy.enemyType;
                LETHAL_LOGGER.LogInfo("\t\tEnemy: " + enemyType.enemyName + ": " + enemy.rarity);
            }
        }

        static void PrintItemTypes(List<SpawnableItemWithRarity> items)
        {
            foreach (var item in items)
            {
                LETHAL_LOGGER.LogInfo("\t\tItem: " + item.spawnableItem + ": " + item.rarity);
            }
        }
    }
}
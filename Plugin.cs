﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using LethalQuantities.Objects;
using System.IO;
using LethalQuantities.Patches;
using LethalQuantities.Json;
using Newtonsoft.Json;

namespace LethalQuantities
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static readonly string EXPORT_DIRECTORY = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Advanced");
        internal static readonly string GLOBAL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Global");
        internal static readonly string LEVEL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Moons");

        public static Plugin INSTANCE { get; private set; }

        public static ManualLogSource LETHAL_LOGGER { get; private set; }

        internal GlobalConfiguration configuration;

        private Harmony _harmony;
        internal bool configInitialized = false;

        internal ExportData defaultInformation;

        private void Awake()
        {
            if (INSTANCE != null && INSTANCE != this)
            {
                Destroy(this);
                return;
            }
            else
            {
                INSTANCE = this;
            }

            LETHAL_LOGGER = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

            LETHAL_LOGGER.LogInfo("Registering custom TomlTypeConverter for AnimationCurve");
            TomlTypeConverter.AddConverter(typeof(AnimationCurve), new AnimationCurveTypeConverter());

            LETHAL_LOGGER.LogInfo("Registering patches...");
            _harmony = new Harmony(PluginInfo.PLUGIN_NAME);
            _harmony.PatchAll(typeof(RoundManagerPatch));
            LETHAL_LOGGER.LogInfo("Registered RoundManager patch");
            _harmony.PatchAll(typeof(ObjectPatch));
            LETHAL_LOGGER.LogInfo("Registered Object patch");
            _harmony.PatchAll(typeof(DungeonPatch));
            LETHAL_LOGGER.LogInfo("Registered RuntimeDungeon patch");
            _harmony.PatchAll(typeof(StartOfRoundPatch));
            LETHAL_LOGGER.LogInfo("Registered StartOfRound patch");
            _harmony.PatchAll(typeof(ConfigEntryBasePatch));
            LETHAL_LOGGER.LogInfo("Registered ConfigEntryBase patch");
            _harmony.PatchAll(typeof(TerminalPatch));
            LETHAL_LOGGER.LogInfo("Registered Terminal patch");

            SceneManager.sceneLoaded += OnSceneLoaded;
            LETHAL_LOGGER.LogInfo("Added sceneLoaded delegate");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LETHAL_LOGGER.LogInfo($"Checking scene {scene.name} for a valid level");
            if (RoundManager.Instance != null)
            {
                SelectableLevel level = RoundManager.Instance.currentLevel;
                if (level != null && configuration.levelConfigs.ContainsKey(RoundManager.Instance.currentLevel.getGuid()))
                {
                    foreach (RoundState oldState in FindObjectsOfType<RoundState>())
                    {
                        LETHAL_LOGGER.LogWarning($"Found stale RoundState for level {oldState.level.name}");
                        Destroy(oldState.gameObject);
                    }

                    LETHAL_LOGGER.LogInfo($"Found a valid configuration for {level.PlanetName}({level.name})");
                    // Add a manager to keep track of all objects
                    GameObject levelModifier = new GameObject("LevelModifier");
                    SceneManager.MoveGameObjectToScene(levelModifier, scene);

                    RoundState state = levelModifier.AddComponent<RoundState>();
                    state.plugin = this;

                    state.setData(scene, configuration);
                    LETHAL_LOGGER.LogInfo($"Initializing round information");
                    state.initialize(level);
                }
            }
        }

        internal void exportData()
        {
            // TODO Export the data to a file
            /*
            if (defaultInformation != null)
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AnimationCurveConverter());
                LETHAL_LOGGER.LogInfo($"Exporting default data");
                Directory.CreateDirectory(EXPORT_DIRECTORY);
                using (StreamWriter streamWriter = new StreamWriter(Path.Combine(EXPORT_DIRECTORY, "defaults.json")))
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    serializer.Serialize(writer, defaultInformation);
                }
            }
            */
        }
        internal static RoundState getRoundState(SelectableLevel level)
        {
            foreach (RoundState state in FindObjectsOfType<RoundState>())
            {
                if (state.level == level)
                {
                    return state;
                }
            }
            return null;
        }
    }
}
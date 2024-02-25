using BepInEx;
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
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static UnityEngine.UIElements.UIR.Implementation.UIRStylePainter;
using System;

namespace LethalQuantities
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static readonly string EXPORT_DIRECTORY = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Advanced");
        internal static readonly string GLOBAL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Global");
        internal static readonly string LEVEL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Moons");

        internal static readonly string PRESET_FILE = Path.Combine(Paths.ConfigPath, EXPORT_DIRECTORY, "Presets.json");
        internal static readonly string RESULT_FILE = Path.Combine(Paths.ConfigPath, EXPORT_DIRECTORY, "Results.json");

        public static Plugin INSTANCE { get; private set; }

        public static ManualLogSource LETHAL_LOGGER { get; private set; }

        internal GlobalConfiguration configuration;

        private Harmony _harmony;
        internal bool configInitialized = false;

        internal ExportData defaultInformation;
        internal ImportData importedInformation;
        internal Dictionary<Guid, LevelPreset> presets = new Dictionary<Guid, LevelPreset>();

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
            
            if (defaultInformation != null)
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AnimationCurveConverter());
                LETHAL_LOGGER.LogInfo($"Exporting default data");
                Directory.CreateDirectory(EXPORT_DIRECTORY);
                string exportPath = PRESET_FILE;
                JObject jObj;
                if (File.Exists(exportPath))
                {
                    jObj = JObject.Parse(File.ReadAllText(exportPath));
                    File.Delete(exportPath);
                }
                else
                {
                    LETHAL_LOGGER.LogInfo("No data file found, exporting global values");

                    // Export the global configuration if it exists
                    jObj = new JObject();

                    Dictionary<string, Preset> presets = new Dictionary<string, Preset>();

                    Preset globalPreset = null;
                    if (configuration.isSet())
                    {
                        // Create default presets based on each configuration
                        globalPreset = new Preset(configuration);
                        globalPreset.name = "Global";
                        globalPreset.id = "exported-Global";
                        LETHAL_LOGGER.LogInfo($"Created global preset {globalPreset.id}");

                        presets.Add(globalPreset.id, globalPreset);
                    }

                    Dictionary<string, string> levelMap = new Dictionary<string, string>();
                    foreach (var entry in configuration.levelConfigs)
                    {
                        if (entry.Value.isSet())
                        {
                            Preset levelPreset = new Preset(entry.Value);
                            levelPreset.name = $"Exported {entry.Key.getLevel().PlanetName}";
                            levelPreset.id = $"exported-{entry.Key.getLevelName()}";
                            levelPreset.parent = globalPreset == null ? "" : globalPreset.id;

                            presets.TryAdd(levelPreset.id, levelPreset);
                            levelMap.TryAdd(entry.Key.getLevelName(), levelPreset.id);

                            LETHAL_LOGGER.LogInfo($"Created level preset {levelPreset.id} for {entry.Key.getLevelName()}");
                        }
                    }

                    jObj["presets"] = JObject.FromObject(presets, serializer);
                    jObj["levels"] = JObject.FromObject(levelMap, serializer);
                }
                // Set the default information
                jObj["defaults"] = JObject.FromObject(defaultInformation, serializer);

                using (StreamWriter streamWriter = new StreamWriter(exportPath))
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    LETHAL_LOGGER.LogInfo($"Writing data to {exportPath}");
                    serializer.Serialize(writer, jObj);
                }
            }
        }

        public void loadData(List<DirectionalSpawnableMapObject> spawnableMapObjects, string path = null)
        {
            if (path == null)
            {
                path = PRESET_FILE;
            }
            if (File.Exists(path))
            {
                LETHAL_LOGGER.LogInfo($"Importing data from {path}");
                ImportData importedInformation = JsonConvert.DeserializeObject<ImportData>(File.ReadAllText(path), new AnimationCurveConverter());

                LETHAL_LOGGER.LogInfo("Generating level presets");
                presets = importedInformation.generate(spawnableMapObjects);

                LETHAL_LOGGER.LogInfo($"Saving level presets to {RESULT_FILE}");
                File.WriteAllText(RESULT_FILE, JsonConvert.SerializeObject(presets, new AnimationCurveConverter()));

                LETHAL_LOGGER.LogInfo("Done loading data");
            }
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
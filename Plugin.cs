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

        public static Plugin INSTANCE { get; private set; }

        public static ManualLogSource LETHAL_LOGGER { get; private set; }

        internal GlobalConfiguration configuration;

        private Harmony _harmony;
        internal bool configInitialized = false;

        internal ExportData defaultInformation;
        internal Dictionary<Guid, LevelPreset> presets = new Dictionary<Guid, LevelPreset>();
        internal GlobalInformation globalInfo;

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

            // Copy the editor html over to the config folder if it does not already exist
            // It's hardcoded for now because I don't know how to get the mod folder directly
            string editorFile = Path.Combine(Paths.PluginPath, "BananaPuncher714-LethalQuantities", "Editor.html");
            string dest = Path.Combine(Paths.ConfigPath, EXPORT_DIRECTORY, "Editor.html");
            if (File.Exists(editorFile) && !File.Exists(dest))
            {
                LETHAL_LOGGER.LogInfo($"Copying editor webpage from {editorFile} to {dest}");
                Directory.CreateDirectory(EXPORT_DIRECTORY);
                File.Copy(editorFile, dest);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LETHAL_LOGGER.LogInfo($"Checking scene {scene.name} for a valid level");
            if (RoundManager.Instance != null)
            {
                SelectableLevel level = RoundManager.Instance.currentLevel;
                if (level != null)
                {
                    if (presets.TryGetValue(RoundManager.Instance.currentLevel.getGuid(), out LevelPreset preset))
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

                        state.setData(scene, preset);
                        LETHAL_LOGGER.LogInfo($"Initializing round information");
                        state.initialize(level);
                    }
                    else
                    {
                        LETHAL_LOGGER.LogWarning($"No preset found for level {level.name}");
                    }
                }
            }
        }

        internal void exportData()
        {
            if (defaultInformation != null)
            {
                LETHAL_LOGGER.LogInfo($"Exporting default data");
                Directory.CreateDirectory(EXPORT_DIRECTORY);
                string exportPath = PRESET_FILE;
                JObject jObj;
                if (File.Exists(exportPath))
                {
                    jObj = JObject.Parse(File.ReadAllText(exportPath));

                    if (configuration != null && configuration.isAnySet())
                    {
                        // If the users chooses to use the legacy values, then overwrite any exported presets with the new updated exported presets
                        LETHAL_LOGGER.LogInfo($"Using legacy configuration: {configuration.useLegacy.Value}");
                        exportLegacy(jObj, configuration.useLegacy.Value);
                    }
                }
                else
                {
                    LETHAL_LOGGER.LogInfo("No data file found, exporting global values");

                    // Export the global configuration if it exists
                    jObj = new JObject();

                    exportLegacy(jObj);
                }

                // Set the default information
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AnimationCurveJsonConverter());
                jObj["defaults"] = JObject.FromObject(defaultInformation, serializer);

                LETHAL_LOGGER.LogInfo($"Writing data to {exportPath}");
                File.WriteAllText(exportPath, JsonConvert.SerializeObject(jObj));
            }
        }

        internal void exportLegacy(JObject jObj, bool replace = false)
        {
            Dictionary<string, Preset> presets = new Dictionary<string, Preset>();
            Preset globalPreset = null;
            if (configuration.isSet())
            {
                // Create default presets based on each configuration
                globalPreset = new Preset(configuration);
                globalPreset.name = "Exported Global";
                globalPreset.id = "exported-Global";
                LETHAL_LOGGER.LogInfo($"Created global preset {globalPreset.id}");

                presets.Add(globalPreset.id, globalPreset);
            }

            Dictionary<string, string> levelMap = new Dictionary<string, string>();
            if (jObj.ContainsKey("levels"))
            {
                // Get the previous level map if it exists
                levelMap = jObj["levels"].ToObject<Dictionary<string, string>>();
            }
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
                else if (globalPreset != null)
                {
                    LETHAL_LOGGER.LogInfo($"No configuration found for {entry.Key.getLevel().PlanetName}, defaulting to global");
                    levelMap.TryAdd(entry.Key.getLevelName(), globalPreset.id);
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new AnimationCurveJsonConverter());
            if (!jObj.ContainsKey("presets"))
            {
                jObj["presets"] = JObject.FromObject(presets, serializer);
            }
            else
            {
                JToken previous = jObj["presets"];
                if (previous is JObject)
                {
                    JObject previousNode = previous as JObject;
                    foreach (var entry in presets)
                    {
                        if (!previousNode.ContainsKey(entry.Key) || replace)
                        {
                            previousNode[entry.Key] = JObject.FromObject(entry.Value, serializer);
                        }
                    }
                }
                else
                {
                    LETHAL_LOGGER.LogError("Found an invalid presets node, replacing");
                    jObj["presets"] = JObject.FromObject(presets, serializer);
                }
            }
            jObj["levels"] = JObject.FromObject(levelMap, serializer);
        }

        public void loadData(GlobalInformation info, string path = null)
        {
            globalInfo = info;

            if (path == null)
            {
                path = PRESET_FILE;
            }
            if (File.Exists(path))
            {
                LETHAL_LOGGER.LogInfo($"Importing data from {path}");
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.Converters.Add(new AnimationCurveJsonConverter());
                ImportData importedInformation = JsonConvert.DeserializeObject<ImportData>(File.ReadAllText(path), settings);

                LETHAL_LOGGER.LogInfo("Generating level presets");
                presets = importedInformation.generate(info);

                JObject jObj = new JObject();
                if (File.Exists(PRESET_FILE))
                {
                    jObj = JObject.Parse(File.ReadAllText(PRESET_FILE));
                }
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AnimationCurveJsonConverter());
                serializer.Converters.Add(new DirectionalSpawnableMapObjectJsonConverter());
                serializer.Converters.Add(new EnemyTypeJsonConverter());
                serializer.Converters.Add(new ItemJsonConverter());

                jObj["results"] = JObject.FromObject(presets, serializer);
                LETHAL_LOGGER.LogInfo($"Saving level presets to {PRESET_FILE}");
                File.WriteAllText(PRESET_FILE, JsonConvert.SerializeObject(jObj));

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
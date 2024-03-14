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
using System.Linq;

namespace LethalQuantities
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static readonly string EXPORT_DIRECTORY = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Advanced");
        internal static readonly string GLOBAL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Global");
        internal static readonly string LEVEL_SAVE_DIR = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_NAME, "Moons");

        internal static readonly string PRESET_FILE_NAME = "Presets.json";
        internal static readonly string PRESET_FILE = Path.Combine(Paths.ConfigPath, EXPORT_DIRECTORY, PRESET_FILE_NAME);

        public static Plugin INSTANCE { get; private set; }

        internal GlobalConfiguration configuration;

        private Harmony _harmony;
        internal bool configInitialized = false;

        internal ExportData defaultInformation;
        internal Dictionary<Guid, LevelPreset> presets = new Dictionary<Guid, LevelPreset>();
        internal GlobalInformation globalInfo;

        internal FileSystemWatcher watcher;
        internal int ignoreCount = 0;

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

            MiniLogger.LogInfo("Registering custom TomlTypeConverter for AnimationCurve");
            TomlTypeConverter.AddConverter(typeof(AnimationCurve), new AnimationCurveTypeConverter());

            MiniLogger.LogInfo("Registering patches...");
            _harmony = new Harmony(PluginInfo.PLUGIN_NAME);
            _harmony.PatchAll(typeof(RoundManagerPatch));
            MiniLogger.LogInfo("Registered RoundManager patch");
            _harmony.PatchAll(typeof(ObjectPatch));
            MiniLogger.LogInfo("Registered Object patch");
            _harmony.PatchAll(typeof(DungeonPatch));
            MiniLogger.LogInfo("Registered RuntimeDungeon patch");
            _harmony.PatchAll(typeof(StartOfRoundPatch));
            MiniLogger.LogInfo("Registered StartOfRound patch");
            _harmony.PatchAll(typeof(ConfigEntryBasePatch));
            MiniLogger.LogInfo("Registered ConfigEntryBase patch");
            _harmony.PatchAll(typeof(TerminalPatch));
            MiniLogger.LogInfo("Registered Terminal patch");

            SceneManager.sceneLoaded += OnSceneLoaded;
            MiniLogger.LogInfo("Added sceneLoaded delegate");

            // Copy the editor html over to the config folder if it does not already exist
            string editorFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Editor.html");
            string dest = Path.Combine(Paths.ConfigPath, EXPORT_DIRECTORY, "Editor.html");
            if (File.Exists(editorFile) && !File.Exists(dest))
            {
                MiniLogger.LogInfo("Copying editor webpage to the advanced config folder");
                Directory.CreateDirectory(EXPORT_DIRECTORY);
                File.Copy(editorFile, dest);
            }

            Directory.CreateDirectory(EXPORT_DIRECTORY);
            watcher = new FileSystemWatcher();
            watcher.Path = EXPORT_DIRECTORY;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            watcher.Filter = "*.json";
            watcher.Changed += onPresetChanged;
            watcher.Deleted += onPresetDeleted;
            watcher.Renamed += onPresetRenamed;

            watcher.EnableRaisingEvents = true;
        }

        private void onPresetChanged(object source, FileSystemEventArgs ev)
        {
            // Update the presets
            if (ev.Name == PRESET_FILE_NAME)
            {
                if (ignoreCount > 0)
                {
                    ignoreCount--;
                }
                else
                {
                    MiniLogger.LogInfo("The preset file has been changed, updating...");
                    loadData(globalInfo, PRESET_FILE);
                }
            }
        }

        private void onPresetDeleted(object source, FileSystemEventArgs ev)
        {
            // Re-create the data
            if (ev.Name == PRESET_FILE_NAME)
            {
                MiniLogger.LogInfo("The preset file has been deleted, saving defaults...");
                exportData();
            }
        }

        private void onPresetRenamed(object source, RenamedEventArgs ev)
        {
            if (ev.OldName == PRESET_FILE_NAME)
            {
                MiniLogger.LogInfo("The preset file has been renamed, saving defaults...");
                exportData();
            }
            else if (ev.Name == PRESET_FILE_NAME)
            {
                MiniLogger.LogInfo("The preset file has been changed, updating...");
                loadData(globalInfo, PRESET_FILE);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            MiniLogger.LogInfo($"Checking scene {scene.name} for a valid level");
            if (RoundManager.Instance != null)
            {
                SelectableLevel level = RoundManager.Instance.currentLevel;
                if (level != null)
                {
                    Guid levelGuid = RoundManager.Instance.currentLevel.getGuid();
                    if (presets.TryGetValue(levelGuid, out LevelPreset preset))
                    {
                        foreach (RoundState oldState in FindObjectsOfType<RoundState>())
                        {
                            MiniLogger.LogWarning($"Found stale RoundState for level {oldState.level.name}");
                            Destroy(oldState.gameObject);
                        }

                        MiniLogger.LogInfo($"Found a valid configuration for {level.PlanetName}({level.name})");
                        // Add a manager to keep track of all objects
                        GameObject levelModifier = new GameObject("LevelModifier");
                        SceneManager.MoveGameObjectToScene(levelModifier, scene);

                        RoundState state = levelModifier.AddComponent<RoundState>();
                        state.plugin = this;

                        state.setData(scene, levelGuid);
                        MiniLogger.LogInfo($"Initializing round information");
                        state.initialize(level);
                    }
                    else
                    {
                        MiniLogger.LogWarning($"No preset found for level {level.name}");
                    }
                }
            }
        }

        internal void exportData()
        {
            if (defaultInformation != null)
            {
                // Disable the file watcher
                MiniLogger.LogInfo($"Exporting default data");
                Directory.CreateDirectory(EXPORT_DIRECTORY);
                string exportPath = PRESET_FILE;
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Converters.Add(new AnimationCurveJsonConverter());
                JObject jObj;

                if (File.Exists(exportPath))
                {
                    jObj = JObject.Parse(File.ReadAllText(exportPath));

                    // TODO In the case that the exported data changes(only here)
                    // then all the presets and any information in them such as enemy names, item names
                    // default moon prices, etc need to be updated since they store their
                    // information separately from the default data
                    // Could probably add references via id, but whatever, it's a low priority task
                    // Also it would probably be good to have data validation and correction here
                    // Update all preset objects

                    if (jObj.ContainsKey("presets"))
                    {
                        Dictionary<string, Preset> loadedPresets = jObj["presets"].ToObject<Dictionary<string, Preset>>(serializer);

                        // TODO Stop reading and writing more than once
                        foreach (Preset preset in loadedPresets.Values)
                        {
                            preset.update(defaultInformation);
                        }
                        jObj["presets"] = JObject.FromObject(loadedPresets, serializer);
                    }

                    if (configuration != null && configuration.isAnySet())
                    {
                        // If the users chooses to use the legacy values, then overwrite any exported presets with the new updated exported presets
                        MiniLogger.LogInfo($"Using legacy configuration: {configuration.useLegacy.Value}");
                        exportLegacy(jObj, configuration.useLegacy.Value);
                    }
                }
                else
                {
                    MiniLogger.LogInfo("No data file found, exporting global values");

                    // Export the global configuration if it exists
                    jObj = new JObject();

                    exportLegacy(jObj);
                }

                // Set the default information
                jObj["defaults"] = JObject.FromObject(defaultInformation, serializer);

                MiniLogger.LogInfo($"Writing data to the advanced config folder");

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
                MiniLogger.LogInfo($"Created global preset {globalPreset.id}");

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

                    MiniLogger.LogInfo($"Created level preset {levelPreset.id} for {entry.Key.getLevelName()}");
                }
                else if (globalPreset != null)
                {
                    MiniLogger.LogInfo($"No configuration found for {entry.Key.getLevel().PlanetName}, defaulting to global");
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
                    MiniLogger.LogError("Found an invalid presets node, replacing");
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
                MiniLogger.LogInfo("Importing data");
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.NullValueHandling = NullValueHandling.Ignore;
                settings.Converters.Add(new AnimationCurveJsonConverter());
                settings.Converters.Add(new DirectionalSpawnableMapObjectJsonConverter());
                settings.Converters.Add(new EnemyTypeJsonConverter());
                settings.Converters.Add(new ItemJsonConverter());
                settings.Converters.Add(new SelectableLevelGuidJsonConverter());
                ImportData importedInformation = JsonConvert.DeserializeObject<ImportData>(File.ReadAllText(path), settings);

                MiniLogger.LogInfo("Generating level presets");
                presets = importedInformation.generate(info);

                JObject jObj = new JObject();
                if (File.Exists(PRESET_FILE))
                {
                    jObj = JObject.Parse(File.ReadAllText(PRESET_FILE));
                }

                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new AnimationCurveJsonConverter());
                serializer.Converters.Add(new DeepDictionaryConverter(settings));

                jObj["results"] = JObject.FromObject(presets, serializer);
                MiniLogger.LogInfo("Saving level presets");

                ignoreCount++;
                File.WriteAllText(PRESET_FILE, JsonConvert.SerializeObject(jObj));

                MiniLogger.LogInfo("Done loading data");
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

    public static class MiniLogger
    {
        private static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_NAME);

        public static void LogInfo(string message)
        {
            logger.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            logger.LogError(message);
        }
    }
}
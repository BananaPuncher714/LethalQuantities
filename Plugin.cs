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

            _harmony = new Harmony(PluginInfo.PLUGIN_NAME);
            _harmony.PatchAll(typeof(RoundManagerPatch));
            _harmony.PatchAll(typeof(ObjectPatch));
            _harmony.PatchAll(typeof(DungeonPatch));
            _harmony.PatchAll(typeof(StartOfRoundPatch));
            _harmony.PatchAll(typeof(ConfigEntryBasePatch));

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
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

        internal static RoundState getRoundState()
        {
            GameObject obj = GameObject.Find("LevelModifier");
            if (obj != null)
            {
                return obj.GetComponent<RoundState>();
            }
            return null;
        }
    }
}
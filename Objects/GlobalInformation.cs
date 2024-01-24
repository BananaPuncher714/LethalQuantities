using BepInEx.Configuration;
using DunGen.Graph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalQuantities.Objects
{
    public class DirectionalSpawnableMapObject
    {
        public GameObject obj;
        public bool faceAwayFromWall;

        public DirectionalSpawnableMapObject(GameObject obj, bool faceAwayFromWall)
        {
            this.obj = obj;
            this.faceAwayFromWall = faceAwayFromWall;
        }
    }

    public class GenericLevelInformation
    {
        public int price { get; set; }
    }

    public class GlobalInformation
    {
        public static readonly Comparison<string> STRING_SORTER = (a, b) =>
        {
            if (a.ToUpper() == b.ToUpper())
            {
                return a.CompareTo(b);
            }
            else
            {
                return a.ToUpper().CompareTo(b.ToUpper());
            }
        };
        public static readonly Comparison<ScriptableObject> SCRIPTABLE_OBJECT_SORTER = (a, b) => STRING_SORTER(a.name, b.name);

        public List<EnemyType> allEnemyTypes { get; } = new List<EnemyType>();
        public List<Item> allItems { get; } = new List<Item>();
        public List<DungeonFlow> allDungeonFlows { get; } = new List<DungeonFlow>();
        public Dictionary<SelectableLevel, GenericLevelInformation> allSelectableLevels { get; } = new Dictionary<SelectableLevel, GenericLevelInformation>();
        public List<DirectionalSpawnableMapObject> allSpawnableMapObjects { get; } = new List<DirectionalSpawnableMapObject>();
        public RoundManager manager;

        public string configSaveDir { get; private set; }
        public string moonSaveDir { get; private set; }

        public GlobalInformation(string globalConfigSaveDir, string moonSaveDir)
        {
            configSaveDir = globalConfigSaveDir;
            this.moonSaveDir = moonSaveDir;
        }

        public void sortData()
        {
            allEnemyTypes.Sort(SCRIPTABLE_OBJECT_SORTER);
            allItems.Sort(SCRIPTABLE_OBJECT_SORTER);
            allDungeonFlows.Sort(SCRIPTABLE_OBJECT_SORTER);
            allSpawnableMapObjects.Sort((a, b) => STRING_SORTER(a.obj.name, b.obj.name));
        }
    }

    public class LevelInformation
    {
        public GlobalConfiguration masterConfig { get; private set; }
        public GlobalInformation globalInfo { get; private set; }
        public SelectableLevel level { get; private set; }
        public string levelSaveDir { get; private set; }
        public ConfigFile mainConfigFile { get; private set; }

        public LevelInformation(GlobalConfiguration config, GlobalInformation globalInfo, SelectableLevel level, string levelSaveDir, ConfigFile mainConfigFile)
        {
            this.masterConfig = config;
            this.globalInfo = globalInfo;
            this.level = level;
            this.levelSaveDir = levelSaveDir;
            this.mainConfigFile = mainConfigFile;
        }
    }

    public static class EnemyTypeExtension
    {
        private static Dictionary<int, TerminalNode> enemyFiles = new Dictionary<int, TerminalNode>();

        static EnemyTypeExtension()
        {
            foreach (TerminalNode node in Resources.FindObjectsOfTypeAll<TerminalNode>())
            {
                enemyFiles.TryAdd(node.creatureFileID, node);
            }
        }

        public static string getFriendlyName(this EnemyType type)
        {
            GameObject obj = type.enemyPrefab;
            if (obj != null)
            {
                ScanNodeProperties node = obj.GetComponentInChildren<ScanNodeProperties>();
                if (node != null)
                {
                    if (enemyFiles.TryGetValue(node.creatureScanID, out TerminalNode file))
                    {
                        return file.creatureName;
                    }
                }
            }

            return type.name;
        }
    }
}

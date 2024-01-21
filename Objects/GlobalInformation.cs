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

    public class GlobalInformation
    {
        public List<EnemyType> allEnemyTypes { get; } = new List<EnemyType>();
        public List<Item> allItems { get; } = new List<Item>();
        public List<DungeonFlow> allDungeonFlows { get; } = new List<DungeonFlow>();
        public List<SelectableLevel> allSelectableLevels { get; } = new List<SelectableLevel>();
        public List<DirectionalSpawnableMapObject> allSpawnableMapObjects { get; } = new List<DirectionalSpawnableMapObject>();

        public string configSaveDir { get; private set; }
        public string moonSaveDir { get; private set; }

        public GlobalInformation(string globalConfigSaveDir, string moonSaveDir)
        {
            configSaveDir = globalConfigSaveDir;
            this.moonSaveDir = moonSaveDir;
        }

        public void sortData()
        {
            Comparison<string> semiAlphabeticalComparison = (a, b) =>
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
            Comparison<ScriptableObject> sortOfAlphabeticalComparison = (a, b) =>
            {
                return semiAlphabeticalComparison(a.name, b.name);
            };

            Comparison<DirectionalSpawnableMapObject> anotherSortOfAlphabeticalComparison = (a, b) =>
            {
                return semiAlphabeticalComparison(a.obj.name, b.obj.name);
            };

            allEnemyTypes.Sort(sortOfAlphabeticalComparison);
            allItems.Sort(sortOfAlphabeticalComparison);
            allDungeonFlows.Sort(sortOfAlphabeticalComparison);
            allSelectableLevels.Sort(sortOfAlphabeticalComparison);
            allSpawnableMapObjects.Sort(anotherSortOfAlphabeticalComparison);
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

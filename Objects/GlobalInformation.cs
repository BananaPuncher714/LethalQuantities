using BepInEx.Configuration;
using DunGen.Graph;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LethalQuantities.Objects
{
    public class GlobalInformation
    {
        public List<EnemyType> allEnemyTypes { get; } = new List<EnemyType>();
        public List<Item> allItems { get; } = new List<Item>();
        public List<DungeonFlow> allDungeonFlows { get; } = new List<DungeonFlow>();
        public List<SelectableLevel> allSelectableLevels { get; } = new List<SelectableLevel>();

        public string configSaveDir { get; private set; }
        public string moonSaveDir { get; private set; }

        public GlobalInformation(string globalConfigSaveDir, string moonSaveDir)
        {
            configSaveDir = globalConfigSaveDir;
            this.moonSaveDir = moonSaveDir;
        }

        public void sortData()
        {
            Comparison<ScriptableObject> sortOfAlphabeticalComparison = (a, b) =>
            {
                string nameA = a.name;
                string nameB = b.name;
                if (nameA.ToUpper() == nameB.ToUpper())
                {
                    return nameA.CompareTo(nameB);
                }
                else
                {
                    return nameA.ToUpper().CompareTo(nameB.ToUpper());
                }
            };

            allEnemyTypes.Sort(sortOfAlphabeticalComparison);
            allItems.Sort(sortOfAlphabeticalComparison);
            allDungeonFlows.Sort(sortOfAlphabeticalComparison);
            allSelectableLevels.Sort(sortOfAlphabeticalComparison);
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
}

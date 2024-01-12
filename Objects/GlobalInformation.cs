using BepInEx.Configuration;
using System.Collections.Generic;

namespace LethalQuantities.Objects
{
    public class GlobalInformation
    {
        public HashSet<EnemyType> allEnemyTypes { get; } = new HashSet<EnemyType>();
        public HashSet<Item> allItems { get; } = new HashSet<Item>();
        public HashSet<SelectableLevel> allSelectableLevels { get; } = new HashSet<SelectableLevel>();

        public string configSaveDir { get; private set; }
        public string moonSaveDir { get; private set; }

        public GlobalInformation(string globalConfigSaveDir, string moonSaveDir)
        {
            configSaveDir = globalConfigSaveDir;
            this.moonSaveDir = moonSaveDir;
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

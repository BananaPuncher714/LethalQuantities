using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LethalQuantities.Objects
{
    public class EnemyConfiguration
    {
        public ConfigEntry<bool> enabled { get; set; }
        public ConfigEntry<int> maxPowerCount { get; set; }
        public ConfigEntry<AnimationCurve> spawnAmountCurve { get; set; }
        public ConfigEntry<float> spawnAmountRange { get; set; }
        public List<EnemyTypeConfiguration> enemyTypes { get; } = new List<EnemyTypeConfiguration>();
    }

    public class EnemyTypeConfiguration
    {
        public EnemyType type { get; protected set; }
        internal EnemyTypeConfiguration(EnemyType type)
        {
            this.type = type;
        }

        public ConfigEntry<int> rarity { get; set; }
        public ConfigEntry<int> maxEnemyCount { get; set; }
        public ConfigEntry<int> powerLevel { get; set; }
        public ConfigEntry<AnimationCurve> spawnCurve { get; set; }
        public ConfigEntry<AnimationCurve> spawnFalloffCurve { get; set; }
        public ConfigEntry<bool> useSpawnFalloff { get; set; }

    }

    public class ScrapConfiguration
    {
        public ConfigEntry<bool> enabled { get; set; }
        public ConfigEntry<int> minScrap { get; set; }
        public ConfigEntry<int> maxScrap { get; set; }
        public ConfigEntry<float> scrapValueMultiplier { get; set; }
        public ConfigEntry<float> scrapAmountMultiplier { get; set; }
        public List<ScrapItemConfiguration> scrapRarities { get; } = new List<ScrapItemConfiguration>();
    }

    public class ScrapItemConfiguration
    {
        public Item item { get; protected set; }
        internal ScrapItemConfiguration(Item item)
        {
            this.item = item;
        }

        public ConfigEntry<int> rarity { get; set; }
        public ConfigEntry<int> maxValue { get; set; }
        public ConfigEntry<int> minValue { get; set; }
    }

    public class LevelConfiguration
    {
        private static readonly string ENEMY_CFG_NAME = "Enemies.cfg";
        private static readonly string DAYTIME_ENEMY_CFG_NAME = "DaytimeEnemies.cfg";
        private static readonly string OUTSIDE_ENEMY_CFG_NAME = "OutsideEnemies.cfg";
        private static readonly string SCRAP_CFG_NAME = "Scrap.cfg";

        public int levelId { get; }
        public string sceneName { get; }
        private string saveDirectory { get; }

        public EnemyConfiguration enemies { get; }
        public EnemyConfiguration daytimeEnemies { get; }
        public EnemyConfiguration outsideEnemies { get; }
        public ScrapConfiguration scrap { get; }

        public LevelConfiguration(string saveDirectory, SelectableLevel level, LevelInformation levelInfo)
        {
            levelId = level.levelID;
            sceneName = level.sceneName;
            this.saveDirectory = saveDirectory;

            enemies = new EnemyConfiguration();
            daytimeEnemies = new EnemyConfiguration();
            outsideEnemies = new EnemyConfiguration();
            scrap = new ScrapConfiguration();


            instantiateConfigs(level, levelInfo);
        }

        private void instantiateConfigs(SelectableLevel level, LevelInformation levelInfo)
        {
            instantiateEnemyConfigs(level, levelInfo.allEnemyTypes);
            instantiateScrapConfigs(level, levelInfo.allItems);
        }

        private void instantiateEnemyConfigs(SelectableLevel level, HashSet<EnemyType> enemyTypes)
        {
            // Process enemies
            {
                ConfigFile enemyConfig = new ConfigFile(Path.Combine(saveDirectory, ENEMY_CFG_NAME), true);
                // TODO Save only if a setting has been changed
                enemyConfig.SaveOnConfigSet = false;
                enemies.enabled = enemyConfig.Bind("General", "Enabled", false, "Enables/disables custom enemy spawn rate modification");
                enemies.maxPowerCount = enemyConfig.Bind("General", "MaxPowerCount", level.maxEnemyPowerCount, "Maximum total power level allowed for inside enemies.");
                enemies.spawnAmountCurve = enemyConfig.Bind("General", "SpawnAmountCurve", level.enemySpawnChanceThroughoutDay, "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1 )");
                enemies.spawnAmountRange = enemyConfig.Bind("General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.Enemies);
                foreach (EnemyType enemyType in enemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.name}";

                    typeConfiguration.rarity = enemyConfig.Bind(tablename, "Rarity", enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {enemyType.enemyName} spawning relative to the total rarity of all other enemy types combined.");
                    typeConfiguration.maxEnemyCount = enemyConfig.Bind(tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once.");
                    typeConfiguration.powerLevel = enemyConfig.Bind(tablename, "PowerLevel", enemyType.PowerLevel, $"How much a single {enemyType.enemyName} contributes to the maximum power level.");
                    typeConfiguration.spawnCurve = enemyConfig.Bind(tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses. (Key ranges from 0-1 )");
                    typeConfiguration.spawnFalloffCurve = enemyConfig.Bind(tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many already have been spawned. (Key is number of ${enemyType.enemyName}s/10)");
                    typeConfiguration.useSpawnFalloff = enemyConfig.Bind(tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are inside.");

                    enemies.enemyTypes.Add(typeConfiguration);
                }
                enemyConfig.SaveOnConfigSet = true;
                enemyConfig.Save();
            }

            // Process daytime enemies
            {
                ConfigFile enemyConfig = new ConfigFile(Path.Combine(saveDirectory, DAYTIME_ENEMY_CFG_NAME), true);
                enemyConfig.SaveOnConfigSet = false;
                daytimeEnemies.enabled = enemyConfig.Bind("General", "Enabled", false, "Enables/disables custom daytime enemy spawn rate modification. Typically enemies like manticoils, locusts, etc.");
                daytimeEnemies.maxPowerCount = enemyConfig.Bind("General", "MaxPowerCount", level.maxDaytimeEnemyPowerCount, "Maximum total power level allowed for daytime enemies.");
                daytimeEnemies.spawnAmountCurve = enemyConfig.Bind("General", "SpawnAmountCurve", level.daytimeEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1)");
                daytimeEnemies.spawnAmountRange = enemyConfig.Bind("General", "SpawnAmountRange", level.daytimeEnemiesProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.DaytimeEnemies);
                foreach (EnemyType enemyType in enemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.name}";

                    typeConfiguration.rarity = enemyConfig.Bind(tablename, "Rarity", enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {enemyType.enemyName} relative to the total rarity of all other enemy types combined.");
                    typeConfiguration.maxEnemyCount = enemyConfig.Bind(tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once.");
                    typeConfiguration.powerLevel = enemyConfig.Bind(tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {enemyType.enemyName} contributes to the maximum power level.");
                    typeConfiguration.spawnCurve = enemyConfig.Bind(tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses. (Key ranges from 0-1 )");
                    typeConfiguration.spawnFalloffCurve = enemyConfig.Bind(tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}s/10). This does not work for daytime enemies.");
                    typeConfiguration.useSpawnFalloff = enemyConfig.Bind(tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are.");

                    daytimeEnemies.enemyTypes.Add(typeConfiguration);
                }
                enemyConfig.SaveOnConfigSet = true;
                enemyConfig.Save();
            }

            // Process outside enemies
            {
                ConfigFile enemyConfig = new ConfigFile(Path.Combine(saveDirectory, OUTSIDE_ENEMY_CFG_NAME), true);
                enemyConfig.SaveOnConfigSet = false;
                outsideEnemies.enabled = enemyConfig.Bind("General", "Enabled", false, "Enables/disables custom outside enemy spawn rate modification. Typically eyeless dogs, forest giants, etc.");
                outsideEnemies.maxPowerCount = enemyConfig.Bind("General", "MaxPowerCount", level.maxOutsideEnemyPowerCount, "Maximum total power level allowed for outside enemies.");
                outsideEnemies.spawnAmountCurve = enemyConfig.Bind("General", "SpawnAmountCurve", level.outsideEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses, (Key ranges from 0-1).");
                outsideEnemies.spawnAmountRange = enemyConfig.Bind("General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.OutsideEnemies);
                foreach (EnemyType enemyType in enemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.name}";

                    typeConfiguration.rarity = enemyConfig.Bind(tablename, "Rarity", enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {enemyType.enemyName} relative to the total rarity of all other enemy types combined.");
                    typeConfiguration.maxEnemyCount = enemyConfig.Bind(tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once.");
                    typeConfiguration.powerLevel = enemyConfig.Bind(tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {enemyType.enemyName} contributes to the maximum power level.");
                    typeConfiguration.spawnCurve = enemyConfig.Bind(tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses, (Key ranges from 0-1).");
                    typeConfiguration.spawnFalloffCurve = enemyConfig.Bind(tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}s/10)");
                    typeConfiguration.useSpawnFalloff = enemyConfig.Bind(tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are.");

                    outsideEnemies.enemyTypes.Add(typeConfiguration);
                }
                enemyConfig.SaveOnConfigSet = true;
                enemyConfig.Save();
            }
        }

        private void instantiateScrapConfigs(SelectableLevel level, HashSet<Item> items)
        {
            {
                ConfigFile scrapConfig = new ConfigFile(Path.Combine(saveDirectory, SCRAP_CFG_NAME), true);
                scrapConfig.SaveOnConfigSet = true;
                scrap.enabled = scrapConfig.Bind("General", "Enabled", false, "Enables/disables custom scrap generation");
                scrap.maxScrap = scrapConfig.Bind("General", "MaxScrapCount", level.maxScrap, "Maximum total number of scrap generated in the level.");
                scrap.minScrap = scrapConfig.Bind("General", "MinScrapCount", level.minScrap, "Minimum total number of scrap generated in the level.");
                scrap.scrapAmountMultiplier = scrapConfig.Bind("General", "ScrapAmountMultiplier", 1f, "Modifier to the total amount of scrap generated in the level.");
                scrap.scrapValueMultiplier = scrapConfig.Bind("General", "ScrapValueMultiplier", .4f, "Modifier to the total value of scrap generated in the level.");
                Dictionary<Item, int> itemSpawnRarities = convertToDictionary(level.spawnableScrap);
                foreach ( Item itemType in items)
                {
                    ScrapItemConfiguration itemConfiguration = new ScrapItemConfiguration(itemType);
                    string tablename = $"ItemType.{itemType.name}";

                    itemConfiguration.rarity = scrapConfig.Bind(tablename, "Rarity", itemSpawnRarities.GetValueOrDefault(itemType, 0), $"Rarity of a(n) {itemType.itemName} relative to the total rarity of all other item types combined.");
                    itemConfiguration.minValue = scrapConfig.Bind(tablename, "MinValue", itemType.minValue, $"Minimum value of a {itemType.itemName}");
                    itemConfiguration.maxValue = scrapConfig.Bind(tablename, "MaxValue", itemType.maxValue, $"Maximum value of a {itemType.itemName}");

                    scrap.scrapRarities.Add(itemConfiguration);
                }
                scrapConfig.SaveOnConfigSet = true;
                scrapConfig.Save();
            }
        }

        private static Dictionary<EnemyType, int> convertToDictionary(List<SpawnableEnemyWithRarity> enemies)
        {
            Dictionary<EnemyType, int> enemySpawnRarities = new Dictionary<EnemyType, int>();
            foreach (SpawnableEnemyWithRarity enemy in enemies)
            {
                enemySpawnRarities.Add(enemy.enemyType, enemy.rarity);
            }
            return enemySpawnRarities;
        }

        private static Dictionary<Item, int> convertToDictionary(List<SpawnableItemWithRarity> items)
        {
            Dictionary<Item, int> itemSpawnRarities = new Dictionary<Item, int>();
            foreach(SpawnableItemWithRarity item in items)
            {
                if (!itemSpawnRarities.TryAdd(item.spawnableItem, item.rarity))
                {
                    itemSpawnRarities[item.spawnableItem] += item.rarity;
                }
            }
            return (itemSpawnRarities);
        }
    }
}

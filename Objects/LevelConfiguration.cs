using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LethalQuantities.Objects
{
    public class OutsideEnemyConfiguration<T> where T: DaytimeEnemyTypeConfiguration
    {
        public ConfigEntry<bool> enabled { get; set; }
        public GlobalConfigEntry<int> maxPowerCount { get; set; }
        public GlobalConfigEntry<AnimationCurve> spawnAmountCurve { get; set; }
        public List<T> enemyTypes { get; } = new List<T>();
    }

    public class EnemyConfiguration<T> : OutsideEnemyConfiguration<T> where T: DaytimeEnemyTypeConfiguration 
    {
        public GlobalConfigEntry<float> spawnAmountRange { get; set; }
    }

    public class DaytimeEnemyTypeConfiguration
    {
        public EnemyType type { get; protected set; }
        public DaytimeEnemyTypeConfiguration(EnemyType type)
        {
            this.type = type;
        }
        public GlobalConfigEntry<int> rarity { get; set; }
        public GlobalConfigEntry<int> maxEnemyCount { get; set; }
        public GlobalConfigEntry<int> powerLevel { get; set; }
        public GlobalConfigEntry<AnimationCurve> spawnCurve { get; set; }
        public GlobalConfigEntry<float> stunTimeMultiplier { get; set; }
        public GlobalConfigEntry<float> doorSpeedMultiplier { get; set; }
        public GlobalConfigEntry<float> stunGameDifficultyMultiplier { get; set; }
        public GlobalConfigEntry<bool> stunnable { get; set; }
        public GlobalConfigEntry<bool> killable { get; set; }
        public GlobalConfigEntry<int> enemyHp { get; set; }
    }

    public class EnemyTypeConfiguration : DaytimeEnemyTypeConfiguration
    {
        public EnemyTypeConfiguration(EnemyType type) : base(type)
        {
        }

        public GlobalConfigEntry<AnimationCurve> spawnFalloffCurve { get; set; }
        public GlobalConfigEntry<bool> useSpawnFalloff { get; set; }
    }

    public class ScrapConfiguration
    {
        public ConfigEntry<bool> enabled { get; set; }
        public GlobalConfigEntry<int> minScrap { get; set; }
        public GlobalConfigEntry<int> maxScrap { get; set; }
        public GlobalConfigEntry<float> scrapValueMultiplier { get; set; }
        public GlobalConfigEntry<float> scrapAmountMultiplier { get; set; }
        public List<ItemConfiguration> scrapRarities { get; } = new List<ItemConfiguration>();
    }

    public class ItemConfiguration
    {
        public Item item { get; protected set; }
        internal ItemConfiguration(Item item)
        {
            this.item = item;
        }

        public GlobalConfigEntry<int> rarity { get; set; }
        public GlobalConfigEntry<bool> conductive { get; set; }
    }

    public class ScrapItemConfiguration : ItemConfiguration
    {
        internal ScrapItemConfiguration(Item item) : base(item)
        {
        }

        public GlobalConfigEntry<int> maxValue { get; set; }
        public GlobalConfigEntry<int> minValue { get; set; }
    }

    public class LevelConfiguration
    {
        public EnemyConfiguration<EnemyTypeConfiguration> enemies { get; }
        public EnemyConfiguration<DaytimeEnemyTypeConfiguration> daytimeEnemies { get; }
        public OutsideEnemyConfiguration<EnemyTypeConfiguration> outsideEnemies { get; }
        public ScrapConfiguration scrap { get; }

        public LevelConfiguration(LevelInformation levelInfo)
        {
            enemies = new EnemyConfiguration<EnemyTypeConfiguration>();
            daytimeEnemies = new EnemyConfiguration<DaytimeEnemyTypeConfiguration>();
            outsideEnemies = new OutsideEnemyConfiguration<EnemyTypeConfiguration>();
            scrap = new ScrapConfiguration();

            instantiateConfigs(levelInfo);
        }

        private void instantiateConfigs(LevelInformation levelInfo)
        {
            instantiateEnemyConfigs(levelInfo);
            instantiateScrapConfigs(levelInfo);
        }

        private void instantiateEnemyConfigs(LevelInformation levelInfo)
        {
            SelectableLevel level = levelInfo.level;
            // Process enemies
            {
                enemies.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "EnemiesEnabled", false, "Enables/disables custom enemy spawn rate modification");

                // Only save the configuration file and options if it is enabled
                if (enemies.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile enemyConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                    enemies.maxPowerCount = enemyConfig.BindGlobal(masterConfig.enemyConfiguration.maxPowerCount, "General", "MaxPowerCount", level.maxEnemyPowerCount, "Maximum total power level allowed for inside enemies. The default value is {0}");
                    enemies.spawnAmountCurve = enemyConfig.BindGlobal(masterConfig.enemyConfiguration.spawnAmountCurve, "General", "SpawnAmountCurve", level.enemySpawnChanceThroughoutDay, "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1 ). The default value is {0}");
                    enemies.spawnAmountRange = enemyConfig.BindGlobal(masterConfig.enemyConfiguration.spawnAmountRange, "General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies. The default value is {0}");

                    Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.Enemies);
                    foreach (EnemyType enemyType in levelInfo.globalInfo.allEnemyTypes)
                    {
                        EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                        string tablename = $"EnemyTypes.{enemyType.name}";

                        GlobalEnemyTypeConfiguration masterTypeConfig = masterConfig.enemyConfiguration.enemyTypeConfigurations[enemyType];

                        // Store rarity in a separate table for convenience
                        typeConfiguration.rarity = enemyConfig.BindGlobal(masterTypeConfig.rarity, "Rarity", enemyType.name, enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {enemyType.enemyName} spawning relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT. If no value or DEFAULT is present in the global config, then this value will be the DEFAULT for this moon.");

                        typeConfiguration.maxEnemyCount = enemyConfig.BindGlobal(masterTypeConfig.maxEnemyCount, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.powerLevel = enemyConfig.BindGlobal(masterTypeConfig.powerLevel, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a single {enemyType.enemyName} contributes to the maximum power level. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULTT");
                        typeConfiguration.spawnCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnCurve, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses. (Key ranges from 0-1 ). The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunTimeMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunTimeMultiplier, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {enemyType.enemyName} can be stunned. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.doorSpeedMultiplier = enemyConfig.BindGlobal(masterTypeConfig.doorSpeedMultiplier, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {enemyType.enemyName} to open a door. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunGameDifficultyMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunGameDifficultyMultiplier, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunnable = enemyConfig.BindGlobal(masterTypeConfig.stunnable, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {enemyType.enemyName} can be stunned. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.killable = enemyConfig.BindGlobal(masterTypeConfig.killable, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {enemyType.enemyName} can die. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.enemyHp = enemyConfig.BindGlobal(masterTypeConfig.enemyHp, tablename, "EnemyHp", enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP, $"The initial amount of health a(n) {enemyType.enemyName} has. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.spawnFalloffCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnFalloffCurve, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many already have been spawned. (Key is number of {enemyType.enemyName}s/10). The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.useSpawnFalloff = enemyConfig.BindGlobal(masterTypeConfig.useSpawnFalloff, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are inside. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");

                        enemies.enemyTypes.Add(typeConfiguration);
                    }
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }

            // Process daytime enemies
            {
                daytimeEnemies.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "DaytimeEnemiesEnabled", false, "Enables/disables custom daytime enemy spawn rate modification. Typically enemies like manticoils, locusts, etc.");

                if (daytimeEnemies.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile enemyConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.DAYTIME_ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                    daytimeEnemies.maxPowerCount = enemyConfig.BindGlobal(masterConfig.daytimeEnemyConfiguration.maxPowerCount, "General", "MaxPowerCount", level.maxDaytimeEnemyPowerCount, "Maximum total power level allowed for daytime enemies. The default value is {0}");
                    daytimeEnemies.spawnAmountCurve = enemyConfig.BindGlobal(masterConfig.daytimeEnemyConfiguration.spawnAmountCurve, "General", "SpawnAmountCurve", level.daytimeEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1). The default value is {0}");
                    daytimeEnemies.spawnAmountRange = enemyConfig.BindGlobal(masterConfig.daytimeEnemyConfiguration.spawnAmountRange, "General", "SpawnAmountRange", level.daytimeEnemiesProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies. The default value is {0}");

                    Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.DaytimeEnemies);
                    foreach (EnemyType enemyType in levelInfo.globalInfo.allEnemyTypes)
                    {
                        DaytimeEnemyTypeConfiguration typeConfiguration = new DaytimeEnemyTypeConfiguration(enemyType);

                        GlobalDaytimeEnemyTypeConfiguration masterTypeConfig = masterConfig.daytimeEnemyConfiguration.enemyTypeConfigurations[enemyType];
                        typeConfiguration.rarity = enemyConfig.BindGlobal(masterTypeConfig.rarity, "Rarity", enemyType.name, enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {enemyType.enemyName} relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT. If no value or DEFAULT is present in the global config, then this value will be the DEFAULT for this moon.");

                        string tablename = $"EnemyTypes.{enemyType.name}";
                        typeConfiguration.maxEnemyCount = enemyConfig.BindGlobal(masterTypeConfig.maxEnemyCount, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.powerLevel = enemyConfig.BindGlobal(masterTypeConfig.powerLevel, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {enemyType.enemyName} contributes to the maximum power level. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.spawnCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnCurve, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses. (Key ranges from 0-1). The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and game with the value DEFAULT");
                        typeConfiguration.stunTimeMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunTimeMultiplier, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {enemyType.enemyName} can be stunned. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.doorSpeedMultiplier = enemyConfig.BindGlobal(masterTypeConfig.doorSpeedMultiplier, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {enemyType.enemyName} to open a door. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunGameDifficultyMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunGameDifficultyMultiplier, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunnable = enemyConfig.BindGlobal(masterTypeConfig.stunnable, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {enemyType.enemyName} can be stunned. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.killable = enemyConfig.BindGlobal(masterTypeConfig.killable, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {enemyType.enemyName} can die. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.enemyHp = enemyConfig.BindGlobal(masterTypeConfig.enemyHp, tablename, "EnemyHp", enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP, $"The initial amount of health a(n) {enemyType.enemyName} has. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        // Not implemented for daytime enemies
                        //typeConfiguration.spawnFalloffCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnFalloffCurve, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}s/10). This does not work for daytime enemies. The default value is {{0}}");
                        //typeConfiguration.useSpawnFalloff = enemyConfig.BindGlobal(masterTypeConfig.useSpawnFalloff, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are. The default value is {{0}}");

                        daytimeEnemies.enemyTypes.Add(typeConfiguration);
                    }
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }

            // Process outside enemies
            {
                outsideEnemies.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "OutsideEnemiesEnabled", false, "Enables/disables custom outside enemy spawn rate modification. Typically eyeless dogs, forest giants, etc.");

                if (outsideEnemies.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile enemyConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.OUTSIDE_ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                    outsideEnemies.maxPowerCount = enemyConfig.BindGlobal(masterConfig.outsideEnemyConfiguration.maxPowerCount, "General", "MaxPowerCount", level.maxOutsideEnemyPowerCount, "Maximum total power level allowed for outside enemies. The default value is {{0}}");
                    outsideEnemies.spawnAmountCurve = enemyConfig.BindGlobal(masterConfig.outsideEnemyConfiguration.spawnAmountCurve, "General", "SpawnAmountCurve", level.outsideEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses, (Key ranges from 0-1). The default value is {{0}}");
                    // Hardcoded to 3 internally
                    //outsideEnemies.spawnAmountRange = enemyConfig.BindGlobal("General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                    Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.OutsideEnemies);
                    foreach (EnemyType enemyType in levelInfo.globalInfo.allEnemyTypes)
                    {
                        EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);

                        GlobalEnemyTypeConfiguration masterTypeConfig = masterConfig.outsideEnemyConfiguration.enemyTypeConfigurations[enemyType];
                        typeConfiguration.rarity = enemyConfig.BindGlobal(masterTypeConfig.rarity, "Rarity", enemyType.name, enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {enemyType.enemyName} relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.");

                        string tablename = $"EnemyTypes.{enemyType.name}";
                        typeConfiguration.maxEnemyCount = enemyConfig.BindGlobal(masterTypeConfig.maxEnemyCount, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.powerLevel = enemyConfig.BindGlobal(masterTypeConfig.powerLevel, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {enemyType.enemyName} contributes to the maximum power level. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and game with the value DEFAULT");
                        typeConfiguration.spawnCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnCurve, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName}s allowed at once. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunTimeMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunTimeMultiplier, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {enemyType.enemyName} can be stunned. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.doorSpeedMultiplier = enemyConfig.BindGlobal(masterTypeConfig.doorSpeedMultiplier, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {enemyType.enemyName} to open a door. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunGameDifficultyMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunGameDifficultyMultiplier, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.stunnable = enemyConfig.BindGlobal(masterTypeConfig.stunnable, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {enemyType.enemyName} can be stunned. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.killable = enemyConfig.BindGlobal(masterTypeConfig.killable, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {enemyType.enemyName} can die. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.enemyHp = enemyConfig.BindGlobal(masterTypeConfig.enemyHp, tablename, "EnemyHp", enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP, $"The initial amount of health a(n) {enemyType.enemyName} has. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.spawnFalloffCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnFalloffCurve, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}s allowed at once. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        typeConfiguration.useSpawnFalloff = enemyConfig.BindGlobal(masterTypeConfig.useSpawnFalloff, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are. The default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");

                        outsideEnemies.enemyTypes.Add(typeConfiguration);
                    }
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }
        }

        private void instantiateScrapConfigs(LevelInformation levelInfo)
        {
            SelectableLevel level = levelInfo.level;
            {
                scrap.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "ScrapEnabled", false, "Enables/disables custom scrap generation");

                if (scrap.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile scrapConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.SCRAP_CFG_NAME), true);
                    scrapConfig.SaveOnConfigSet = true;
                    scrap.minScrap = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.minScrap, "General", "MinScrapCount", level.minScrap, "Minimum total number of scrap generated in the level. The default value is {{0}}");
                    scrap.maxScrap = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.maxScrap, "General", "MaxScrapCount", level.maxScrap, "Maximum total number of scrap generated in the level. The default value is {{0}}");
                    scrap.scrapAmountMultiplier = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.scrapAmountMultiplier, "General", "ScrapAmountMultiplier", 1f, "Modifier to the total amount of scrap generated in the level. Default value is {0}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                    scrap.scrapValueMultiplier = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.scrapValueMultiplier, "General", "ScrapValueMultiplier", .4f, "Modifier to the total value of scrap generated in the level. Default value is {0}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                    Dictionary<Item, int> itemSpawnRarities = convertToDictionary(level.spawnableScrap);
                    foreach ( Item itemType in levelInfo.globalInfo.allItems)
                    {
                        ItemConfiguration configuration;
                        string tablename = $"ItemType.{itemType.name}";
                        GlobalItemConfiguration mainItemConfig = masterConfig.scrapConfiguration.itemConfigurations[itemType];
                        if (mainItemConfig is GlobalItemScrapConfiguration)
                        {
                            GlobalItemScrapConfiguration masterTypeConfig = mainItemConfig as GlobalItemScrapConfiguration;
                            ScrapItemConfiguration itemConfiguration = new ScrapItemConfiguration(itemType);
                            configuration = itemConfiguration;

                            itemConfiguration.minValue = scrapConfig.BindGlobal(masterTypeConfig.minValue, tablename, "MinValue", itemType.minValue, $"Minimum value of a {itemType.itemName}. Default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                            itemConfiguration.maxValue = scrapConfig.BindGlobal(masterTypeConfig.maxValue, tablename, "MaxValue", itemType.maxValue, $"Maximum value of a {itemType.itemName}. Default value is {{0}}. This option can inherit from the global config with the value GLOBAL and from the game with the value DEFAULT");
                        }
                        else
                        {
                            configuration = new ItemConfiguration(itemType);
                        }

                        configuration.rarity = scrapConfig.BindGlobal(mainItemConfig.rarity, "Rarity", itemType.name, itemSpawnRarities.GetValueOrDefault(itemType, 0), $"Rarity of a(n) {itemType.itemName} relative to the total rarity of all other item types combined. A higher rarity increases the chance that the item will spawn. The default value is {{0}}");

                        configuration.conductive = scrapConfig.BindGlobal(mainItemConfig.conductive, tablename, "Conductive", itemType.isConductiveMetal, $"Whether or not {itemType.itemName} is conductive(can be struck by lightning). The default value is {{0}}.");
                        scrap.scrapRarities.Add(configuration);
                    }
                    scrapConfig.SaveOnConfigSet = true;
                    scrapConfig.Save();
                }
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

using BepInEx.Configuration;
using DunGen.Graph;
using System;
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

    public class DungeonGenerationConfiguration
    {
        public ConfigEntry<bool> enabled { get; set; }
        public GlobalConfigEntry<float> mapSizeMultiplier { get; set; }
        public Dictionary<string, DungeonFlowConfiguration> dungeonFlowConfigurations { get; private set; } = new Dictionary<string, DungeonFlowConfiguration>();
    }

    public class DungeonFlowConfiguration
    {
        public GlobalConfigEntry<int> rarity { get; set; }
        public GlobalConfigEntry<float> factorySizeMultiplier { get; set; }
    }

    public class TrapConfiguration
    {
        public ConfigEntry<bool> enabled { get; set; }
        public Dictionary<GameObject, SpawnableMapObjectConfiguration> traps { get; private set; } = new Dictionary<GameObject, SpawnableMapObjectConfiguration>();
    }

    public class SpawnableMapObjectConfiguration
    {
        public DirectionalSpawnableMapObject spawnableObject { get; }
        public GlobalConfigEntry<AnimationCurve> numberToSpawn { get; set; }

        public SpawnableMapObjectConfiguration(DirectionalSpawnableMapObject obj)
        {
            spawnableObject = obj;
        }
    }

    public class LevelConfiguration
    {
        public EnemyConfiguration<EnemyTypeConfiguration> enemies { get; } = new EnemyConfiguration<EnemyTypeConfiguration>();
        public EnemyConfiguration<DaytimeEnemyTypeConfiguration> daytimeEnemies { get; } = new EnemyConfiguration<DaytimeEnemyTypeConfiguration>();
        public OutsideEnemyConfiguration<EnemyTypeConfiguration> outsideEnemies { get; } = new OutsideEnemyConfiguration<EnemyTypeConfiguration>();
        public ScrapConfiguration scrap { get; } = new ScrapConfiguration();
        public DungeonGenerationConfiguration dungeon { get; } = new DungeonGenerationConfiguration();
        public TrapConfiguration trap { get; } = new TrapConfiguration();

        public LevelConfiguration(LevelInformation levelInfo)
        {
            instantiateConfigs(levelInfo);
        }

        private void instantiateConfigs(LevelInformation levelInfo)
        {
            instantiateEnemyConfigs(levelInfo);
            instantiateScrapConfigs(levelInfo);
            instantiateDungeonConfigs(levelInfo);
            instantiateTrapConfigs(levelInfo);
        }

        private void instantiateEnemyConfigs(LevelInformation levelInfo)
        {
            SelectableLevel level = levelInfo.level;
            // Process enemies
            {
                enemies.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "EnemiesEnabled", false, $"Enables/disables custom enemy spawn rate modification for {level.PlanetName}");

                // Only save the configuration file and options if it is enabled
                if (enemies.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile enemyConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                    enemies.maxPowerCount = enemyConfig.BindGlobal(masterConfig.enemyConfiguration.maxPowerCount, "General", "MaxPowerCount", level.maxEnemyPowerCount, "Maximum total power level allowed for inside enemies\nAlternate values: DEFAULT, GLOBAL");
                    enemies.spawnAmountCurve = enemyConfig.BindGlobal(masterConfig.enemyConfiguration.spawnAmountCurve, "General", "SpawnAmountCurve", level.enemySpawnChanceThroughoutDay, "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1 )\nAlternate values: DEFAULT, GLOBAL");
                    enemies.spawnAmountRange = enemyConfig.BindGlobal(masterConfig.enemyConfiguration.spawnAmountRange, "General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies\nAlternate values: DEFAULT, GLOBAL");

                    Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.Enemies);
                    foreach (EnemyType enemyType in levelInfo.globalInfo.allEnemyTypes)
                    {
                        EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                        string tablename = $"EnemyTypes.{enemyType.name}";
                        string friendlyName = enemyType.getFriendlyName();

                        GlobalEnemyTypeConfiguration masterTypeConfig = masterConfig.enemyConfiguration.enemyTypeConfigurations[enemyType];

                        // Store rarity in a separate table for convenience
                        typeConfiguration.rarity = enemyConfig.BindGlobal(masterTypeConfig.rarity, "Rarity", enemyType.name, enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {friendlyName} spawning relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.\nAlternate values: DEFAULT, GLOBAL");

                        typeConfiguration.maxEnemyCount = enemyConfig.BindGlobal(masterTypeConfig.maxEnemyCount, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {friendlyName} allowed at once.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.powerLevel = enemyConfig.BindGlobal(masterTypeConfig.powerLevel, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a single {friendlyName} contributes to the maximum power level.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.spawnCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnCurve, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {friendlyName} is to spawn as the day progresses. (Key ranges from 0-1 ).\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunTimeMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunTimeMultiplier, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {friendlyName} can be stunned.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.doorSpeedMultiplier = enemyConfig.BindGlobal(masterTypeConfig.doorSpeedMultiplier, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {friendlyName} to open a door.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunGameDifficultyMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunGameDifficultyMultiplier, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunnable = enemyConfig.BindGlobal(masterTypeConfig.stunnable, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {friendlyName} can be stunned.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.killable = enemyConfig.BindGlobal(masterTypeConfig.killable, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {friendlyName} can die.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.enemyHp = enemyConfig.BindGlobal(masterTypeConfig.enemyHp, tablename, "EnemyHp", enemyType.enemyPrefab != null ? enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP : 3, $"The initial amount of health a(n) {friendlyName} has.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.spawnFalloffCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnFalloffCurve, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {friendlyName} is to spawn based on how many already have been spawned. (Key is number of {friendlyName}/10).\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.useSpawnFalloff = enemyConfig.BindGlobal(masterTypeConfig.useSpawnFalloff, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {friendlyName} there are inside.\nAlternate values: DEFAULT, GLOBAL");

                        enemies.enemyTypes.Add(typeConfiguration);
                    }
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }

            // Process daytime enemies
            {
                daytimeEnemies.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "DaytimeEnemiesEnabled", false, $"Enables/disables custom daytime enemy spawn rate modification for {level.PlanetName}. Typically enemies like manticoils, locusts, etc.");

                if (daytimeEnemies.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile enemyConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.DAYTIME_ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                    daytimeEnemies.maxPowerCount = enemyConfig.BindGlobal(masterConfig.daytimeEnemyConfiguration.maxPowerCount, "General", "MaxPowerCount", level.maxDaytimeEnemyPowerCount, "Maximum total power level allowed for daytime enemies\nAlternate values: DEFAULT, GLOBAL");
                    daytimeEnemies.spawnAmountCurve = enemyConfig.BindGlobal(masterConfig.daytimeEnemyConfiguration.spawnAmountCurve, "General", "SpawnAmountCurve", level.daytimeEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1)\nAlternate values: DEFAULT, GLOBAL");
                    daytimeEnemies.spawnAmountRange = enemyConfig.BindGlobal(masterConfig.daytimeEnemyConfiguration.spawnAmountRange, "General", "SpawnAmountRange", level.daytimeEnemiesProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies\nAlternate values: DEFAULT, GLOBAL");

                    Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.DaytimeEnemies);
                    foreach (EnemyType enemyType in levelInfo.globalInfo.allEnemyTypes)
                    {
                        DaytimeEnemyTypeConfiguration typeConfiguration = new DaytimeEnemyTypeConfiguration(enemyType);

                        GlobalDaytimeEnemyTypeConfiguration masterTypeConfig = masterConfig.daytimeEnemyConfiguration.enemyTypeConfigurations[enemyType];
                        string friendlyName = enemyType.getFriendlyName();
                        typeConfiguration.rarity = enemyConfig.BindGlobal(masterTypeConfig.rarity, "Rarity", enemyType.name, enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {friendlyName} relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.\nAlternate values: DEFAULT, GLOBAL");

                        string tablename = $"EnemyTypes.{enemyType.name}";
                        typeConfiguration.maxEnemyCount = enemyConfig.BindGlobal(masterTypeConfig.maxEnemyCount, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {friendlyName} allowed at once.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.powerLevel = enemyConfig.BindGlobal(masterTypeConfig.powerLevel, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {friendlyName} contributes to the maximum power level.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.spawnCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnCurve, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {friendlyName} is to spawn as the day progresses. (Key ranges from 0-1). \nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunTimeMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunTimeMultiplier, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {friendlyName} can be stunned.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.doorSpeedMultiplier = enemyConfig.BindGlobal(masterTypeConfig.doorSpeedMultiplier, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {friendlyName} to open a door.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunGameDifficultyMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunGameDifficultyMultiplier, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunnable = enemyConfig.BindGlobal(masterTypeConfig.stunnable, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {friendlyName} can be stunned.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.killable = enemyConfig.BindGlobal(masterTypeConfig.killable, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {friendlyName} can die.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.enemyHp = enemyConfig.BindGlobal(masterTypeConfig.enemyHp, tablename, "EnemyHp", enemyType.enemyPrefab != null ? enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP : 3, $"The initial amount of health a(n) {friendlyName} has.\nAlternate values: DEFAULT, GLOBAL");
                        // Not implemented for daytime enemies
                        //typeConfiguration.spawnFalloffCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnFalloffCurve, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {friendlyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}/10). This does not work for daytime enemies. The default value is {{0}}");
                        //typeConfiguration.useSpawnFalloff = enemyConfig.BindGlobal(masterTypeConfig.useSpawnFalloff, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {friendlyName} there are. The default value is {{0}}");

                        daytimeEnemies.enemyTypes.Add(typeConfiguration);
                    }
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }

            // Process outside enemies
            {
                outsideEnemies.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "OutsideEnemiesEnabled", false, $"Enables/disables custom outside enemy spawn rate modification for {level.PlanetName}. Typically eyeless dogs, forest giants, etc.");

                if (outsideEnemies.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile enemyConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.OUTSIDE_ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                    outsideEnemies.maxPowerCount = enemyConfig.BindGlobal(masterConfig.outsideEnemyConfiguration.maxPowerCount, "General", "MaxPowerCount", level.maxOutsideEnemyPowerCount, "Maximum total power level allowed for outside enemies\nAlternate values: DEFAULT, GLOBAL");
                    outsideEnemies.spawnAmountCurve = enemyConfig.BindGlobal(masterConfig.outsideEnemyConfiguration.spawnAmountCurve, "General", "SpawnAmountCurve", level.outsideEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses, (Key ranges from 0-1)\nAlternate values: DEFAULT, GLOBAL");
                    // Hardcoded to 3 internally
                    //outsideEnemies.spawnAmountRange = enemyConfig.BindGlobal("General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                    Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.OutsideEnemies);
                    foreach (EnemyType enemyType in levelInfo.globalInfo.allEnemyTypes)
                    {
                        EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);

                        GlobalEnemyTypeConfiguration masterTypeConfig = masterConfig.outsideEnemyConfiguration.enemyTypeConfigurations[enemyType];
                        string friendlyName = enemyType.getFriendlyName();
                        typeConfiguration.rarity = enemyConfig.BindGlobal(masterTypeConfig.rarity, "Rarity", enemyType.name, enemySpawnRarities.GetValueOrDefault(enemyType, 0), $"Rarity of a(n) {friendlyName} relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.\nAlternate values: DEFAULT, GLOBAL");

                        string tablename = $"EnemyTypes.{enemyType.name}";
                        typeConfiguration.maxEnemyCount = enemyConfig.BindGlobal(masterTypeConfig.maxEnemyCount, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {friendlyName} allowed at once.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.powerLevel = enemyConfig.BindGlobal(masterTypeConfig.powerLevel, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {friendlyName} contributes to the maximum power level.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.spawnCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnCurve, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {friendlyName} allowed at once.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunTimeMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunTimeMultiplier, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {friendlyName} can be stunned.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.doorSpeedMultiplier = enemyConfig.BindGlobal(masterTypeConfig.doorSpeedMultiplier, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {friendlyName} to open a door.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunGameDifficultyMultiplier = enemyConfig.BindGlobal(masterTypeConfig.stunGameDifficultyMultiplier, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.stunnable = enemyConfig.BindGlobal(masterTypeConfig.stunnable, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {friendlyName} can be stunned.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.killable = enemyConfig.BindGlobal(masterTypeConfig.killable, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {friendlyName} can die.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.enemyHp = enemyConfig.BindGlobal(masterTypeConfig.enemyHp, tablename, "EnemyHp", enemyType.enemyPrefab != null ? enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP : 3, $"The initial amount of health a(n) {friendlyName} has.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.spawnFalloffCurve = enemyConfig.BindGlobal(masterTypeConfig.spawnFalloffCurve, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {friendlyName} is to spawn based on how many have already been spawned. (Key is number of {friendlyName} allowed at once.\nAlternate values: DEFAULT, GLOBAL");
                        typeConfiguration.useSpawnFalloff = enemyConfig.BindGlobal(masterTypeConfig.useSpawnFalloff, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {friendlyName} there are.\nAlternate values: DEFAULT, GLOBAL");

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
                scrap.enabled = levelInfo.mainConfigFile.Bind($"Level.{levelInfo.level.name}", "ScrapEnabled", false, $"Enables/disables custom scrap generation modifications for {level.PlanetName}");

                if (scrap.enabled.Value)
                {
                    GlobalConfiguration masterConfig = levelInfo.masterConfig;
                    ConfigFile scrapConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.SCRAP_CFG_NAME), true);
                    scrapConfig.SaveOnConfigSet = false;
                    scrap.minScrap = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.minScrap, "General", "MinScrapCount", level.minScrap, "Minimum total number of scrap generated in the level\nAlternate values: DEFAULT, GLOBAL");
                    scrap.maxScrap = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.maxScrap, "General", "MaxScrapCount", level.maxScrap, "Maximum total number of scrap generated in the level\nAlternate values: DEFAULT, GLOBAL");
                    scrap.scrapAmountMultiplier = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.scrapAmountMultiplier, "General", "ScrapAmountMultiplier", RoundManager.Instance.scrapAmountMultiplier, "Modifier to the total amount of scrap generated in the level.\nAlternate values: DEFAULT, GLOBAL");
                    scrap.scrapValueMultiplier = scrapConfig.BindGlobal(masterConfig.scrapConfiguration.scrapValueMultiplier, "General", "ScrapValueMultiplier", RoundManager.Instance.scrapValueMultiplier, "Modifier to the total value of scrap generated in the level.\nAlternate values: DEFAULT, GLOBAL");
                    Dictionary<Item, int> itemSpawnRarities = convertToDictionary(level.spawnableScrap);
                    foreach (Item itemType in levelInfo.globalInfo.allItems)
                    {
                        ItemConfiguration configuration;
                        string tablename = $"ItemType.{itemType.name}";
                        GlobalItemConfiguration mainItemConfig = masterConfig.scrapConfiguration.itemConfigurations[itemType];
                        if (mainItemConfig is GlobalItemScrapConfiguration)
                        {
                            GlobalItemScrapConfiguration masterTypeConfig = mainItemConfig as GlobalItemScrapConfiguration;
                            ScrapItemConfiguration itemConfiguration = new ScrapItemConfiguration(itemType);
                            configuration = itemConfiguration;

                            itemConfiguration.minValue = scrapConfig.BindGlobal(masterTypeConfig.minValue, tablename, "MinValue", Math.Min(itemType.minValue, itemType.maxValue), $"Minimum value of a {itemType.itemName}.\nAlternate values: DEFAULT, GLOBAL");
                            itemConfiguration.maxValue = scrapConfig.BindGlobal(masterTypeConfig.maxValue, tablename, "MaxValue", Math.Max(itemType.minValue, itemType.maxValue), $"Maximum value of a {itemType.itemName}.\nAlternate values: DEFAULT, GLOBAL");
                        }
                        else
                        {
                            configuration = new ItemConfiguration(itemType);
                        }

                        configuration.rarity = scrapConfig.BindGlobal(mainItemConfig.rarity, "Rarity", itemType.name, itemSpawnRarities.GetValueOrDefault(itemType, 0), $"Rarity of a(n) {itemType.itemName} relative to the total rarity of all other item types combined. A higher rarity increases the chance that the item will spawn.\nAlternate values: DEFAULT, GLOBAL");

                        configuration.conductive = scrapConfig.BindGlobal(mainItemConfig.conductive, tablename, "Conductive", itemType.isConductiveMetal, $"Whether or not {itemType.itemName} is conductive(can be struck by lightning).\nAlternate values: DEFAULT, GLOBAL");
                        scrap.scrapRarities.Add(configuration);
                    }
                    scrapConfig.SaveOnConfigSet = true;
                    scrapConfig.Save();
                }
            }
        }

        private void instantiateDungeonConfigs(LevelInformation levelInfo)
        {
            SelectableLevel level = levelInfo.level;
            dungeon.enabled = levelInfo.mainConfigFile.Bind($"Level.{level.name}", "DungeonGenerationEnabled", false, $"Enables/disables custom dungeon generation modifications for {level.PlanetName}");

            if (dungeon.enabled.Value)
            {
                GlobalConfiguration masterConfig = levelInfo.masterConfig;
                ConfigFile dungeonConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.DUNGEON_GENERATION_CFG_NAME), true);
                dungeonConfig.SaveOnConfigSet = false;
                dungeon.mapSizeMultiplier = dungeonConfig.BindGlobal(masterConfig.dungeonConfiguration.mapSizeMultiplier, "General", "MapSizeMultiplier", RoundManager.Instance.mapSizeMultiplier, "Size modifier of the dungeon generated.\nAlternate values: DEFAULT, GLOBAL");
                Dictionary<DungeonFlow, int> flowRarities = new Dictionary<DungeonFlow, int>();
                foreach (IntWithRarity flow in level.dungeonFlowTypes)
                {
                    flowRarities.TryAdd(RoundManager.Instance.dungeonFlowTypes[flow.id], flow.rarity);
                }

                foreach (DungeonFlow flow in levelInfo.globalInfo.allDungeonFlows)
                {
                    GlobalDungeonFlowConfiguration masterFlowConfig = masterConfig.dungeonConfiguration.dungeonFlowConfigurations[flow.name];
                    DungeonFlowConfiguration dungeonFlowConfig = new DungeonFlowConfiguration();

                    dungeonFlowConfig.rarity = dungeonConfig.BindGlobal(masterFlowConfig.rarity, "Rarity", flow.name, flowRarities.GetValueOrDefault(flow, 0), $"Rarity of creating a dungeon using {flow.name} as the generator.\nAlternate values: DEFAULT, GLOBAL");

                    string tablename = $"DungeonFlow.{flow.name}";
                    dungeonFlowConfig.factorySizeMultiplier = dungeonConfig.BindGlobal(masterFlowConfig.factorySizeMultiplier, tablename, "FactorySizeMultiplier", level.factorySizeMultiplier, $"Size of the dungeon when using this dungeon flow.\nAlternate values: DEFAULT, GLOBAL");

                    dungeon.dungeonFlowConfigurations.Add(flow.name, dungeonFlowConfig);
                }
                dungeonConfig.SaveOnConfigSet = true;
                dungeonConfig.Save();
            }
        }

        private void instantiateTrapConfigs(LevelInformation levelInfo)
        {
            SelectableLevel level = levelInfo.level;
            trap.enabled = levelInfo.mainConfigFile.Bind($"Level.{level.name}", "TrapEnabled", false, $"Enables/disables custom trap generation modifications for {level.PlanetName}");

            if (trap.enabled.Value)
            {
                GlobalConfiguration masterConfig = levelInfo.masterConfig;
                ConfigFile trapConfig = new ConfigFile(Path.Combine(levelInfo.levelSaveDir, GlobalConfiguration.TRAP_CFG_NAME), true);
                trapConfig.SaveOnConfigSet = false;
                Dictionary<GameObject, AnimationCurve> defaultSpawnAmounts = new Dictionary<GameObject, AnimationCurve>();
                foreach (SpawnableMapObject obj in level.spawnableMapObjects)
                {
                    defaultSpawnAmounts.TryAdd(obj.prefabToSpawn, obj.numberToSpawn);
                }
                foreach (DirectionalSpawnableMapObject mapObject in levelInfo.globalInfo.allSpawnableMapObjects)
                {
                    GlobalSpawnableMapObjectConfiguration masterTrapConfig = masterConfig.trapConfiguration.traps[mapObject.obj];
                    SpawnableMapObjectConfiguration configuration = new SpawnableMapObjectConfiguration(mapObject);

                    string tablename = $"Trap.{mapObject.obj.name}";
                    configuration.numberToSpawn = trapConfig.BindGlobal(masterTrapConfig.numberToSpawn, tablename, "SpawnAmount", defaultSpawnAmounts.GetValueOrDefault(mapObject.obj, new AnimationCurve()), $"The amount of this trap to spawn. 'Y Axis is the amount to be spawned; X axis should be from 0 to 1 and is randomly picked from.'\nAlternate values: DEFAULT, GLOBAL");

                    trap.traps.Add(mapObject.obj, configuration);
                }
                trapConfig.SaveOnConfigSet = true;
                trapConfig.Save();
            }

        }
        private static Dictionary<EnemyType, int> convertToDictionary(List<SpawnableEnemyWithRarity> enemies)
        {
            Dictionary<EnemyType, int> enemySpawnRarities = new Dictionary<EnemyType, int>();
            foreach (SpawnableEnemyWithRarity enemy in enemies)
            {
                if (!enemySpawnRarities.TryAdd(enemy.enemyType, enemy.rarity))
                {
                    enemySpawnRarities[enemy.enemyType] += enemy.rarity;
                }
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

using BepInEx;
using BepInEx.Configuration;
using DunGen.Graph;
using HarmonyLib;
using LethalQuantities.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LethalQuantities.Objects
{
    public interface IWeightConfigurable
    {
        public CustomEntry<float> weight { get; set; }
    }

    public class GlobalItemScrapConfiguration : GlobalItemConfiguration, IScrappableConfiguration
    {
        public GlobalItemScrapConfiguration(Item item) : base(item)
        {
        }

        public CustomEntry<int> minValue { get; set; }
        public CustomEntry<int> maxValue { get; set; }

        public override bool isDefault()
        {
            return base.isDefault() && minValue.isDefault() && maxValue.isDefault();
        }
    }

    public class GlobalItemConfiguration : ItemConfiguration, IWeightConfigurable
    {
        public GlobalItemConfiguration(Item item) : base(item)
        {
            this.item = item;
        }

        public CustomEntry<float> weight { get; set; }
    }

    public class GlobalConfiguration
    {
        public static readonly string ENEMY_CFG_NAME = "Enemies.cfg";
        public static readonly string DAYTIME_ENEMY_CFG_NAME = "DaytimeEnemies.cfg";
        public static readonly string OUTSIDE_ENEMY_CFG_NAME = "OutsideEnemies.cfg";
        public static readonly string SCRAP_CFG_NAME = "Scrap.cfg";
        public static readonly string DUNGEON_GENERATION_CFG_NAME = "DungeonGeneration.cfg";
        public static readonly string TRAP_CFG_NAME = "Traps.cfg";
        public static readonly string PRICES_CFG_NAME = "Prices.cfg";
        public static readonly string FILES_CFG_NAME = "Configuration.cfg";

        public EnemyConfiguration<EnemyTypeConfiguration> enemyConfiguration { get; private set; } = new EnemyConfiguration<EnemyTypeConfiguration>();
        public EnemyConfiguration<DaytimeEnemyTypeConfiguration> daytimeEnemyConfiguration { get; private set; } = new EnemyConfiguration<DaytimeEnemyTypeConfiguration>();
        public OutsideEnemyConfiguration<EnemyTypeConfiguration> outsideEnemyConfiguration { get; private set; } = new OutsideEnemyConfiguration<EnemyTypeConfiguration>();
        public ScrapConfiguration scrapConfiguration { get; private set; } = new ScrapConfiguration();
        public DungeonGenerationConfiguration dungeonConfiguration { get; private set; } = new DungeonGenerationConfiguration();
        public TrapConfiguration trapConfiguration { get; private set; } = new TrapConfiguration();
        public PriceConfiguration priceConfiguration { get; private set; } = new PriceConfiguration();
        public Dictionary<string, LevelConfiguration> levelConfigs { get; } = new Dictionary<string, LevelConfiguration>();

        public GlobalConfiguration(GlobalInformation globalInfo)
        {
            instantiateConfigs(globalInfo);
        }

        private void instantiateConfigs(GlobalInformation globalInfo)
        {
            ConfigFile fileConfigFile = new ConfigFile(Path.Combine(globalInfo.configSaveDir, FILES_CFG_NAME), true);
            fileConfigFile.SaveOnConfigSet = false;

            instantiateEnemyConfigs(globalInfo, fileConfigFile);
            instantiateScrapConfigs(globalInfo, fileConfigFile);
            instantiateDungeonConfigs(globalInfo, fileConfigFile);
            instantiateTrapConfigs(globalInfo, fileConfigFile);
            instantiatePriceConfigs(globalInfo, fileConfigFile);
            instantiateMoonConfig(globalInfo, fileConfigFile);

            fileConfigFile.SaveOnConfigSet = true;
            fileConfigFile.Save();
        }

        private void instantiateMoonConfig(GlobalInformation globalInfo, ConfigFile fileConfigFile)
        {
            // Config file for enabling/disabling individual moon config files
            foreach (SelectableLevel level in globalInfo.allSelectableLevels.Keys)
            {
                string levelSaveDir = Path.Combine(globalInfo.moonSaveDir, level.name);
                LevelInformation levelInfo = new LevelInformation(this, globalInfo, level, levelSaveDir, fileConfigFile);

                levelConfigs.Add(level.name, new LevelConfiguration(levelInfo));
            }
        }

        private void instantiateEnemyConfigs(GlobalInformation globalInfo, ConfigFile fileConfigFile)
        {
            // Process enemies
            {
                enemyConfiguration.enabled = fileConfigFile.Bind("Global", "EnemiesEnabled", false, "Whether or not to enable the global config for all inside enemies");

                ConfigFile enemyConfig = null;
                if (enemyConfiguration.enabled.Value)
                {
                    enemyConfig = new ConfigFile(Path.Combine(globalInfo.configSaveDir, ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                }

                enemyConfiguration.maxPowerCount = BindEmptyOrNonDefaultable<int>(enemyConfig, "General", "MaxPowerCount", "Maximum total power level allowed for inside enemies.\nLeave blank or DEFAULT to use the moon's default rarity.");
                enemyConfiguration.spawnAmountCurve = BindEmptyOrNonDefaultable<AnimationCurve>(enemyConfig, "General", "SpawnAmountCurve", "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1 ).\nLeave blank or DEFAULT to use the moon's default rarity.");
                enemyConfiguration.spawnAmountRange = BindEmptyOrNonDefaultable<float>(enemyConfig, "General", "SpawnAmountRange", "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.\nLeave blank or DEFAULT to use the moon's default rarity.");

                foreach (EnemyType enemyType in globalInfo.allEnemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);

                    // Use a separate table for rarity
                    typeConfiguration.rarity = BindEmptyOrNonDefaultable<int>(enemyConfig, "Rarity", enemyType.name, $"Rarity of a(n) {enemyType.enemyName} spawning relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.\nLeave blank or DEFAULT to use the moon's default rarity.");

                    string tablename = $"EnemyTypes.{enemyType.name}";
                    typeConfiguration.maxEnemyCount = BindEmptyOrDefaultable(enemyConfig, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once.\nAlternate values: DEFAULT");
                    typeConfiguration.powerLevel = BindEmptyOrDefaultable(enemyConfig, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a single {enemyType.enemyName} contributes to the maximum power level\nAlternate values: DEFAULT");
                    typeConfiguration.spawnCurve = BindEmptyOrDefaultable(enemyConfig, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses. (Key ranges from 0-1 )\nAlternate values: DEFAULT");
                    typeConfiguration.stunTimeMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {enemyType.enemyName} can be stunned\nAlternate values: DEFAULT");
                    typeConfiguration.doorSpeedMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {enemyType.enemyName} to open a door\nAlternate values: DEFAULT");
                    typeConfiguration.stunGameDifficultyMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does\nAlternate values: DEFAULT");
                    typeConfiguration.stunnable = BindEmptyOrDefaultable(enemyConfig, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {enemyType.enemyName} can be stunned\nAlternate values: DEFAULT");
                    typeConfiguration.killable = BindEmptyOrDefaultable(enemyConfig, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {enemyType.enemyName} can die\nAlternate values: DEFAULT");
                    typeConfiguration.enemyHp = BindEmptyOrDefaultable(enemyConfig, tablename, "EnemyHp", enemyType.enemyPrefab != null ? enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP : 3, $"The initial amount of health a(n) {enemyType.enemyName} has\nAlternate values: DEFAULT");
                    typeConfiguration.spawnFalloffCurve = BindEmptyOrDefaultable(enemyConfig, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many already have been spawned. (Key is number of {enemyType.enemyName}s/10).\nAlternate values: DEFAULT");
                    typeConfiguration.useSpawnFalloff = BindEmptyOrDefaultable(enemyConfig, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are inside.\nAlternate values: DEFAULT");

                    enemyConfiguration.enemyTypes.Add(enemyType, typeConfiguration);
                }

                if (enemyConfiguration.enabled.Value)
                {
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }

            // Process daytime enemies
            {
                daytimeEnemyConfiguration.enabled = fileConfigFile.Bind("Global", "DaytimeEnemiesEnabled", false, "Whether or not to enable the global config for all daytime enemies. Typically enemies like manticoils, locusts, etc.");

                ConfigFile enemyConfig = null;
                if (daytimeEnemyConfiguration.enabled.Value)
                {
                    enemyConfig = new ConfigFile(Path.Combine(globalInfo.configSaveDir, DAYTIME_ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                }

                daytimeEnemyConfiguration.maxPowerCount = BindEmptyOrNonDefaultable<int>(enemyConfig, "General", "MaxPowerCount", "Maximum total power level allowed for inside enemies.\nLeave blank or DEFAULT to use the moon's default rarity.");
                daytimeEnemyConfiguration.spawnAmountCurve = BindEmptyOrNonDefaultable<AnimationCurve>(enemyConfig, "General", "SpawnAmountCurve", "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1 ).\nLeave blank or DEFAULT to use the moon's default rarity.");
                daytimeEnemyConfiguration.spawnAmountRange = BindEmptyOrNonDefaultable<float>(enemyConfig, "General", "SpawnAmountRange", "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.\nLeave blank or DEFAULT to use the moon's default rarity.");

                foreach (EnemyType enemyType in globalInfo.allEnemyTypes)
                {
                    DaytimeEnemyTypeConfiguration typeConfiguration = new DaytimeEnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.name}";

                    // Use a separate table for rarity
                    typeConfiguration.rarity = BindEmptyOrNonDefaultable<int>(enemyConfig, "Rarity", enemyType.name, $"Rarity of a(n) {enemyType.enemyName} spawning relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.\nLeave blank or DEFAULT to use the moon's default rarity.");

                    typeConfiguration.maxEnemyCount = BindEmptyOrDefaultable(enemyConfig, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once\nAlternate values: DEFAULT");
                    typeConfiguration.powerLevel = BindEmptyOrDefaultable(enemyConfig, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {enemyType.enemyName} contributes to the maximum power level\nAlternate values: DEFAULT");
                    typeConfiguration.spawnCurve = BindEmptyOrDefaultable(enemyConfig, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses. (Key ranges from 0-1 )\nAlternate values: DEFAULT");
                    typeConfiguration.stunTimeMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {enemyType.enemyName} can be stunned\nAlternate values: DEFAULT");
                    typeConfiguration.doorSpeedMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {enemyType.enemyName} to open a door\nAlternate values: DEFAULT");
                    typeConfiguration.stunGameDifficultyMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does\nAlternate values: DEFAULT");
                    typeConfiguration.stunnable = BindEmptyOrDefaultable(enemyConfig, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {enemyType.enemyName} can be stunned\nAlternate values: DEFAULT");
                    typeConfiguration.killable = BindEmptyOrDefaultable(enemyConfig, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {enemyType.enemyName} can die\nAlternate values: DEFAULT");
                    typeConfiguration.enemyHp = BindEmptyOrDefaultable(enemyConfig, tablename, "EnemyHp", enemyType.enemyPrefab != null ? enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP : 3, $"The initial amount of health a(n) {enemyType.enemyName} has\nAlternate values: DEFAULT");
                    // No spawn curve falloff is implemented for daytime enemies
                    //typeConfiguration.spawnFalloffCurve = BindEmptyOrDefaultable(enemyConfig, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}s/10). This does not work for daytime enemies.");
                    //typeConfiguration.useSpawnFalloff = BindEmptyOrDefaultable(enemyConfig, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are.");

                    daytimeEnemyConfiguration.enemyTypes.Add(enemyType, typeConfiguration);
                }

                if (daytimeEnemyConfiguration.enabled.Value)
                {
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }

            // Process outside enemies
            {
                outsideEnemyConfiguration.enabled = fileConfigFile.Bind("Global", "OutsideEnemiesEnabled", false, "Whether or not to enable the global config for all outside enemies. Typically eyeless dogs, forest giants, etc.");

                ConfigFile enemyConfig = null;
                if (outsideEnemyConfiguration.enabled.Value)
                {
                    enemyConfig = new ConfigFile(Path.Combine(globalInfo.configSaveDir, OUTSIDE_ENEMY_CFG_NAME), true);
                    enemyConfig.SaveOnConfigSet = false;
                }

                outsideEnemyConfiguration.maxPowerCount = BindEmptyOrNonDefaultable<int>(enemyConfig, "General", "MaxPowerCount", "Maximum total power level allowed for inside enemies.\nLeave blank or DEFAULT to use the moon's default rarity.");
                outsideEnemyConfiguration.spawnAmountCurve = BindEmptyOrNonDefaultable<AnimationCurve>(enemyConfig, "General", "SpawnAmountCurve", "How many enemies can spawn enemy as the day progresses. (Key ranges from 0-1 ).\nLeave blank or DEFAULT to use the moon's default rarity.");

                foreach (EnemyType enemyType in globalInfo.allEnemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.name}";

                    // Use a separate table for rarity
                    typeConfiguration.rarity = BindEmptyOrNonDefaultable<int>(enemyConfig, "Rarity", enemyType.name, $"Rarity of a(n) {enemyType.enemyName} spawning relative to the total rarity of all other enemy types combined. A higher rarity increases the chance that the enemy will spawn.\nLeave blank or DEFAULT to use the moon's default rarity.");

                    typeConfiguration.maxEnemyCount = BindEmptyOrDefaultable(enemyConfig, tablename, "MaxEnemyCount", enemyType.MaxCount, $"Maximum amount of {enemyType.enemyName}s allowed at once\nAlternate values: DEFAULT");
                    typeConfiguration.powerLevel = BindEmptyOrDefaultable(enemyConfig, tablename, "PowerLevel", enemyType.PowerLevel, $"How much a(n) {enemyType.enemyName} contributes to the maximum power level\nAlternate values: DEFAULT");
                    typeConfiguration.spawnCurve = BindEmptyOrDefaultable(enemyConfig, tablename, "SpawnChanceCurve", enemyType.probabilityCurve, $"How likely a(n) {enemyType.enemyName} is to spawn as the day progresses, (Key ranges from 0-1)\nAlternate values: DEFAULT");
                    typeConfiguration.stunTimeMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "StunTimeMultiplier", enemyType.stunTimeMultiplier, $"The multiplier for how long a(n) {enemyType.enemyName} can be stunned\nAlternate values: DEFAULT");
                    typeConfiguration.doorSpeedMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "DoorSpeedMultiplier", enemyType.doorSpeedMultiplier, $"The multiplier for how long it takes a(n) {enemyType.enemyName} to open a door\nAlternate values: DEFAULT");
                    typeConfiguration.stunGameDifficultyMultiplier = BindEmptyOrDefaultable(enemyConfig, tablename, "StunGameDifficultyMultiplier", enemyType.stunGameDifficultyMultiplier, $"I don't know what this does\nAlternate values: DEFAULT");
                    typeConfiguration.stunnable = BindEmptyOrDefaultable(enemyConfig, tablename, "Stunnable", enemyType.canBeStunned, $"Whether or not a(n) {enemyType.enemyName} can be stunned\nAlternate values: DEFAULT");
                    typeConfiguration.killable = BindEmptyOrDefaultable(enemyConfig, tablename, "Killable", enemyType.canDie, $"Whether or not a(n) {enemyType.enemyName} can die\nAlternate values: DEFAULT");
                    typeConfiguration.enemyHp = BindEmptyOrDefaultable(enemyConfig, tablename, "EnemyHp", enemyType.enemyPrefab != null ? enemyType.enemyPrefab.GetComponent<EnemyAI>().enemyHP : 3, $"The initial amount of health a(n) {enemyType.enemyName} has\nAlternate values: DEFAULT");
                    typeConfiguration.spawnFalloffCurve = BindEmptyOrDefaultable(enemyConfig, tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, $"The spawning curve multiplier of how less/more likely a(n) {enemyType.enemyName} is to spawn based on how many have already been spawned. (Key is number of {enemyType.enemyName}s/10)\nAlternate values: DEFAULT");
                    typeConfiguration.useSpawnFalloff = BindEmptyOrDefaultable(enemyConfig, tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, $"Whether or not to modify spawn rates based on how many existing {enemyType.enemyName}s there are\nAlternate values: DEFAULT");

                    outsideEnemyConfiguration.enemyTypes.Add(enemyType, typeConfiguration);
                }

                if (outsideEnemyConfiguration.enabled.Value)
                {
                    enemyConfig.SaveOnConfigSet = true;
                    enemyConfig.Save();
                }
            }
        }

        private void instantiateScrapConfigs(GlobalInformation globalInfo, ConfigFile fileConfigFile)
        {
            scrapConfiguration.enabled = fileConfigFile.Bind("Global", "ScrapEnabled", false, "Whether or not to enable the global config for all scrap generation.");

            ConfigFile scrapConfig = null;
            if (scrapConfiguration.enabled.Value)
            {
                scrapConfig = new ConfigFile(Path.Combine(globalInfo.configSaveDir, SCRAP_CFG_NAME), true);
                scrapConfig.SaveOnConfigSet = false;
            }

            scrapConfiguration.minScrap = BindEmptyOrNonDefaultable<int>(scrapConfig, "General", "MinScrapCount", "Minimum total number of scrap generated in the level, inclusive.\nLeave blank or DEFAULT to use the moon's default min scrap count.");
            scrapConfiguration.maxScrap = BindEmptyOrNonDefaultable<int>(scrapConfig, "General", "MaxScrapCount", "Maximum total number of scrap generated in the level, exclusive.\nLeave blank or DEFAULT to use the moon's default max scrap count.");
            scrapConfiguration.scrapAmountMultiplier = BindEmptyOrDefaultable(scrapConfig, "General", "ScrapAmountMultiplier", globalInfo.manager.scrapAmountMultiplier, "Modifier to the total amount of scrap generated in the level.\nAlternate values: DEFAULT");
            scrapConfiguration.scrapValueMultiplier = BindEmptyOrDefaultable(scrapConfig, "General", "ScrapValueMultiplier", globalInfo.manager.scrapValueMultiplier, "Modifier to the total value of scrap generated in the level.\nAlternate values: DEFAULT");
            foreach (Item itemType in globalInfo.allItems)
            {
                GlobalItemConfiguration itemConfiguration;
                string tablename = $"ItemType.{itemType.name}";
                if (itemType.isScrap)
                {
                    GlobalItemScrapConfiguration configuration = new GlobalItemScrapConfiguration(itemType);
                    itemConfiguration = configuration;

                    configuration.minValue = BindEmptyOrDefaultable(scrapConfig, tablename, "MinValue", Math.Min(itemType.minValue, itemType.maxValue), $"Minimum value of a {itemType.itemName}, inclusive.\nAlternate values: DEFAULT");
                    configuration.maxValue = BindEmptyOrDefaultable(scrapConfig, tablename, "MaxValue", Math.Max(itemType.minValue, itemType.maxValue), $"Maximum value of a {itemType.itemName}, exclusive.\nAlternate values: DEFAULT");
                } else
                {
                    itemConfiguration = new GlobalItemConfiguration(itemType);
                }

                itemConfiguration.rarity = BindEmptyOrNonDefaultable<int>(scrapConfig, "Rarity", itemType.name, $"Rarity of a(n) {itemType.itemName} relative to the total rarity of all other item types combined. A higher rarity increases the chance that the item will spawn.\nLeave blank or DEFAULT to use the moon's default rarity.");

                itemConfiguration.weight = BindEmptyOrDefaultable(scrapConfig, tablename, "Weight", itemType.weight, $"The weight of a(n) {itemType.itemName}. The in-game weight can be found by the formula: pounds = (value - 1) * 100. For example, a value of 1.18 means the object weighs 18lbs. This option can only be set in the global config.\nAlternate values: DEFAULT");
                itemConfiguration.conductive = BindEmptyOrDefaultable(scrapConfig, tablename, "Conductive", itemType.isConductiveMetal, $"Whether or not {itemType.itemName} is conductive(can be struck by lightning).\nAlternate values: DEFAULT");

                scrapConfiguration.items.Add(itemType, itemConfiguration);
            }

            if (scrapConfiguration.enabled.Value)
            {
                scrapConfig.SaveOnConfigSet = true;
                scrapConfig.Save();
            }
        }

        private void instantiateDungeonConfigs(GlobalInformation globalInfo, ConfigFile fileConfigFile)
        {
            dungeonConfiguration.enabled = fileConfigFile.Bind("Global", "DungeonGenerationEnabled", false, "Whether or not to enable the global config for all dungeon generation.");

            ConfigFile dungeonFile = null;
            if (dungeonConfiguration.enabled.Value)
            {
                dungeonFile = new ConfigFile(Path.Combine(globalInfo.configSaveDir, DUNGEON_GENERATION_CFG_NAME), true);
                dungeonFile.SaveOnConfigSet = false;
            }

            dungeonConfiguration.mapSizeMultiplier = BindEmptyOrDefaultable(dungeonFile, "General", "MapSizeMultiplier", globalInfo.manager.mapSizeMultiplier, "The multiplier to use for determining the size of the dungeon.\nAlternate values: DEFAULT");
            foreach (DungeonFlow flow in globalInfo.allDungeonFlows)
            {
                DungeonFlowConfiguration configuration = new DungeonFlowConfiguration();
                configuration.rarity = BindEmptyOrNonDefaultable<int>(dungeonFile, "Rarity", flow.name, $"Rarity of a moon using a {flow.name} dungeon generator as the interior. A higher rarity increases the chance that the moon will use this dungeon flow.\nLeave blank or DEFAULT to use the moon's default rarity.");

                string tablename = $"DungeonFlow." + flow.name;
                configuration.factorySizeMultiplier = BindEmptyOrNonDefaultable<float>(dungeonFile, tablename, "FactorySizeMultiplier", $"Size of the dungeon when using this dungeon flow.\nLeave blank or DEFAULT to use the moon's default factory size multiplier.\nAlternate values: DEFAULT");

                dungeonConfiguration.dungeonFlowConfigurations.Add(flow.name, configuration);
            }

            if (dungeonConfiguration.enabled.Value)
            {
                dungeonFile.SaveOnConfigSet = true;
                dungeonFile.Save();
            }
        }

        private void instantiateTrapConfigs(GlobalInformation globalInfo, ConfigFile fileConfigFile)
        {
            trapConfiguration.enabled = fileConfigFile.Bind("Global", "TrapEnabled", false, "Whether or not to enable the global config for all trap generation");

            ConfigFile trapFile = null;
            if (trapConfiguration.enabled.Value)
            {
                trapFile = new ConfigFile(Path.Combine(globalInfo.configSaveDir, TRAP_CFG_NAME), true);
                trapFile.SaveOnConfigSet = false;
            }

            foreach (DirectionalSpawnableMapObject mapObject in globalInfo.allSpawnableMapObjects)
            {
                SpawnableMapObjectConfiguration configuration = new SpawnableMapObjectConfiguration(mapObject);
                string tablename = $"Trap.{mapObject.obj.name}";
                configuration.numberToSpawn = BindEmptyOrNonDefaultable<AnimationCurve>(trapFile, tablename, "SpawnAmount", $"The amount of this trap to spawn. 'Y Axis is the amount to be spawned; X axis should be from 0 to 1 and is randomly picked from.'\nAlternate values: DEFAULT");

                trapConfiguration.traps.Add(mapObject.obj, configuration);
            }

            if (trapConfiguration.enabled.Value)
            {
                trapFile.SaveOnConfigSet = true;
                trapFile.Save();
            }
        }

        private void instantiatePriceConfigs(GlobalInformation globalInfo, ConfigFile fileConfigFile)
        {
            priceConfiguration.enabled = fileConfigFile.Bind("Global", "PriceEnabled", false, "Whether or not to enable the global config for all moon pricing");

            ConfigFile priceFile = null;
            if (priceConfiguration.enabled.Value)
            {
                priceFile = new ConfigFile(Path.Combine(globalInfo.configSaveDir, PRICES_CFG_NAME), true);
                priceFile.SaveOnConfigSet = false;
            }

            List<SelectableLevel> levelList = globalInfo.allSelectableLevels.Keys.ToList();
            levelList.Sort(GlobalInformation.SCRIPTABLE_OBJECT_SORTER);
            foreach (SelectableLevel level in levelList)
            {
                if (globalInfo.allSelectableLevels.TryGetValue(level, out GenericLevelInformation info))
                {
                    MoonPriceConfiguration config = new MoonPriceConfiguration(level.name);
                    string tablename = $"Level.{level.name}";

                    config.price = BindEmptyOrDefaultable(priceFile, tablename, "TravelCost", info.price, $"How many credits it costs to travel to {level.PlanetName}.\nAlternate values: DEFAULT");

                    priceConfiguration.moons.Add(level.name, config);
                }
            }

            if (priceConfiguration.enabled.Value)
            {
                priceFile.SaveOnConfigSet = true;
                priceFile.Save();
            }
        }

        private static CustomEntry<T> BindEmptyOrDefaultable<T>(ConfigFile file, string tableName, string name, T defaultValue, string desc)
        {
            if (file == null)
            {
                return new EmptyEntry<T>(defaultValue);
            }
            else
            {
                return file.BindDefaultable(tableName, name, defaultValue, desc);
            }
        }

        private static CustomEntry<T> BindEmptyOrNonDefaultable<T>(ConfigFile file, string tableName, string name, string desc)
        {
            if (file == null)
            {
                return new EmptyEntry<T>();
            }
            else
            {
                return file.BindNonDefaultable<T>(tableName, name, desc);
            }
        }
    }

    public static class ConfigEntryExtension
    {
        public static GlobalConfigEntry<T> BindGlobal<T>(this ConfigFile file, CustomEntry<T> parent, string tablename, string name, T defaultValue, string description)
        {
            TypeConverter converter = TomlTypeConverter.GetConverter(typeof(T));

            ConfigEntry<string> entry = file.Bind(tablename, name, GlobalConfigEntry<T>.GLOBAL_OPTION, description);

            GlobalConfigEntry<T> globalEntry = new GlobalConfigEntry<T>(parent, entry, defaultValue, converter);

            ConfigEntryBasePatch.entries.Add(entry, globalEntry);

            return globalEntry;
        }

        public static DefaultableConfigEntry<T> BindDefaultable<T>(this ConfigFile file, string tablename, string name, T defaultValue, string description)
        {
            TypeConverter converter = TomlTypeConverter.GetConverter(typeof(T));

            ConfigEntry<string> entry = file.Bind(tablename, name, DefaultableConfigEntry<T>.DEFAULT_OPTION, description);

            DefaultableConfigEntry<T> defaultEntry = new DefaultableConfigEntry<T>(entry, defaultValue, converter);

            ConfigEntryBasePatch.entries.Add(entry, defaultEntry);

            return defaultEntry;
        }

        public static NonDefaultableConfigEntry<T> BindNonDefaultable<T>(this ConfigFile file, string tablename, string name, string description)
        {
            TypeConverter converter = TomlTypeConverter.GetConverter(typeof(T));

            ConfigEntry<string> entry = file.Bind(tablename, name, NonDefaultableConfigEntry<T>.DEFAULT_OPTION, description);

            NonDefaultableConfigEntry<T> nonDefaultableEntry = new NonDefaultableConfigEntry<T>(entry, converter);

            ConfigEntryBasePatch.entries.Add(entry, nonDefaultableEntry);

            return nonDefaultableEntry;
        }
    }
}

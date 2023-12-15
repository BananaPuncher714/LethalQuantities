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

    public class LevelConfiguration
    {
        private static readonly string ENEMY_CFG_NAME = "Enemies.cfg";
        private static readonly string DAYTIME_ENEMY_CFG_NAME = "DaytimeEnemies.cfg";
        private static readonly string OUTSIDE_ENEMY_CFG_NAME = "OutsideEnemies.cfg";

        public int levelId { get; }
        public string sceneName { get; }
        private string saveDirectory { get; }

        public EnemyConfiguration enemies { get; }
        public EnemyConfiguration daytimeEnemies { get; }
        public EnemyConfiguration outsideEnemies { get; }

        public LevelConfiguration(string saveDirectory, SelectableLevel level, HashSet<EnemyType> enemyTypes)
        {
            levelId = level.levelID;
            sceneName = level.sceneName;
            this.saveDirectory = saveDirectory;

            enemies = new EnemyConfiguration();
            daytimeEnemies = new EnemyConfiguration();
            outsideEnemies = new EnemyConfiguration();

            instantiateConfigs(level, enemyTypes);
        }

        private void instantiateConfigs(SelectableLevel level, HashSet<EnemyType> enemyTypes)
        {
            instantiateEnemyConfigs(level, enemyTypes);
        }

        private void instantiateEnemyConfigs(SelectableLevel level, HashSet<EnemyType> enemyTypes)
        {
            // Process enemies
            {
                ConfigFile enemyConfig = new ConfigFile(Path.Combine(saveDirectory, ENEMY_CFG_NAME), true);
                enemies.enabled = enemyConfig.Bind("General", "Enabled", false, "Enables/disables custom enemy spawn rate modification");
                enemies.maxPowerCount = enemyConfig.Bind("General", "MaxPowerCount", level.maxEnemyPowerCount, "Maximum total power level allowed for inside enemies.");
                enemies.spawnAmountCurve = enemyConfig.Bind("General", "SpawnAmountCurve", level.enemySpawnChanceThroughoutDay, "How many enemies can spawn enemy as the day progresses, from 0 to 1.");
                enemies.spawnAmountRange = enemyConfig.Bind("General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.Enemies);
                foreach (EnemyType enemyType in enemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.enemyName}";

                    typeConfiguration.rarity = enemyConfig.Bind(tablename, "Rarity", enemySpawnRarities.GetValueOrDefault(enemyType, 0), "Rarity of an enemy from 0 to 100, relative to the total rarity of all enemies combined.");
                    typeConfiguration.maxEnemyCount = enemyConfig.Bind(tablename, "MaxEnemyCount", enemyType.MaxCount, "Maximum amount of this type of enemy allowed at once.");
                    typeConfiguration.powerLevel = enemyConfig.Bind(tablename, "PowerLevel", enemyType.PowerLevel, "How much a single enemy of this type contributes to the maximum power level.");
                    typeConfiguration.spawnCurve = enemyConfig.Bind(tablename, "SpawnChanceCurve", enemyType.probabilityCurve, "How  likely this enemy is to spawn as the day progresses, from 0 to 1.");
                    typeConfiguration.spawnFalloffCurve = enemyConfig.Bind(tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, "The spawning curve multiplier of how less/more likely this enemy is to spawn based on how many existing enemies with the same type.");
                    typeConfiguration.useSpawnFalloff = enemyConfig.Bind(tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, "Whether or not to modify spawn rates based on how many existing enemies of the same type there are.");

                    enemies.enemyTypes.Add(typeConfiguration);
                }
            }

            // Process daytime enemies
            {
                ConfigFile enemyConfig = new ConfigFile(Path.Combine(saveDirectory, DAYTIME_ENEMY_CFG_NAME), true);
                daytimeEnemies.enabled = enemyConfig.Bind("General", "Enabled", false, "Enables/disables custom daytime enemy spawn rate modification. Typically enemies like manticoils, locusts, etc.");
                daytimeEnemies.maxPowerCount = enemyConfig.Bind("General", "MaxPowerCount", level.maxDaytimeEnemyPowerCount, "Maximum total power level allowed for daytime enemies.");
                daytimeEnemies.spawnAmountCurve = enemyConfig.Bind("General", "SpawnAmountCurve", level.daytimeEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses, from 0 to 1.");
                daytimeEnemies.spawnAmountRange = enemyConfig.Bind("General", "SpawnAmountRange", level.daytimeEnemiesProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.DaytimeEnemies);
                foreach (EnemyType enemyType in enemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.enemyName}";

                    typeConfiguration.rarity = enemyConfig.Bind(tablename, "Rarity", enemySpawnRarities.GetValueOrDefault(enemyType, 0), "Rarity of an enemy from 0 to 100, relative to the total rarity of all enemies combined.");
                    typeConfiguration.maxEnemyCount = enemyConfig.Bind(tablename, "MaxEnemyCount", enemyType.MaxCount, "Maximum amount of this type of enemy allowed at once.");
                    typeConfiguration.powerLevel = enemyConfig.Bind(tablename, "PowerLevel", enemyType.PowerLevel, "How much a single enemy of this type contributes to the maximum power level.");
                    typeConfiguration.spawnCurve = enemyConfig.Bind(tablename, "SpawnChanceCurve", enemyType.probabilityCurve, "How  likely this enemy is to spawn as the day progresses, from 0 to 1.");
                    typeConfiguration.spawnFalloffCurve = enemyConfig.Bind(tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, "The spawning curve multiplier of how less/more likely this enemy is to spawn based on how many existing enemies with the same type.");
                    typeConfiguration.useSpawnFalloff = enemyConfig.Bind(tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, "Whether or not to modify spawn rates based on how many existing enemies of the same type there are.");

                    daytimeEnemies.enemyTypes.Add(typeConfiguration);
                }
            }

            // Process outside enemies
            {
                ConfigFile enemyConfig = new ConfigFile(Path.Combine(saveDirectory, OUTSIDE_ENEMY_CFG_NAME), true);
                outsideEnemies.enabled = enemyConfig.Bind("General", "Enabled", false, "Enables/disables custom outside enemy spawn rate modification. Typically eyeless dogs, forest giants, etc.");
                outsideEnemies.maxPowerCount = enemyConfig.Bind("General", "MaxPowerCount", level.maxOutsideEnemyPowerCount, "Maximum total power level allowed for outside enemies.");
                outsideEnemies.spawnAmountCurve = enemyConfig.Bind("General", "SpawnAmountCurve", level.outsideEnemySpawnChanceThroughDay, "How many enemies can spawn enemy as the day progresses, from 0 to 1.");
                outsideEnemies.spawnAmountRange = enemyConfig.Bind("General", "SpawnAmountRange", level.spawnProbabilityRange, "How many more/less enemies can spawn. A spawn range of 3 means there can be -/+3 enemies.");

                Dictionary<EnemyType, int> enemySpawnRarities = convertToDictionary(level.OutsideEnemies);
                foreach (EnemyType enemyType in enemyTypes)
                {
                    EnemyTypeConfiguration typeConfiguration = new EnemyTypeConfiguration(enemyType);
                    string tablename = $"EnemyTypes.{enemyType.enemyName}";

                    typeConfiguration.rarity = enemyConfig.Bind(tablename, "Rarity", enemySpawnRarities.GetValueOrDefault(enemyType, 0), "Rarity of an enemy from 0 to 100, relative to the total rarity of all enemies combined.");
                    typeConfiguration.maxEnemyCount = enemyConfig.Bind(tablename, "MaxEnemyCount", enemyType.MaxCount, "Maximum amount of this type of enemy allowed at once.");
                    typeConfiguration.powerLevel = enemyConfig.Bind(tablename, "PowerLevel", enemyType.PowerLevel, "How much a single enemy of this type contributes to the maximum power level.");
                    typeConfiguration.spawnCurve = enemyConfig.Bind(tablename, "SpawnChanceCurve", enemyType.probabilityCurve, "How likely this enemy is to spawn as the day progresses, from 0 to 1.");
                    typeConfiguration.spawnFalloffCurve = enemyConfig.Bind(tablename, "SpawnFalloffCurve", enemyType.numberSpawnedFalloff, "The spawning curve multiplier of how less/more likely this enemy is to spawn based on how many existing enemies with the same type.");
                    typeConfiguration.useSpawnFalloff = enemyConfig.Bind(tablename, "UseSpawnFalloff", enemyType.useNumberSpawnedFalloff, "Whether or not to modify spawn rates based on how many existing enemies of the same type there are.");

                    outsideEnemies.enemyTypes.Add(typeConfiguration);
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
    }
}

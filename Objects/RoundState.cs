using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalQuantities.Objects
{
    public class RoundState : MonoBehaviour
    {
        public Plugin plugin { get; internal set; }
        public Scene scene { get; internal set; }
        public SelectableLevel level { get; internal set; }
        public GlobalConfiguration globalConfiguration { get; set; }
        public LevelConfiguration levelConfiguration { get; set; }

        public List<SpawnableEnemyWithRarity> enemies { get; } = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableEnemyWithRarity> daytimeEnemies { get; } = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableEnemyWithRarity> outsideEnemies { get; } = new List<SpawnableEnemyWithRarity>();

        private List<SpawnableEnemyWithRarity> oldEnemies = new List<SpawnableEnemyWithRarity>();
        private List<SpawnableEnemyWithRarity> oldDaytimeEnemies = new List<SpawnableEnemyWithRarity>();
        private List<SpawnableEnemyWithRarity> oldOutsideEnemies = new List<SpawnableEnemyWithRarity>();

        public HashSet<GameObject> modifiedEnemyTypes { get; } = new HashSet<GameObject>();

        public float defaultScrapAmountMultiplier = 1f;
        public float defaultScrapValueMultiplier = .4f;
        public float defaultMapSizeMultiplier = 1f;
        public List<IntWithRarity> defaultFlowTypes = new List<IntWithRarity>();
        public List<SpawnableItemWithRarity> defaultItems = new List<SpawnableItemWithRarity>();
        public Dictionary<Item, ItemInformation> defaultItemInformation = new Dictionary<Item, ItemInformation>();

        public List<SpawnableMapObject> defaultSpawnableMapObjects = new List<SpawnableMapObject>();

        public void setData(Scene scene, GlobalConfiguration config)
        {
            this.scene = scene;
            globalConfiguration = config;
        }

        public void initialize(SelectableLevel level)
        {
            this.level = level;
            levelConfiguration = globalConfiguration.levelConfigs[level.name];

            Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable enemy options");

            oldEnemies.AddRange(level.Enemies);
            if (levelConfiguration.enemies.enabled.Value)
            {
                populate(level.Enemies, enemies, levelConfiguration.enemies.enemyTypes);
            }
            else if (globalConfiguration.enemyConfiguration.enabled.Value && !globalConfiguration.enemyConfiguration.isDefault())
            {
                populateGlobal(level.Enemies,enemies, globalConfiguration.enemyConfiguration);
            }

            oldDaytimeEnemies.AddRange(level.DaytimeEnemies);
            if (levelConfiguration.daytimeEnemies.enabled.Value)
            {
                populate(level.DaytimeEnemies, daytimeEnemies, levelConfiguration.daytimeEnemies.enemyTypes, true, true);
            }
            else if (globalConfiguration.daytimeEnemyConfiguration.enabled.Value && !globalConfiguration.daytimeEnemyConfiguration.isDefault())
            {
                populateGlobal(level.DaytimeEnemies, daytimeEnemies, globalConfiguration.daytimeEnemyConfiguration, true, true);
            }

            oldOutsideEnemies.AddRange(level.OutsideEnemies);
            if (levelConfiguration.outsideEnemies.enabled.Value)
            {
                populate(level.OutsideEnemies, outsideEnemies, levelConfiguration.outsideEnemies.enemyTypes, true);
            }
            else if (globalConfiguration.outsideEnemyConfiguration.enabled.Value && !globalConfiguration.outsideEnemyConfiguration.isDefault())
            {
                populateGlobal(level.OutsideEnemies, outsideEnemies, globalConfiguration.outsideEnemyConfiguration, true);
            }

            Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable item options");
            defaultItems.AddRange(level.spawnableScrap);
            if (levelConfiguration.scrap.enabled.Value)
            {
                defaultScrapAmountMultiplier = RoundManager.Instance.scrapAmountMultiplier;
                defaultScrapValueMultiplier = RoundManager.Instance.scrapValueMultiplier;
                copyDefaultItems(defaultItemInformation, levelConfiguration.scrap.scrapRarities);
            }
            else if (globalConfiguration.scrapConfiguration.enabled.Value)
            {
                if (!globalConfiguration.scrapConfiguration.scrapAmountMultiplier.isDefault())
                {
                    defaultScrapAmountMultiplier = RoundManager.Instance.scrapAmountMultiplier;
                }
                if (!globalConfiguration.scrapConfiguration.scrapValueMultiplier.isDefault())
                {
                    defaultScrapValueMultiplier = RoundManager.Instance.scrapValueMultiplier;
                }
                foreach (GlobalItemConfiguration config in globalConfiguration.scrapConfiguration.itemConfigurations.Values)
                {
                    if (!config.isDefault())
                    {
                        defaultItemInformation.Add(config.item, new ItemInformation(config.item.minValue, config.item.maxValue, config.item.isConductiveMetal));
                    }
                }
            }

            defaultFlowTypes.AddRange(level.dungeonFlowTypes);
            if (levelConfiguration.dungeon.enabled.Value)
            {
                defaultMapSizeMultiplier = RoundManager.Instance.mapSizeMultiplier;
            }
            else if(globalConfiguration.dungeonConfiguration.enabled.Value && !globalConfiguration.dungeonConfiguration.isDefault())
            {
                defaultMapSizeMultiplier = RoundManager.Instance.mapSizeMultiplier;
            }

            defaultSpawnableMapObjects.AddRange(level.spawnableMapObjects);
        }

        public void OnDestroy()
        {
            foreach (GameObject obj in modifiedEnemyTypes)
            {
                Destroy(obj);
            }
            modifiedEnemyTypes.Clear();

            if ((levelConfiguration.scrap.enabled.Value && !levelConfiguration.scrap.scrapAmountMultiplier.isDefault()) || (globalConfiguration.dungeonConfiguration.enabled.Value && !globalConfiguration.scrapConfiguration.scrapAmountMultiplier.isDefault()))
            {
                RoundManager.Instance.scrapAmountMultiplier = defaultScrapAmountMultiplier;
            }
            if ((levelConfiguration.scrap.enabled.Value && !levelConfiguration.scrap.scrapValueMultiplier.isDefault()) || (globalConfiguration.dungeonConfiguration.enabled.Value && !globalConfiguration.scrapConfiguration.scrapValueMultiplier.isDefault()))
            {
                RoundManager.Instance.scrapValueMultiplier = defaultScrapValueMultiplier;
            }
            if ((levelConfiguration.dungeon.enabled.Value && !levelConfiguration.dungeon.mapSizeMultiplier.isDefault()) || (globalConfiguration.dungeonConfiguration.enabled.Value && !globalConfiguration.dungeonConfiguration.mapSizeMultiplier.isDefault()))
            {
                RoundManager.Instance.mapSizeMultiplier = defaultMapSizeMultiplier;
            }

            foreach (var item in defaultItemInformation)
            {
                item.Key.maxValue = item.Value.maxValue;
                item.Key.minValue = item.Value.minValue;
                item.Key.isConductiveMetal = item.Value.conductive;
            }

            level.Enemies.Clear();
            level.Enemies.AddRange(oldEnemies);
            level.DaytimeEnemies.Clear();
            level.DaytimeEnemies.AddRange(oldDaytimeEnemies);
            level.OutsideEnemies.Clear();
            level.OutsideEnemies.AddRange(oldOutsideEnemies);
            level.dungeonFlowTypes = defaultFlowTypes.ToArray();
            level.spawnableScrap.Clear();
            level.spawnableScrap.AddRange(defaultItems);
            level.spawnableMapObjects = defaultSpawnableMapObjects.ToArray();
        }

        private void populate<T>(List<SpawnableEnemyWithRarity> originals, List<SpawnableEnemyWithRarity> enemiesList, List<T> configs, bool isOutside = false, bool isDaytimeEnemy = false) where T : DaytimeEnemyTypeConfiguration
        {
            Dictionary<EnemyType, int> defaultRarities = new Dictionary<EnemyType, int>();
            foreach (SpawnableEnemyWithRarity enemy in originals)
            {
                defaultRarities.Add(enemy.enemyType, enemy.rarity);
            }

            foreach (var item in configs)
            {
                int rarity = defaultRarities.GetValueOrDefault(item.type, 0);
                item.rarity.Set(ref rarity);
                int maxEnemyCount = item.type.MaxCount;
                item.maxEnemyCount.Set(ref maxEnemyCount);
                if (rarity > 0 && maxEnemyCount > 0)
                {
                    EnemyType type = Instantiate(item.type);
                    item.maxEnemyCount.Set(ref type.MaxCount);
                    item.powerLevel.Set(ref type.PowerLevel);
                    item.spawnCurve.Set(ref type.probabilityCurve);
                    item.stunTimeMultiplier.Set(ref type.stunTimeMultiplier);
                    item.doorSpeedMultiplier.Set(ref type.doorSpeedMultiplier);
                    item.stunGameDifficultyMultiplier.Set(ref type.stunGameDifficultyMultiplier);
                    item.stunnable.Set(ref type.canBeStunned);
                    item.killable.Set(ref type.canDie);
                    if (item is EnemyTypeConfiguration)
                    {
                        EnemyTypeConfiguration normalType = item as EnemyTypeConfiguration;
                        normalType.spawnFalloffCurve.Set(ref type.numberSpawnedFalloff);
                        normalType.useSpawnFalloff.Set(ref type.useNumberSpawnedFalloff);
                    }
                    type.isOutsideEnemy = isOutside;
                    type.isDaytimeEnemy = isDaytimeEnemy;

                    EnemyAI ai = instantiateEnemyTypeObject(type);
                    if (ai != null)
                    {
                        item.enemyHp.Set(ref ai.enemyHP);
                    }

                    SpawnableEnemyWithRarity spawnable = new SpawnableEnemyWithRarity();
                    spawnable.enemyType = type;
                    spawnable.rarity = rarity;
                    enemiesList.Add(spawnable);
                }
            }
        }

        private void populateGlobal<T>(List<SpawnableEnemyWithRarity> originals, List<SpawnableEnemyWithRarity> enemiesList, GlobalOutsideEnemyConfiguration<T> configuration, bool isOutside = false, bool isDaytime = false) where T: GlobalDaytimeEnemyTypeConfiguration
        {
            List<SpawnableEnemyWithRarity> spawnableEnemies = new List<SpawnableEnemyWithRarity>();
            Dictionary<EnemyType, int> defaultRarities = new Dictionary<EnemyType, int>();
            foreach (SpawnableEnemyWithRarity enemy in originals)
            {
                defaultRarities.Add(enemy.enemyType, enemy.rarity);
            }

            foreach (var item in configuration.enemyTypeConfigurations)
            {
                EnemyType enemyType = item.Key;
                GlobalDaytimeEnemyTypeConfiguration typeConfig = item.Value;
                int rarity = defaultRarities.GetValueOrDefault(enemyType, 0);
                typeConfig.rarity.Set(ref rarity);
                int maxCount = enemyType.MaxCount;
                typeConfig.maxEnemyCount.Set(ref maxCount);
                if (rarity > 0 && maxCount > 0)
                {
                    if (!typeConfig.isEnemyTypeDefault())
                    {
                        EnemyType type = Instantiate(enemyType);
                        type.MaxCount = maxCount;
                        typeConfig.powerLevel.Set(ref type.PowerLevel);
                        typeConfig.spawnCurve.Set(ref type.probabilityCurve);
                        typeConfig.stunTimeMultiplier.Set(ref type.stunTimeMultiplier);
                        typeConfig.doorSpeedMultiplier.Set(ref type.doorSpeedMultiplier);
                        typeConfig.stunGameDifficultyMultiplier.Set(ref type.stunGameDifficultyMultiplier);
                        typeConfig.stunnable.Set(ref type.canBeStunned);
                        typeConfig.killable.Set(ref type.canDie);
                        if (typeConfig is GlobalEnemyTypeConfiguration)
                        {
                            GlobalEnemyTypeConfiguration enemyConfig = typeConfig as GlobalEnemyTypeConfiguration;
                            enemyConfig.spawnFalloffCurve.Set(ref type.numberSpawnedFalloff);
                            enemyConfig.useSpawnFalloff.Set(ref type.useNumberSpawnedFalloff);
                        }

                        type.isOutsideEnemy = isOutside;
                        type.isDaytimeEnemy = isDaytime;

                        EnemyAI ai = instantiateEnemyTypeObject(type);
                        if (ai != null)
                        {
                            typeConfig.enemyHp.Set(ref ai.enemyHP);
                        }

                        SpawnableEnemyWithRarity spawnable = new SpawnableEnemyWithRarity();
                        spawnable.enemyType = type;
                        spawnable.rarity = rarity;
                        spawnableEnemies.Add(spawnable);
                    }
                    else
                    {
                        // defaultRarities should always contain an enemyType at this point
                        SpawnableEnemyWithRarity spawnable = new SpawnableEnemyWithRarity();
                        spawnable.enemyType = enemyType;
                        spawnable.rarity = rarity;
                        spawnableEnemies.Add(spawnable);
                    }
                }
            }
            enemiesList.Clear();
            enemiesList.AddRange(spawnableEnemies);
        }

        private EnemyAI instantiateEnemyTypeObject(EnemyType type)
        {
            GameObject obj = type.enemyPrefab;
            if (obj == null)
            {
                Plugin.LETHAL_LOGGER.LogError($"No enemy prefab found for enemy {type.name}!");
                return null;
            }
            bool isActive = obj.activeSelf;
            obj.SetActive(false);

            GameObject copy = Instantiate(obj, new Vector3(0, -50, 0), Quaternion.identity);
            copy.name = obj.name;
            modifiedEnemyTypes.Add(copy);
            SceneManager.MoveGameObjectToScene(copy, SceneManager.GetSceneByName("SampleSceneRelay"));
            type.enemyPrefab = copy;
            EnemyAI ai = copy.GetComponent<EnemyAI>();
            ai.enemyType = type;
            obj.SetActive(isActive);
            copy.hideFlags = HideFlags.HideAndDontSave;

            DontDestroyOnLoad(copy);

            return ai;
        }

        private void copyDefaultItems(Dictionary<Item, ItemInformation> items, List<ItemConfiguration> scrapRarities)
        {
            foreach (ItemConfiguration itemconfig in scrapRarities)
            {
                items.Add(itemconfig.item, new ItemInformation(itemconfig.item.minValue, itemconfig.item.maxValue, itemconfig.item.isConductiveMetal));
            }
        }
    }
}

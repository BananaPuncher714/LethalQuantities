using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalQuantities.Objects
{
    internal class EnemySpawnCategoryAttribute: Attribute
    {
        public bool daytimeEnemy { get; private set; }
        public bool outsideEnemy { get; private set; }

        internal EnemySpawnCategoryAttribute(bool daytime, bool outside = false) {
            daytimeEnemy = daytime;
            outsideEnemy = daytime ? true : outside;
        }
    }

    public static class EnemySpawnCategoryExtension
    {
        public static bool isDaytime(this EnemySpawnCategory category)
        {
            return getAttribute(category).daytimeEnemy;
        }

        public static bool isOutside(this EnemySpawnCategory category)
        {
            return getAttribute(category).outsideEnemy;
        }

        private static EnemySpawnCategoryAttribute getAttribute(EnemySpawnCategory category)
        {
            return (EnemySpawnCategoryAttribute) Attribute.GetCustomAttribute(typeof(EnemySpawnCategory).GetField(Enum.GetName(typeof(EnemySpawnCategory), category)), typeof(EnemySpawnCategoryAttribute));
        }
    }

    public enum EnemySpawnCategory
    {
        [EnemySpawnCategoryAttribute(false)] INSIDE,
        [EnemySpawnCategoryAttribute(false, true)] OUTSIDE,
        [EnemySpawnCategoryAttribute(true)] DAYTIME
    }

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
        public HashSet<GameObject> modifiedEnemyTypes { get; } = new HashSet<GameObject>();

        private List<SpawnableEnemyWithRarity> oldEnemies = new List<SpawnableEnemyWithRarity>();
        private List<SpawnableEnemyWithRarity> oldDaytimeEnemies = new List<SpawnableEnemyWithRarity>();
        private List<SpawnableEnemyWithRarity> oldOutsideEnemies = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableItemWithRarity> defaultItems = new List<SpawnableItemWithRarity>();


        public float defaultScrapAmountMultiplier = 1f;
        public float defaultScrapValueMultiplier = .4f;
        public float defaultMapSizeMultiplier = 1f;
        public List<IntWithRarity> defaultFlowTypes = new List<IntWithRarity>();
        public List<SpawnableMapObject> defaultSpawnableMapObjects = new List<SpawnableMapObject>();
        public Dictionary<Item, ItemInformation> defaultItemInformation = new Dictionary<Item, ItemInformation>();

        public void setData(Scene scene, GlobalConfiguration config)
        {
            this.scene = scene;
            globalConfiguration = config;
        }

        public bool getValidEnemyConfiguration(out EnemyConfiguration<EnemyTypeConfiguration> configuration)
        {
            configuration = (levelConfiguration.enemies as IValidatableConfiguration).isValid()
                    ? levelConfiguration.enemies : ((globalConfiguration.enemyConfiguration as IValidatableConfiguration).isValid()
                            ? globalConfiguration.enemyConfiguration : null);
            return configuration != null;
        }

        public bool getValidDaytimeEnemyConfiguration(out EnemyConfiguration<DaytimeEnemyTypeConfiguration> configuration)
        {
            configuration = (levelConfiguration.daytimeEnemies as IValidatableConfiguration).isValid()
                    ? levelConfiguration.daytimeEnemies : ((globalConfiguration.daytimeEnemyConfiguration as IValidatableConfiguration).isValid()
                            ? globalConfiguration.daytimeEnemyConfiguration : null);
            return configuration != null;
        }

        public bool getValidOutsideEnemyConfiguration(out OutsideEnemyConfiguration<EnemyTypeConfiguration> configuration)
        {
            configuration = (levelConfiguration.outsideEnemies as IValidatableConfiguration).isValid()
                    ? levelConfiguration.outsideEnemies : ((globalConfiguration.outsideEnemyConfiguration as IValidatableConfiguration).isValid()
                            ? globalConfiguration.outsideEnemyConfiguration : null);
            return configuration != null;
        }

        public bool getValidScrapConfiguration(out ScrapConfiguration configuration)
        {
            configuration = (levelConfiguration.scrap as IValidatableConfiguration).isValid()
                    ? levelConfiguration.scrap : ((globalConfiguration.scrapConfiguration as IValidatableConfiguration).isValid()
                            ? globalConfiguration.scrapConfiguration : null);
            return configuration != null;
        }

        public bool getValidDungeonGenerationConfiguration(out DungeonGenerationConfiguration configuration)
        {
            configuration = (levelConfiguration.dungeon as IValidatableConfiguration).isValid()
                    ? levelConfiguration.dungeon : ((globalConfiguration.dungeonConfiguration as IValidatableConfiguration).isValid()
                            ? globalConfiguration.dungeonConfiguration : null);
            return configuration != null;
        }

       public bool getValidTrapConfiguration(out TrapConfiguration configuration)
        {
            configuration = (levelConfiguration.trap as IValidatableConfiguration).isValid()
                    ? levelConfiguration.trap : ((globalConfiguration.trapConfiguration as IValidatableConfiguration).isValid()
                            ? globalConfiguration.trapConfiguration : null);
            return configuration != null;
        }

        public void initialize(SelectableLevel level)
        {
            this.level = level;
            levelConfiguration = globalConfiguration.levelConfigs[level.name];

            Plugin.LETHAL_LOGGER.LogInfo("Preparing for level modification");

            oldEnemies.AddRange(level.Enemies);
            oldDaytimeEnemies.AddRange(level.DaytimeEnemies);
            oldOutsideEnemies.AddRange(level.OutsideEnemies);
            defaultItems.AddRange(level.spawnableScrap);
            defaultScrapAmountMultiplier = RoundManager.Instance.scrapAmountMultiplier;
            defaultScrapValueMultiplier = RoundManager.Instance.scrapValueMultiplier;
            defaultMapSizeMultiplier = RoundManager.Instance.mapSizeMultiplier;
            defaultFlowTypes = level.dungeonFlowTypes.ToList();
            defaultSpawnableMapObjects = level.spawnableMapObjects.ToList();

            {
                if (getValidEnemyConfiguration(out EnemyConfiguration<EnemyTypeConfiguration> config)) {
                    Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable enemy rarities");
                    populate(level.Enemies, enemies, config.enemyTypes.Values, EnemySpawnCategory.INSIDE);
                }
            }

            {
                if (getValidDaytimeEnemyConfiguration(out EnemyConfiguration<DaytimeEnemyTypeConfiguration> config)) {
                    Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable daytime enemy rarities");
                    populate(level.DaytimeEnemies, daytimeEnemies, config.enemyTypes.Values, EnemySpawnCategory.DAYTIME
                        );
                }
            }

            {
                if (getValidOutsideEnemyConfiguration(out OutsideEnemyConfiguration<EnemyTypeConfiguration> config)) {
                    Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable outside enemy rarities");
                    populate(level.OutsideEnemies, outsideEnemies, config.enemyTypes.Values, EnemySpawnCategory.OUTSIDE);
                }
            }

            {
                if (getValidScrapConfiguration(out ScrapConfiguration config)) {
                    Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable item rarities");
                    copyDefaultItems(defaultItemInformation, config.items.Values);
                }
            }

            defaultSpawnableMapObjects.AddRange(level.spawnableMapObjects);
        }

        public void OnDestroy()
        {
            Plugin.LETHAL_LOGGER.LogInfo($"Cleaning up and restoring values to {level.name}");
            // Undo any modifications made to the SelectedLevel
            foreach (GameObject obj in modifiedEnemyTypes)
            {
                Destroy(obj);
            }
            modifiedEnemyTypes.Clear();

            RoundManager.Instance.scrapAmountMultiplier = defaultScrapAmountMultiplier;
            RoundManager.Instance.scrapValueMultiplier = defaultScrapValueMultiplier;
            RoundManager.Instance.mapSizeMultiplier = defaultMapSizeMultiplier;

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
            level.spawnableScrap.Clear();
            level.spawnableScrap.AddRange(defaultItems);
            level.dungeonFlowTypes = defaultFlowTypes.ToArray();
            level.spawnableMapObjects = defaultSpawnableMapObjects.ToArray();
        }

        private void populate<T>(List<SpawnableEnemyWithRarity> originals, List<SpawnableEnemyWithRarity> enemiesList, IEnumerable<T> configs, EnemySpawnCategory category) where T : DaytimeEnemyTypeConfiguration
        {
            Dictionary<EnemyType, int> defaultRarities = new Dictionary<EnemyType, int>();
            foreach (SpawnableEnemyWithRarity enemy in originals)
            {
                defaultRarities.TryAdd(enemy.enemyType, enemy.rarity);
            }

            foreach (var item in configs)
            {
                EnemyType originalType = item.type;
                int rarity = defaultRarities.GetValueOrDefault(originalType, 0);
                item.rarity.Set(ref rarity);
                int maxEnemyCount = item.type.MaxCount;
                item.maxEnemyCount.Set(ref maxEnemyCount);
                if (rarity > 0 && maxEnemyCount > 0)
                {
                    EnemyType type = Instantiate(originalType);
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
                    type.isOutsideEnemy = category.isOutside();
                    type.isDaytimeEnemy = category.isDaytime();

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

        private void copyDefaultItems(Dictionary<Item, ItemInformation> items, IEnumerable<ItemConfiguration> scrapRarities)
        {
            foreach (ItemConfiguration itemconfig in scrapRarities)
            {
                if (!itemconfig.isDefault())
                {
                    items.TryAdd(itemconfig.item, new ItemInformation(itemconfig.item.minValue, itemconfig.item.maxValue, itemconfig.item.isConductiveMetal));
                }
            }
        }
    }
}

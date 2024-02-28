using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using LethalQuantities.Json;

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
        public LevelPreset preset { get; internal set; }

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

        public void setData(Scene scene, LevelPreset preset)
        {
            this.scene = scene;
            this.preset = preset;
        }

        public void initialize(SelectableLevel level)
        {
            this.level = level;

            MiniLogger.LogInfo("Preparing for level modification");

            oldEnemies.AddRange(level.Enemies);
            oldDaytimeEnemies.AddRange(level.DaytimeEnemies);
            oldOutsideEnemies.AddRange(level.OutsideEnemies);
            defaultItems.AddRange(level.spawnableScrap);
            defaultScrapAmountMultiplier = RoundManager.Instance.scrapAmountMultiplier;
            defaultScrapValueMultiplier = RoundManager.Instance.scrapValueMultiplier;
            defaultMapSizeMultiplier = RoundManager.Instance.mapSizeMultiplier;
            defaultFlowTypes = level.dungeonFlowTypes.ToList();
            defaultSpawnableMapObjects = level.spawnableMapObjects.ToList();


            if (preset.enemies.isSet())
            {
                populate(level.Enemies, enemies, preset.enemies.value, EnemySpawnCategory.INSIDE);
            }

            if (preset.daytimeEnemies.isSet())
            {
                populate(level.DaytimeEnemies, daytimeEnemies, preset.daytimeEnemies.value, EnemySpawnCategory.DAYTIME);
            }

            if (preset.outsideEnemies.isSet())
            {
                populate(level.OutsideEnemies, outsideEnemies, preset.outsideEnemies.value, EnemySpawnCategory.OUTSIDE);
            }

            if (preset.scrap.isSet())
            {
                copyDefaultItems(defaultItemInformation, preset.scrap.value);
            }
        }

        public void OnDestroy()
        {
            MiniLogger.LogInfo($"Cleaning up and restoring values to {level.name}");
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

        private void populate(List<SpawnableEnemyWithRarity> originals, List<SpawnableEnemyWithRarity> enemiesList, Dictionary<EnemyType, LevelPresetEnemyType> types, EnemySpawnCategory category)
        {
            Dictionary<EnemyType, int> defaultRarities = new Dictionary<EnemyType, int>();
            foreach (SpawnableEnemyWithRarity enemy in originals)
            {
                defaultRarities.TryAdd(enemy.enemyType, enemy.rarity);
            }

            foreach (var entry in types)
            {
                EnemyType originalType = entry.Key;
                LevelPresetEnemyType presetInfo = entry.Value;
                int rarity = defaultRarities.GetValueOrDefault(originalType, 0);
                presetInfo.rarity.update(ref rarity);
                int maxEnemyCount = originalType.MaxCount;
                presetInfo.maxEnemyCount.update(ref maxEnemyCount);
                if (rarity > 0 && maxEnemyCount > 0)
                {
                    EnemyType type = Instantiate(originalType);
                    type.MaxCount = maxEnemyCount;
                    presetInfo.maxEnemyCount.update(ref type.MaxCount);
                    presetInfo.powerLevel.update(ref type.PowerLevel);
                    presetInfo.spawnChanceCurve.update(ref type.probabilityCurve);
                    presetInfo.stunTimeMultiplier.update(ref type.stunTimeMultiplier);
                    presetInfo.doorSpeedMultiplier.update(ref type.doorSpeedMultiplier);
                    presetInfo.stunGameDifficultyMultiplier.update(ref type.stunGameDifficultyMultiplier);
                    presetInfo.stunnable.update(ref type.canBeStunned);
                    presetInfo.killable.update(ref type.canDie);
                    presetInfo.spawnFalloffCurve.update(ref type.numberSpawnedFalloff);
                    presetInfo.useSpawnFalloff.update(ref type.useNumberSpawnedFalloff);

                    type.isOutsideEnemy = category.isOutside();
                    type.isDaytimeEnemy = category.isDaytime();

                    EnemyAI ai = instantiateEnemyTypeObject(type);
                    if (ai != null)
                    {
                        presetInfo.enemyHp.update(ref ai.enemyHP);
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
                MiniLogger.LogError($"No enemy prefab found for enemy {type.name}!");
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

        private void copyDefaultItems(Dictionary<Item, ItemInformation> items, Dictionary<Item, LevelPresetItem> scrapRarities)
        {
            foreach (Item item in scrapRarities.Keys)
            {
                items.TryAdd(item, new ItemInformation(item.minValue, item.maxValue, item.isConductiveMetal));
            }
        }
    }
}

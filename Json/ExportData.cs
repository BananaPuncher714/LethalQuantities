using LethalQuantities.Objects;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace LethalQuantities.Json
{
    public class ExportDataEnemyType
    {
        public int max_enemy_count;
        public int power_level;
        public AnimationCurve spawn_chance_curve;
        public float stun_time_multiplier;
        public float door_speed_multiplier;
        public float stun_game_difficulty_multiplier;
        public bool stunnable;
        public bool killable;
        public int enemy_hp;
        public AnimationCurve spawn_falloff_curve;
        public bool use_spawn_falloff;
        public string name;

        public ExportDataEnemyType(EnemyType type, int health)
        {
            max_enemy_count = type.MaxCount;
            power_level = type.PowerLevel;
            spawn_chance_curve = type.probabilityCurve;
            stun_time_multiplier = type.stunTimeMultiplier;
            door_speed_multiplier = type.doorSpeedMultiplier;
            stun_game_difficulty_multiplier = type.stunGameDifficultyMultiplier;
            stunnable = type.canBeStunned;
            killable = type.canDie;
            enemy_hp = health;
            spawn_falloff_curve = type.numberSpawnedFalloff;
            use_spawn_falloff = type.useNumberSpawnedFalloff;
            name = type.getFriendlyName();
        }
    }

    public class ExportDataItem
    {
        public float weight;
        public int min_value;
        public int max_value;
        public bool conductive;
        public bool scrap;
        public string name;

        public ExportDataItem(Item item)
        {
            weight = item.weight;
            min_value = Math.Min(item.minValue, item.maxValue);
            max_value = Math.Max(item.minValue, item.maxValue);
            scrap = item.isScrap;
            conductive = item.isConductiveMetal;
            name = item.itemName;
        }
    }

    public class ExportDataTrap
    {
        public bool spawn_facing_away_from_wall;
        public string name;
        public string description;

        public ExportDataTrap(DirectionalSpawnableMapObject obj)
        {
            this.spawn_facing_away_from_wall = obj.faceAwayFromWall;
            this.name = obj.getName();
            this.description = obj.getDescription();
        }
    }

    public class ExportTypeSelectableLevel
    {
        public int price;
        public string planet_name;
        public float factory_size_multiplier;
        public int max_power_count;
        public int max_daytime_power_count;
        public int max_outside_power_count;
        public AnimationCurve enemy_spawn_curve;
        public AnimationCurve daytime_enemy_spawn_curve;
        public AnimationCurve outside_enemy_spawn_curve;
        public float enemy_spawn_probability_range;
        public float daytime_enemy_spawn_probability_range;
        public int min_scrap;
        public int max_scrap;
        public bool spawn_enemies_and_scrap;
        public Dictionary<string, int> enemies = new Dictionary<string, int>();
        public Dictionary<string, int> daytime_enemies = new Dictionary<string, int>();
        public Dictionary<string, int> outside_enemies = new Dictionary<string, int>();
        public Dictionary<string, int> scrap = new Dictionary<string, int>();
        public Dictionary<string, int> dungeon_flows = new Dictionary<string, int>();
        public Dictionary<string, AnimationCurve> spawnable_map_objects = new Dictionary<string, AnimationCurve>();

        public ExportTypeSelectableLevel(SelectableLevel level, int price)
        {
            this.price = price;
            planet_name = level.PlanetName;
            factory_size_multiplier = level.factorySizeMultiplier;
            max_power_count = level.maxEnemyPowerCount;
            max_outside_power_count = level.maxOutsideEnemyPowerCount;
            max_daytime_power_count = level.maxDaytimeEnemyPowerCount;
            enemy_spawn_curve = level.enemySpawnChanceThroughoutDay;
            daytime_enemy_spawn_curve = level.daytimeEnemySpawnChanceThroughDay;
            outside_enemy_spawn_curve = level.outsideEnemySpawnChanceThroughDay;
            enemy_spawn_probability_range = level.spawnProbabilityRange;
            daytime_enemy_spawn_probability_range = level.daytimeEnemiesProbabilityRange;
            min_scrap = level.minScrap;
            max_scrap = level.maxScrap;
            spawn_enemies_and_scrap = level.spawnEnemiesAndScrap;

            foreach (var item in level.Enemies)
            {
                enemies.TryAdd(item.enemyType.name, item.rarity);
            }
            foreach (var item in level.DaytimeEnemies)
            {
                daytime_enemies.TryAdd(item.enemyType.name, item.rarity);
            }
            foreach (var item in level.OutsideEnemies)
            {
                outside_enemies.TryAdd(item.enemyType.name, item.rarity);
            }
            foreach (var item in level.spawnableScrap)
            {
                scrap.TryAdd(item.spawnableItem.name, item.rarity);
            }
            foreach (var item in level.dungeonFlowTypes)
            {
                dungeon_flows.TryAdd(RoundManager.Instance.dungeonFlowTypes[item.id].name, item.rarity);
            }
            foreach (var item in level.spawnableMapObjects)
            {
                spawnable_map_objects.TryAdd(item.prefabToSpawn.name, item.numberToSpawn);
            }
        }
    }

    public class ExportData
    {
        public float scrap_value_multiplier;
        public float scrap_amount_multiplier;
        public float map_size_multiplier;

        public Dictionary<string, ExportDataEnemyType> enemies = new Dictionary<string, ExportDataEnemyType>();
        public Dictionary<string, ExportDataItem> items = new Dictionary<string, ExportDataItem>();
        public Dictionary<string, ExportDataTrap> traps = new Dictionary<string, ExportDataTrap>();
        public Dictionary<string, ExportTypeSelectableLevel> levels = new Dictionary<string, ExportTypeSelectableLevel>();
        public List<string> dungeon_flows = new List<string>();

        public ExportData(RoundManager manager, GlobalInformation info)
        {
            scrap_value_multiplier = manager.scrapValueMultiplier;
            scrap_amount_multiplier = manager.scrapAmountMultiplier;
            map_size_multiplier = manager.mapSizeMultiplier;

            foreach (EnemyType type in info.allEnemyTypes)
            {
                int health = type.enemyPrefab.GetComponent<EnemyAI>().enemyHP;
                enemies.Add(type.name, new ExportDataEnemyType(type, health));
            }

            foreach (Item type in info.allItems)
            {
                items.Add(type.name, new ExportDataItem(type));
            }

            foreach (DirectionalSpawnableMapObject type in info.allSpawnableMapObjects)
            {
                traps.Add(type.obj.name, new ExportDataTrap(type));
            }

            foreach (var item in info.allSelectableLevels)
            {
                levels.Add(item.Key.getOriginalLevelName(), new ExportTypeSelectableLevel(item.Key, item.Value.price));
            }

            foreach (var flow in info.allDungeonFlows)
            {
                dungeon_flows.Add(flow.name);
            }
        }

        public bool updatePrice(SelectableLevel level, int price)
        {
            ExportTypeSelectableLevel data = levels[level.getOriginalLevelName()];
            if (data.price == -1)
            {
                data.price = price;
                return true;
            }
            return false;
        }
    }
}

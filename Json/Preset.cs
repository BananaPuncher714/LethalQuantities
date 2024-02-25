using LethalQuantities.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;
using Object = System.Object;

namespace LethalQuantities.Json
{
    // Quick easy to edit hardcoded ad-hoc json solution for now

    public interface IOptional
    {
        public bool isSet();
        public Object getValue();
    }

    public class Optional<T> : IOptional
    {
        public T value;
        public bool set;

        public Optional()
        {
            set = false;
        }

        public Optional(T value)
        {
            this.value = value;
            set = true;
        }

        public Optional(T value, bool set)
        {
            this.value = value;
            this.set = set;
        }

        public void setValue(T val)
        {
            this.value = val;
            set = val != null;
        }

        public Optional(CustomEntry<T> entry)
        {
            value = entry.Value(value);
            set = entry.isLocallySet();
        }

        public bool update(ref T variable)
        {
            if (set)
            {
                variable = value;
            }
            return set;
        }

        public static Optional<T> Empty()
        {
            return new Optional<T>();
        }

        public bool isSet()
        {
            return set;
        }

        public Object getValue()
        {
            return value;
        }
    }

    public interface Identifiable
    {
        public string id { get; set; }
    }

    public class ItemOptions : Identifiable
    {
        public string name;
        public string id { get; set; }
        public bool scrap;
        public Optional<bool> conductive = new Optional<bool>();
        public Optional<int> minValue = new Optional<int>();
        public Optional<int> maxValue = new Optional<int>();
        public Optional<float> weight = new Optional<float>();
        public Optional<int> rarity = new Optional<int>();

        public ItemOptions()
        {
        }

        public ItemOptions(ItemConfiguration configuration)
        {
            scrap = configuration is IScrappableConfiguration;
            conductive = new Optional<bool>(configuration.conductive);
            rarity = new Optional<int>(configuration.rarity);

            if (scrap)
            {
                IScrappableConfiguration config = configuration as IScrappableConfiguration;
                minValue = new Optional<int>(config.minValue);
                maxValue = new Optional<int>(config.maxValue);
            }

            if (configuration is IWeightConfigurable)
            {
                IWeightConfigurable config = configuration as IWeightConfigurable;
                weight = new Optional<float>(config.weight);

                // Convert it to pounds
                weight.value = (weight.value - 1) * 100; ;
            }
        }
    }

    public class EnemyTypeOptions : Identifiable
    {
        public string id { get; set; } = "";
        public string name = "";
        public Optional<float> doorSpeedMultiplier = new Optional<float>();
        public Optional<int> enemyHp = new Optional<int>();
        public Optional<int> rarity = new Optional<int>();
        public Optional<bool> killable = new Optional<bool>();
        public Optional<int> maxEnemyCount = new Optional<int>();
        public Optional<int> powerLevel = new Optional<int>();
        public Optional<AnimationCurve> spawnChanceCurve = new Optional<AnimationCurve>();
        public Optional<AnimationCurve> spawnFalloffCurve = new Optional<AnimationCurve>();
        public Optional<float> stunGameDifficultyMultiplier = new Optional<float>();
        public Optional<float> stunTimeMultiplier = new Optional<float>();
        public Optional<bool> stunnable = new Optional<bool>();
        public Optional<bool> useSpawnFalloff = new Optional<bool>();

        public EnemyTypeOptions()
        {
        }

        public EnemyTypeOptions(DaytimeEnemyTypeConfiguration configuration)
        {
            id = configuration.type.name;
            name = configuration.type.getFriendlyName();
            doorSpeedMultiplier = new Optional<float>(configuration.doorSpeedMultiplier);
            enemyHp = new Optional<int>(configuration.enemyHp);
            rarity = new Optional<int>(configuration.rarity);
            killable = new Optional<bool>(configuration.killable);
            maxEnemyCount = new Optional<int>(configuration.maxEnemyCount);
            powerLevel = new Optional<int>(configuration.powerLevel);
            spawnChanceCurve = new Optional<AnimationCurve>(configuration.spawnCurve);
            stunGameDifficultyMultiplier = new Optional<float>(configuration.stunGameDifficultyMultiplier);
            stunTimeMultiplier = new Optional<float>(configuration.stunTimeMultiplier);
            stunnable = new Optional<bool>(configuration.stunnable);
            if (configuration is EnemyTypeConfiguration)
            {
                EnemyTypeConfiguration config = configuration as EnemyTypeConfiguration;
                useSpawnFalloff = new Optional<bool>(config.useSpawnFalloff);
                // Multiply key by 10
                spawnFalloffCurve = new Optional<AnimationCurve>(config.spawnFalloffCurve);
                spawnFalloffCurve.value = new AnimationCurve(spawnFalloffCurve.value.keys.Select(key => new Keyframe(key.time * 10, key.value)).ToArray());
            }
        }
    }

    public class DungeonFlowOptions : Identifiable
    {
        public string id { get; set; } = "";
        public string name = "";
        public Optional<float> factorySizeMultiplier = new Optional<float>();
        public Optional<int> rarity = new Optional<int>();

        public DungeonFlowOptions()
        {
        }

        public DungeonFlowOptions(DungeonFlowConfiguration configuration)
        {
            factorySizeMultiplier = new Optional<float>(configuration.factorySizeMultiplier);
            rarity = new Optional<int>(configuration.rarity);
        }
    }

    public class TrapOptions : Identifiable
    {
        public string id { get; set; } = "";
        public string name = "";
        public string description = "";
        public Optional<AnimationCurve> spawnCurve = new Optional<AnimationCurve>();
        public bool spawnFacingAwayFromWall = false;

        public TrapOptions()
        {
        }

        public TrapOptions(SpawnableMapObjectConfiguration config)
        {
            name = config.spawnableObject.getName();
            description = config.spawnableObject.getDescription();
            spawnCurve = new Optional<AnimationCurve>(config.numberToSpawn);
            spawnFacingAwayFromWall = config.spawnableObject.faceAwayFromWall;
        }
    }

    public class PriceOptions : Identifiable
    {
        public string id { get; set; } = "";
        public string name = "";
        public Optional<int> price = new Optional<int>();

        public PriceOptions()
        {
        }

        public PriceOptions(MoonPriceConfiguration config)
        {
            price = new Optional<int>(config.price);
        }
    }

    public class Preset : Identifiable
    {
        public string name = "";
        public string id { get; set; } = "";
        public string parent = "";
        public Optional<List<EnemyTypeOptions>> daytimeEnemies = new Optional<List<EnemyTypeOptions>>();
        public Optional<AnimationCurve> daytimeSpawnCurve = new Optional<AnimationCurve>();
        public Optional<float> daytimeSpawnProbabilityRange = new Optional<float>();
        public Optional<List<DungeonFlowOptions>> dungeonFlows = new Optional<List<DungeonFlowOptions>>();
        public Optional<List<EnemyTypeOptions>> enemies = new Optional<List<EnemyTypeOptions>>();
        public Optional<float> mapSizeMultiplier = new Optional<float>();
        public Optional<int> maxDaytimePowerCount = new Optional<int>();
        public Optional<int> maxOutsidePowerCount = new Optional<int>();
        public Optional<int> maxPowerCount = new Optional<int>();
        public Optional<int> maxScrap = new Optional<int>();
        public Optional<int> minScrap = new Optional<int>();
        public Optional<List<EnemyTypeOptions>> outsideEnemies = new Optional<List<EnemyTypeOptions>>();
        public Optional<AnimationCurve> outsideSpawnCurve = new Optional<AnimationCurve>();
        public Optional<List<PriceOptions>> price = new Optional<List<PriceOptions>>();
        public Optional<List<ItemOptions>> scrap = new Optional<List<ItemOptions>>();
        public Optional<float> scrapAmountMultiplier = new Optional<float>();
        public Optional<float> scrapValueMultiplier = new Optional<float>();
        public Optional<AnimationCurve> spawnCurve = new Optional<AnimationCurve>();
        public Optional<float> spawnProbabilityRange = new Optional<float>();
        public Optional<List<TrapOptions>> traps = new Optional<List<TrapOptions>>();

        public Preset()
        {
        }

        public Preset(LevelConfiguration configuration)
        {
            if (configuration.enemies.enabled.Value)
            {
                maxPowerCount = new Optional<int>(configuration.enemies.maxPowerCount);
                spawnCurve = new Optional<AnimationCurve>(configuration.enemies.spawnAmountCurve);
                spawnProbabilityRange = new Optional<float>(configuration.enemies.spawnAmountRange);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.enemies.enemyTypes.Values)
                {
                    if (item.isSet() && item.rarity.Value(0) > 0)
                    {
                        options.Add(new EnemyTypeOptions(item));
                    }
                }
                if (options.Count > 0)
                {
                    enemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            if (configuration.daytimeEnemies.enabled.Value)
            {
                maxDaytimePowerCount = new Optional<int>(configuration.daytimeEnemies.maxPowerCount);
                daytimeSpawnCurve = new Optional<AnimationCurve>(configuration.daytimeEnemies.spawnAmountCurve);
                daytimeSpawnProbabilityRange = new Optional<float>(configuration.daytimeEnemies.spawnAmountRange);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (DaytimeEnemyTypeConfiguration item in configuration.daytimeEnemies.enemyTypes.Values)
                {
                    if (item.isSet() && item.rarity.Value(0) > 0)
                    {
                        options.Add(new EnemyTypeOptions(item));
                    }
                }
                if (options.Count > 0)
                {
                    daytimeEnemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            if (configuration.outsideEnemies.enabled.Value)
            {
                maxOutsidePowerCount = new Optional<int>(configuration.outsideEnemies.maxPowerCount);
                outsideSpawnCurve = new Optional<AnimationCurve>(configuration.outsideEnemies.spawnAmountCurve);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.outsideEnemies.enemyTypes.Values)
                {
                    if (item.isSet() && item.rarity.Value(0) > 0)
                    {
                        options.Add(new EnemyTypeOptions(item));
                    }
                }
                if (options.Count > 0)
                {
                    outsideEnemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            if (configuration.scrap.enabled.Value)
            {
                scrapAmountMultiplier = new Optional<float>(configuration.scrap.scrapAmountMultiplier);
                scrapValueMultiplier = new Optional<float>(configuration.scrap.scrapValueMultiplier);
                minScrap = new Optional<int>(configuration.scrap.minScrap);
                maxScrap = new Optional<int>(configuration.scrap.maxScrap);
                List<ItemOptions> options = new List<ItemOptions>();
                foreach (var entry in configuration.scrap.items)
                {
                    if (entry.Value.isSet() && entry.Value.rarity.Value(0) > 0)
                    {
                        ItemOptions option = new ItemOptions(entry.Value);
                        option.name = entry.Key.itemName;
                        option.id = entry.Key.name;
                        options.Add(option);
                    }
                }
                if (options.Count > 0)
                {
                    scrap = new Optional<List<ItemOptions>>(options);
                }
            }

            if (configuration.dungeon.enabled.Value)
            {
                mapSizeMultiplier = new Optional<float>(configuration.dungeon.mapSizeMultiplier);
                List<DungeonFlowOptions> options = new List<DungeonFlowOptions>();
                foreach (var entry in configuration.dungeon.dungeonFlowConfigurations)
                {
                    if (entry.Value.isSet() && entry.Value.rarity.Value(0) > 0)
                    {
                        DungeonFlowOptions option = new DungeonFlowOptions(entry.Value);
                        option.name = entry.Key;
                        option.id = entry.Key;
                        options.Add(option);
                    }
                }
                if (options.Count > 0)
                {
                    dungeonFlows = new Optional<List<DungeonFlowOptions>>(options);
                }
            }

            if (configuration.trap.enabled.Value)
            {
                List<TrapOptions> options = new List<TrapOptions>();
                foreach (var entry in configuration.trap.traps)
                {
                    // Always add traps, for the most part
                    TrapOptions option = new TrapOptions(entry.Value);
                    option.id = entry.Key.name;
                    options.Add(option);
                }
                if (options.Count > 0)
                {
                    traps = new Optional<List<TrapOptions>>(options);
                }
            }

            if (configuration.price.enabled.Value)
            {
                List<PriceOptions> options = new List<PriceOptions>();
                foreach (var entry in configuration.price.moons)
                {
                    if (entry.Value.isSet())
                    {
                        PriceOptions option = new PriceOptions(entry.Value);
                        option.name = entry.Key.getLevel().PlanetName;
                        option.id = entry.Key.getLevelName();
                        options.Add(option);
                    }
                }
                if (options.Count > 0)
                {
                    price = new Optional<List<PriceOptions>>(options);
                }
            }
        }

        // Exact same thing as the LevelConfiguration constructor... Just going to copy paste this since it is not going to be updated anyways, and removed in the future
        public Preset(GlobalConfiguration configuration)
        {
            if (configuration.enemyConfiguration.enabled.Value && configuration.enemyConfiguration.isSet())
            {
                maxPowerCount = new Optional<int>(configuration.enemyConfiguration.maxPowerCount);
                spawnCurve = new Optional<AnimationCurve>(configuration.enemyConfiguration.spawnAmountCurve);
                spawnProbabilityRange = new Optional<float>(configuration.enemyConfiguration.spawnAmountRange);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.enemyConfiguration.enemyTypes.Values)
                {
                    if (item.isSet() && item.rarity.Value(0) > 0)
                    {
                        options.Add(new EnemyTypeOptions(item));
                    }
                }
                if (options.Count > 0)
                {
                    enemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            if (configuration.daytimeEnemyConfiguration.enabled.Value && configuration.daytimeEnemyConfiguration.isSet())
            {
                maxDaytimePowerCount = new Optional<int>(configuration.daytimeEnemyConfiguration.maxPowerCount);
                daytimeSpawnCurve = new Optional<AnimationCurve>(configuration.daytimeEnemyConfiguration.spawnAmountCurve);
                daytimeSpawnProbabilityRange = new Optional<float>(configuration.daytimeEnemyConfiguration.spawnAmountRange);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.daytimeEnemyConfiguration.enemyTypes.Values)
                {
                    if (item.isSet() && item.rarity.Value(0) > 0)
                    {
                        options.Add(new EnemyTypeOptions(item));
                    }
                }
                if (options.Count > 0)
                {
                    daytimeEnemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            if (configuration.outsideEnemyConfiguration.enabled.Value && configuration.outsideEnemyConfiguration.isSet())
            {
                maxOutsidePowerCount = new Optional<int>(configuration.outsideEnemyConfiguration.maxPowerCount);
                outsideSpawnCurve = new Optional<AnimationCurve>(configuration.outsideEnemyConfiguration.spawnAmountCurve);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.outsideEnemyConfiguration.enemyTypes.Values)
                {
                    if (item.isSet() && item.rarity.Value(0) > 0)
                    {
                        options.Add(new EnemyTypeOptions(item));
                    }
                }
                if (options.Count > 0)
                {
                    outsideEnemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            if (configuration.scrapConfiguration.enabled.Value && configuration.scrapConfiguration.isSet())
            {
                scrapAmountMultiplier = new Optional<float>(configuration.scrapConfiguration.scrapAmountMultiplier);
                scrapValueMultiplier = new Optional<float>(configuration.scrapConfiguration.scrapValueMultiplier);
                minScrap = new Optional<int>(configuration.scrapConfiguration.minScrap);
                maxScrap = new Optional<int>(configuration.scrapConfiguration.maxScrap);
                List<ItemOptions> options = new List<ItemOptions>();
                foreach (var entry in configuration.scrapConfiguration.items)
                {
                    if (entry.Value.isSet() && entry.Value.rarity.Value(0) > 0)
                    {
                        ItemOptions option = new ItemOptions(entry.Value);
                        option.name = entry.Key.itemName;
                        option.id = entry.Key.name;
                        options.Add(option);
                    }
                }
                if (options.Count > 0)
                {
                    scrap = new Optional<List<ItemOptions>>(options);
                }
            }

            if (configuration.dungeonConfiguration.enabled.Value && configuration.dungeonConfiguration.isSet())
            {
                mapSizeMultiplier = new Optional<float>(configuration.dungeonConfiguration.mapSizeMultiplier);
                List<DungeonFlowOptions> options = new List<DungeonFlowOptions>();
                foreach (var entry in configuration.dungeonConfiguration.dungeonFlowConfigurations)
                {
                    if (entry.Value.isSet() && entry.Value.rarity.Value(0) > 0)
                    {
                        DungeonFlowOptions option = new DungeonFlowOptions(entry.Value);
                        option.name = entry.Key;
                        option.id = entry.Key;
                        options.Add(option);
                    }
                }
                if (options.Count > 0)
                {
                    dungeonFlows = new Optional<List<DungeonFlowOptions>>(options);
                }
            }

            if (configuration.trapConfiguration.enabled.Value && configuration.trapConfiguration.isSet())
            {
                List<TrapOptions> options = new List<TrapOptions>();
                foreach (var entry in configuration.trapConfiguration.traps)
                {
                    TrapOptions option = new TrapOptions(entry.Value);
                    option.id = entry.Key.name;

                    // Only add if it is set
                    options.Add(option);
                }
                if (options.Count > 0)
                {
                    traps = new Optional<List<TrapOptions>>(options);
                }
            }

            if (configuration.priceConfiguration.enabled.Value && configuration.priceConfiguration.isSet())
            {
                List<PriceOptions> options = new List<PriceOptions>();
                foreach (var entry in configuration.priceConfiguration.moons)
                {
                    if (entry.Value.isSet())
                    {
                        PriceOptions option = new PriceOptions(entry.Value);
                        option.name = entry.Key.getLevel().PlanetName;
                        option.id = entry.Key.getLevelName();
                        options.Add(option);
                    }
                }
                if (options.Count > 0)
                {
                    price = new Optional<List<PriceOptions>>(options);
                }
            }
        }
    }

    public class ImportData
    {
        public Dictionary<string, Preset> presets = new Dictionary<string, Preset>();
        public Dictionary<string, string> levels = new Dictionary<string, string>();

        // Create a single preset per levels
        public Dictionary<Guid, LevelPreset> generate(GlobalInformation info)
        {
            Dictionary<Guid, LevelPreset> levelPresets = new Dictionary<Guid, LevelPreset>();

            Dictionary<string, EnemyType> enemyTypeMap = new Dictionary<string, EnemyType>();
            foreach (EnemyType item in info.allEnemyTypes)
            {
                enemyTypeMap.TryAdd(item.name, item);
            }

            Dictionary<string, Item> itemMap = new Dictionary<string, Item>();
            foreach (Item item in info.allItems)
            {
                itemMap.TryAdd(item.name, item);
            }

            Dictionary<string, DirectionalSpawnableMapObject> trapMap = new Dictionary<string, DirectionalSpawnableMapObject>();
            foreach (DirectionalSpawnableMapObject item in info.allSpawnableMapObjects)
            {
                trapMap.TryAdd(item.obj.name, item);
            }
            foreach (var entry in levels)
            {
                Optional<Guid> foundGuid = SelectableLevelCache.getGuid(entry.Key);
                if (foundGuid.set)
                {
                    if (presets.ContainsKey(entry.Value))
                    {
                        List<Preset> parents = getChain(presets[entry.Value]);
                        LevelPreset levelPreset = new LevelPreset();

                        levelPreset.maxPowerCount = set<int>(parents, nameof(Preset.maxPowerCount));
                        levelPreset.spawnCurve = set<AnimationCurve>(parents, nameof(Preset.spawnCurve));
                        levelPreset.spawnProbabilityRange = set<float>(parents, nameof(Preset.spawnProbabilityRange));

                        levelPreset.maxDaytimePowerCount = set<int>(parents, nameof(Preset.maxDaytimePowerCount));
                        levelPreset.daytimeSpawnCurve = set<AnimationCurve>(parents, nameof(Preset.daytimeSpawnCurve));
                        levelPreset.daytimeSpawnProbabilityRange = set<float>(parents, nameof(Preset.daytimeSpawnProbabilityRange));

                        levelPreset.maxOutsidePowerCount = set<int>(parents, nameof(Preset.maxOutsidePowerCount));
                        levelPreset.outsideSpawnCurve = set<AnimationCurve>(parents, nameof(Preset.outsideSpawnCurve));

                        levelPreset.scrapAmountMultiplier = set<float>(parents, nameof(Preset.scrapAmountMultiplier));
                        levelPreset.scrapValueMultiplier = set<float>(parents, nameof(Preset.scrapValueMultiplier));
                        levelPreset.minScrap = set<int>(parents, nameof(Preset.minScrap));
                        levelPreset.maxScrap = set<int>(parents, nameof(Preset.maxScrap));

                        levelPreset.mapSizeMultiplier = set<float>(parents, nameof(Preset.mapSizeMultiplier));

                        {
                            Optional<List<EnemyTypeOptions>> enemyOptions = set<List<EnemyTypeOptions>>(parents, nameof(Preset.enemies));
                            if (enemyOptions.isSet())
                            {
                                Dictionary<EnemyType, LevelPresetEnemyType> enemies = new Dictionary<EnemyType, LevelPresetEnemyType>();
                                foreach (EnemyTypeOptions option in enemyOptions.value)
                                {
                                    if (enemyTypeMap.TryGetValue(option.id, out EnemyType enemyType))
                                    {
                                        LevelPresetEnemyType type = new LevelPresetEnemyType();
                                        type.rarity = set<int>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.rarity));
                                        type.powerLevel = set<int>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.powerLevel));
                                        type.spawnChanceCurve = set<AnimationCurve>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.spawnChanceCurve));
                                        type.spawnFalloffCurve = set<AnimationCurve>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.spawnFalloffCurve));
                                        if (type.spawnFalloffCurve.isSet())
                                        {
                                            type.spawnFalloffCurve.value = new AnimationCurve(type.spawnFalloffCurve.value.keys.Select(key => new Keyframe(key.time / 10, key.value)).ToArray());
                                        }
                                        type.useSpawnFalloff = set<bool>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.useSpawnFalloff));
                                        type.killable = set<bool>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.killable));
                                        type.enemyHp = set<int>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.enemyHp));
                                        type.stunnable = set<bool>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.stunnable));
                                        type.stunGameDifficultyMultiplier = set<float>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.stunGameDifficultyMultiplier));
                                        type.stunTimeMultiplier = set<float>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.stunTimeMultiplier));
                                        type.doorSpeedMultiplier = set<float>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.doorSpeedMultiplier));

                                        enemies.Add(enemyType, type);
                                    }
                                    else
                                    {
                                        Plugin.LETHAL_LOGGER.LogError($"Unable to find enemy type with id {option.id}");
                                    }
                                }
                                if (enemies.Count > 0)
                                {
                                    levelPreset.enemies.setValue(enemies);
                                }
                            }
                        }

                        {
                            Optional<List<EnemyTypeOptions>> daytimeEnemyOptions = set<List<EnemyTypeOptions>>(parents, nameof(Preset.daytimeEnemies));
                            if (daytimeEnemyOptions.isSet())
                            {
                                Dictionary<EnemyType, LevelPresetEnemyType> enemies = new Dictionary<EnemyType, LevelPresetEnemyType>();
                                foreach (EnemyTypeOptions option in daytimeEnemyOptions.value)
                                {
                                    if (enemyTypeMap.TryGetValue(option.id, out EnemyType enemyType))
                                    {
                                        LevelPresetEnemyType type = new LevelPresetEnemyType();
                                        type.rarity = set<int>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.rarity));
                                        type.powerLevel = set<int>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.powerLevel));
                                        type.spawnChanceCurve = set<AnimationCurve>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.spawnChanceCurve));
                                        type.spawnFalloffCurve = set<AnimationCurve>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.spawnFalloffCurve));
                                        if (type.spawnFalloffCurve.isSet())
                                        {
                                            type.spawnFalloffCurve.value = new AnimationCurve(type.spawnFalloffCurve.value.keys.Select(key => new Keyframe(key.time / 10, key.value)).ToArray());
                                        }
                                        type.useSpawnFalloff = set<bool>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.useSpawnFalloff));
                                        type.killable = set<bool>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.killable));
                                        type.enemyHp = set<int>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.enemyHp));
                                        type.stunnable = set<bool>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.stunnable));
                                        type.stunGameDifficultyMultiplier = set<float>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.stunGameDifficultyMultiplier));
                                        type.stunTimeMultiplier = set<float>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.stunTimeMultiplier));
                                        type.doorSpeedMultiplier = set<float>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.doorSpeedMultiplier));

                                        enemies.Add(enemyType, type);
                                    }
                                    else
                                    {
                                        Plugin.LETHAL_LOGGER.LogError($"Unable to find enemy type with id {option.id}");
                                    }
                                }
                                if (enemies.Count > 0)
                                {
                                    levelPreset.daytimeEnemies.setValue(enemies);
                                }
                            }
                        }

                        {
                            Optional<List<EnemyTypeOptions>> outsideEnemyOptions = set<List<EnemyTypeOptions>>(parents, nameof(Preset.outsideEnemies));
                            if (outsideEnemyOptions.isSet())
                            {
                                Dictionary<EnemyType, LevelPresetEnemyType> enemies = new Dictionary<EnemyType, LevelPresetEnemyType>();
                                foreach (EnemyTypeOptions option in outsideEnemyOptions.value)
                                {
                                    if (enemyTypeMap.TryGetValue(option.id, out EnemyType enemyType))
                                    {
                                        LevelPresetEnemyType type = new LevelPresetEnemyType();
                                        type.rarity = set<int>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.rarity));
                                        type.powerLevel = set<int>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.powerLevel));
                                        type.spawnChanceCurve = set<AnimationCurve>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.spawnChanceCurve));
                                        type.spawnFalloffCurve = set<AnimationCurve>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.spawnFalloffCurve));
                                        if (type.spawnFalloffCurve.isSet())
                                        {
                                            type.spawnFalloffCurve.value = new AnimationCurve(type.spawnFalloffCurve.value.keys.Select(key => new Keyframe(key.time / 10, key.value)).ToArray());
                                        }
                                        type.useSpawnFalloff = set<bool>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.useSpawnFalloff));
                                        type.killable = set<bool>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.killable));
                                        type.enemyHp = set<int>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.enemyHp));
                                        type.stunnable = set<bool>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.stunnable));
                                        type.stunGameDifficultyMultiplier = set<float>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.stunGameDifficultyMultiplier));
                                        type.stunTimeMultiplier = set<float>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.stunTimeMultiplier));
                                        type.doorSpeedMultiplier = set<float>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.doorSpeedMultiplier));

                                        enemies.Add(enemyType, type);
                                    }
                                    else
                                    {
                                        Plugin.LETHAL_LOGGER.LogError($"Unable to find enemy type with id {option.id}");
                                    }
                                }
                                if (enemies.Count > 0)
                                {
                                    levelPreset.outsideEnemies.setValue(enemies);
                                }
                            }
                        }

                        {
                            Optional<List<ItemOptions>> scrapOptions = set<List<ItemOptions>>(parents, nameof(Preset.scrap));
                            if (scrapOptions.isSet())
                            {
                                Dictionary<Item, LevelPresetItem> items = new Dictionary<Item, LevelPresetItem>();
                                foreach (ItemOptions option in scrapOptions.value)
                                {
                                    if (itemMap.TryGetValue(option.id, out Item item))
                                    {
                                        LevelPresetItem type = new LevelPresetItem();
                                        type.rarity = set<int>(parents, nameof(Preset.scrap), option.id, nameof(ItemOptions.rarity));
                                        type.minValue = set<int>(parents, nameof(Preset.scrap), option.id, nameof(ItemOptions.minValue));
                                        type.maxValue = set<int>(parents, nameof(Preset.scrap), option.id, nameof(ItemOptions.maxValue));
                                        // Update the weight
                                        type.weight = set<float>(parents, nameof(Preset.scrap), option.id, nameof(ItemOptions.weight));
                                        if (type.weight.isSet())
                                        {
                                            type.weight.value = (type.weight.value / 100) + 1;
                                        }
                                        type.conductive = set<bool>(parents, nameof(Preset.scrap), option.id, nameof(ItemOptions.conductive));

                                        items.Add(item, type);
                                    }
                                    else
                                    {
                                        Plugin.LETHAL_LOGGER.LogError($"Unable to find item with id {option.id}");
                                    }
                                }
                                if (items.Count > 0)
                                {
                                    levelPreset.scrap.setValue(items);
                                }
                            }
                        }

                        {
                            Optional<List<DungeonFlowOptions>> dungeonFlowOptions = set<List<DungeonFlowOptions>>(parents, nameof(Preset.dungeonFlows));
                            if (dungeonFlowOptions.isSet())
                            {
                                Dictionary<string, LevelPresetDungeonFlow> flows = new Dictionary<string, LevelPresetDungeonFlow>();
                                foreach (DungeonFlowOptions option in dungeonFlowOptions.value)
                                {
                                    LevelPresetDungeonFlow type = new LevelPresetDungeonFlow();
                                    type.rarity = set<int>(parents, nameof(Preset.dungeonFlows), option.id, nameof(DungeonFlowOptions.rarity));
                                    type.factorySizeMultiplier = set<float>(parents, nameof(Preset.dungeonFlows), option.id, nameof(DungeonFlowOptions.factorySizeMultiplier));

                                    flows.Add(option.id, type);
                                }

                                if (flows.Count > 0)
                                {
                                    levelPreset.dungeonFlows.setValue(flows);
                                }
                            }
                        }

                        {
                            Optional<List<TrapOptions>> trapOptions = set<List<TrapOptions>>(parents, nameof(Preset.traps));
                            if (trapOptions.isSet())
                            {
                                Dictionary<DirectionalSpawnableMapObject, LevelPresetTrap> traps = new Dictionary<DirectionalSpawnableMapObject, LevelPresetTrap>();
                                foreach (TrapOptions option in trapOptions.value)
                                {
                                    if (trapMap.TryGetValue(option.id, out DirectionalSpawnableMapObject obj))
                                    {
                                        LevelPresetTrap type = new LevelPresetTrap();
                                        type.spawnCurve = set<AnimationCurve>(parents, nameof(Preset.traps), option.id, nameof(TrapOptions.spawnCurve));

                                        traps.Add(obj, type);
                                    }
                                    else
                                    {
                                        Plugin.LETHAL_LOGGER.LogWarning($"Could not find trap with id {option.id}");
                                    }
                                }
                                if (traps.Count > 0)
                                {
                                    levelPreset.traps.setValue(traps);
                                }
                            }
                        }

                        {
                            Optional<List<PriceOptions>> priceOptions = set<List<PriceOptions>>(parents, nameof(Preset.price));
                            if (priceOptions.isSet())
                            {
                                Dictionary<Guid, LevelPresetPrice> prices = new Dictionary<Guid, LevelPresetPrice>();
                                foreach (PriceOptions option in priceOptions.value)
                                {
                                    Optional<Guid> optGuid = SelectableLevelCache.getGuid(option.id);
                                    if (optGuid.isSet())
                                    {
                                        LevelPresetPrice type = new LevelPresetPrice();
                                        type.price = set<int>(parents, nameof(Preset.price), option.id, nameof(PriceOptions.price));

                                        prices.Add(optGuid.value, type);
                                    }
                                    else
                                    {
                                        Plugin.LETHAL_LOGGER.LogWarning($"Could not find matching SelectableLevel with id {option.id}");
                                    }
                                }
                                if (prices.Count > 0)
                                {
                                    levelPreset.price.setValue(prices);
                                }

                            }
                        }

                        // Could move this to the top for faster return
                        if (!levelPresets.TryAdd(foundGuid.value, levelPreset))
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Already added a preset for the level {entry.Key}!");
                        }
                    } else
                    {
                        Plugin.LETHAL_LOGGER.LogError($"Unable to find preset {entry.Value} for level {entry.Key}");
                    }
                }
                else
                {
                    Plugin.LETHAL_LOGGER.LogError($"Unable to find a level with the name {entry.Key}, skipping");
                }
            }

            return levelPresets;
        }

        public List<Preset> getChain(Preset original)
        {
            List<Preset> parents = new List<Preset>() { original };
            HashSet<string> checkset = new HashSet<string>() { original.id };

            Preset current = original;
            while (presets.ContainsKey(current.parent) && !checkset.Contains(current.parent))
            {
                current = presets[current.parent];
                if (current != original)
                {
                    parents.Add(current);
                    checkset.Add(current.id);
                }
                else
                {
                    break;
                }
            }

            return parents;
        }

        // Lazy coding
        internal Optional<T> set<T>(List<Preset> parents, params string[] values)
        {
            foreach (Preset preset in parents)
            {
                Object obj = preset;
                for (int i = 0; i < values.Length; i++)
                {
                    string name = values[i];
                    Type objType = obj.GetType();
                    if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Optional<>))
                    {
                        // Not the optional we are looking for, but it could be a list or something
                        IOptional iOpt = (IOptional)obj;
                        if (iOpt.isSet())
                        {
                            obj = iOpt.getValue();
                            i--;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // Search for an object with the id
                        int size = ((IList)obj).Count;
                        bool found = false;
                        foreach (Object element in (IList)obj)
                        {
                            if (element is Identifiable)
                            {
                                if ((element as Identifiable).id == name)
                                {
                                    obj = element;
                                    found = true;
                                    break;
                                }
                            }
                        }

                        // Not found, so return empty. If it is not found, then it means that a parent has explicity removed it and we don't want to continue
                        if (!found)
                        {
                            return Optional<T>.Empty();
                        }
                    }
                    else
                    {
                        // Attempt to get the next field with the value
                        FieldInfo info = objType.GetFields().Where(f => f.Name == name).First();
                        if (info != null)
                        {
                            obj = info.GetValue(obj);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (obj.GetType() == typeof(Optional<T>))
                {
                    Optional<T> potential = obj as Optional<T>;
                    if (potential.set)
                    {
                        return potential;
                    }
                }
            }
            return Optional<T>.Empty();
        }
    }

    public class LevelPresetDungeonFlow
    {
        public Optional<int> rarity = new Optional<int>();
        public Optional<float> factorySizeMultiplier = new Optional<float>();
    }

    public class LevelPresetPrice
    {
        public Optional<int> price = new Optional<int>();
    }

    public class LevelPresetTrap
    {
        public Optional<AnimationCurve> spawnCurve = new Optional<AnimationCurve>();
    }

    public class LevelPresetItem
    {
        public Optional<int> rarity = new Optional<int>();
        public Optional<int> minValue = new Optional<int>();
        public Optional<int> maxValue = new Optional<int>();
        public Optional<bool> conductive = new Optional<bool>();
        public Optional<float> weight = new Optional<float>();
    }

    public class LevelPresetEnemyType
    {
        public Optional<int> rarity = new Optional<int>();

        public Optional<int> maxEnemyCount = new Optional<int>();
        public Optional<int> powerLevel = new Optional<int>();
        public Optional<AnimationCurve> spawnChanceCurve = new Optional<AnimationCurve>();
        public Optional<AnimationCurve> spawnFalloffCurve = new Optional<AnimationCurve>();
        public Optional<bool> useSpawnFalloff = new Optional<bool>();

        public Optional<bool> killable = new Optional<bool>();
        public Optional<int> enemyHp = new Optional<int>();

        public Optional<bool> stunnable = new Optional<bool>();
        public Optional<float> stunGameDifficultyMultiplier = new Optional<float>();
        public Optional<float> stunTimeMultiplier = new Optional<float>();
        public Optional<float> doorSpeedMultiplier = new Optional<float>();
    }

    public class LevelPreset
    {
        public Optional<Dictionary<EnemyType, LevelPresetEnemyType>> enemies = new Optional<Dictionary<EnemyType, LevelPresetEnemyType>>();
        public Optional<int> maxPowerCount = new Optional<int>();
        public Optional<AnimationCurve> spawnCurve = new Optional<AnimationCurve>();
        public Optional<float> spawnProbabilityRange = new Optional<float>();
        
        public Optional<Dictionary<EnemyType, LevelPresetEnemyType>> daytimeEnemies = new Optional<Dictionary<EnemyType, LevelPresetEnemyType>>();
        public Optional<int> maxDaytimePowerCount = new Optional<int>();
        public Optional<AnimationCurve> daytimeSpawnCurve = new Optional<AnimationCurve>();
        public Optional<float> daytimeSpawnProbabilityRange = new Optional<float>();
        
        public Optional<Dictionary<EnemyType, LevelPresetEnemyType>> outsideEnemies = new Optional<Dictionary<EnemyType, LevelPresetEnemyType>>();
        public Optional<int> maxOutsidePowerCount = new Optional<int>();
        public Optional<AnimationCurve> outsideSpawnCurve = new Optional<AnimationCurve>();

        public Optional<Dictionary<Item, LevelPresetItem>> scrap = new Optional<Dictionary<Item, LevelPresetItem>>();
        public Optional<float> scrapAmountMultiplier = new Optional<float>();
        public Optional<float> scrapValueMultiplier = new Optional<float>();
        public Optional<int> minScrap = new Optional<int>();
        public Optional<int> maxScrap = new Optional<int>();
        
        public Optional<Dictionary<string, LevelPresetDungeonFlow>> dungeonFlows = new Optional<Dictionary<string, LevelPresetDungeonFlow>>();
        public Optional<float> mapSizeMultiplier = new Optional<float>();
        
        public Optional<Dictionary<Guid, LevelPresetPrice>> price = new Optional<Dictionary<Guid, LevelPresetPrice>>();
        
        public Optional<Dictionary<DirectionalSpawnableMapObject, LevelPresetTrap>> traps = new Optional<Dictionary<DirectionalSpawnableMapObject, LevelPresetTrap>>();
    }
}

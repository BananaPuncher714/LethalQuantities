﻿using LethalQuantities.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.UIElements.UIR.Implementation.UIRStylePainter;
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

        public bool get(out T val)
        {
            val = this.value;
            return set;
        }

        public Optional(CustomEntry<T> entry)
        {
            value = entry.localValue(value);
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
                weight.value = (weight.value - 1) * 100;
            }
        }

        public ItemOptions(Item item)
        {
            name = item.itemName;
            id = item.name;
            conductive = new Optional<bool>(item.isConductiveMetal, false);
            // Convert to pounds
            weight = new Optional<float>((item.weight - 1) * 100, false);
            minValue = new Optional<int>(Math.Min(item.minValue, item.maxValue), false);
            maxValue = new Optional<int>(Math.Max(item.minValue, item.maxValue), false);
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
        public Optional<float> powerLevel = new Optional<float>();
        public Optional<int> groupSpawnCount = new Optional<int>();
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
            powerLevel = new Optional<float>(configuration.powerLevel);
            groupSpawnCount = new Optional<int>(configuration.spawnGroupCount);
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

        public EnemyTypeOptions(EnemyType type)
        {
            id = type.name;
            name = type.getFriendlyName();
            doorSpeedMultiplier = new Optional<float>(type.doorSpeedMultiplier, false);
            killable = new Optional<bool>(type.canDie, false);
            maxEnemyCount = new Optional<int>(type.MaxCount, false);
            powerLevel = new Optional<float>(type.PowerLevel, false);
            groupSpawnCount = new Optional<int>(type.spawnInGroupsOf, false);
            spawnChanceCurve = new Optional<AnimationCurve>(type.probabilityCurve, false);
            stunGameDifficultyMultiplier = new Optional<float>(type.stunGameDifficultyMultiplier, false);
            stunTimeMultiplier = new Optional<float>(type.stunTimeMultiplier, false);
            stunnable = new Optional<bool>(type.canBeStunned, false);
            useSpawnFalloff = new Optional<bool>(type.useNumberSpawnedFalloff, false);
            spawnFalloffCurve = new Optional<AnimationCurve>(type.numberSpawnedFalloff, false);
            spawnFalloffCurve.value = new AnimationCurve(spawnFalloffCurve.value.keys.Select(key => new Keyframe(key.time * 10, key.value)).ToArray());
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
        public Optional<string> levelDescription = new Optional<string>();
        public Optional<string> riskLevel = new Optional<string>();
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

        public void update(ExportData data)
        {
            // TODO Improve this
            // For now, add some small fixes like updating the prices
            if (price.value != null)
            {
                price.value.RemoveAll(opt => !data.levels.ContainsKey(opt.id));
                foreach (PriceOptions priceOpt in price.value)
                {
                    if (priceOpt.price.value == -1)
                    {
                        if (data.levels.TryGetValue(priceOpt.id, out ExportTypeSelectableLevel level)) {
                            priceOpt.price.value = level.price;
                        }
                    }
                }
            }

            if (scrap.value != null)
            {
                scrap.value.RemoveAll(opt => !data.items.ContainsKey(opt.id));
            }

            if (enemies.value != null)
            {
                enemies.value.RemoveAll(opt => !data.enemies.ContainsKey(opt.id));
            }

            if (daytimeEnemies.value != null)
            {
                daytimeEnemies.value.RemoveAll(opt => !data.enemies.ContainsKey(opt.id));
            }

            if (outsideEnemies.value != null)
            {
                outsideEnemies.value.RemoveAll(opt => !data.enemies.ContainsKey(opt.id));
            }

            if (dungeonFlows.value != null)
            {
                dungeonFlows.value.RemoveAll(opt => !data.dungeon_flows.Contains(opt.id));
            }

            if (traps.value != null)
            {
                traps.value.RemoveAll(opt => !data.traps.ContainsKey(opt.id));
            }
        }

        public Preset(LevelConfiguration configuration)
        {
            {
                SelectableLevel level = configuration.levelGuid.getLevel();

                riskLevel = new Optional<string>(level.riskLevel);
                levelDescription = new Optional<string>(level.LevelDescription);
            }
            {
                maxPowerCount = new Optional<int>(configuration.enemies.maxPowerCount);
                spawnCurve = new Optional<AnimationCurve>(configuration.enemies.spawnAmountCurve);
                spawnProbabilityRange = new Optional<float>(configuration.enemies.spawnAmountRange);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.enemies.enemyTypes.Values)
                {
                    int rarity = item.rarity.Value(0);
                    if (rarity > 0)
                    {
                        if (item.isSet())
                        {
                            options.Add(new EnemyTypeOptions(item));
                        }
                        else
                        {
                            EnemyTypeOptions option = new EnemyTypeOptions(item.type);
                            option.rarity = new Optional<int>(item.rarity.localValue(0), false);
                            option.enemyHp = new Optional<int>(item.enemyHp);
                            options.Add(option);
                        }
                    }
                }
                if (options.Count > 0)
                {
                    enemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            {
                maxDaytimePowerCount = new Optional<int>(configuration.daytimeEnemies.maxPowerCount);
                daytimeSpawnCurve = new Optional<AnimationCurve>(configuration.daytimeEnemies.spawnAmountCurve);
                daytimeSpawnProbabilityRange = new Optional<float>(configuration.daytimeEnemies.spawnAmountRange);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (DaytimeEnemyTypeConfiguration item in configuration.daytimeEnemies.enemyTypes.Values)
                {
                    int rarity = item.rarity.Value(0);
                    if (rarity > 0)
                    {
                        if (item.isSet())
                        {
                            options.Add(new EnemyTypeOptions(item));
                        }
                        else
                        {
                            EnemyTypeOptions option = new EnemyTypeOptions(item.type);
                            option.rarity = new Optional<int>(item.rarity.localValue(0), false);
                            option.enemyHp = new Optional<int>(item.enemyHp);
                            options.Add(option);
                        }
                    }
                }
                if (options.Count > 0)
                {
                    daytimeEnemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            {
                maxOutsidePowerCount = new Optional<int>(configuration.outsideEnemies.maxPowerCount);
                outsideSpawnCurve = new Optional<AnimationCurve>(configuration.outsideEnemies.spawnAmountCurve);
                List<EnemyTypeOptions> options = new List<EnemyTypeOptions>();
                foreach (EnemyTypeConfiguration item in configuration.outsideEnemies.enemyTypes.Values)
                {
                    int rarity = item.rarity.Value(0);
                    if (rarity > 0)
                    {
                        if (item.isSet())
                        {
                            options.Add(new EnemyTypeOptions(item));
                        }
                        else
                        {
                            EnemyTypeOptions option = new EnemyTypeOptions(item.type);
                            option.rarity = new Optional<int>(item.rarity.localValue(0), false);
                            option.enemyHp = new Optional<int>(item.enemyHp);
                            options.Add(option);
                        }
                    }
                }
                if (options.Count > 0)
                {
                    outsideEnemies = new Optional<List<EnemyTypeOptions>>(options);
                }
            }

            {
                scrapAmountMultiplier = new Optional<float>(configuration.scrap.scrapAmountMultiplier);
                scrapValueMultiplier = new Optional<float>(configuration.scrap.scrapValueMultiplier);
                minScrap = new Optional<int>(configuration.scrap.minScrap);
                maxScrap = new Optional<int>(configuration.scrap.maxScrap);
                List<ItemOptions> options = new List<ItemOptions>();
                foreach (var entry in configuration.scrap.items)
                {
                    int rarity = entry.Value.rarity.Value(0);
                    if (rarity > 0)
                    {
                        if (entry.Value.isSet())
                        {
                            ItemOptions option = new ItemOptions(entry.Value);
                            option.name = entry.Key.itemName;
                            option.id = entry.Key.name;
                            options.Add(option);
                        }
                        else
                        {
                            ItemOptions option = new ItemOptions(entry.Key);
                            option.rarity = new Optional<int>(entry.Value.rarity.localValue(0), false);
                            options.Add(option);
                        }
                    }
                }
                if (options.Count > 0)
                {
                    scrap = new Optional<List<ItemOptions>>(options);
                }
            }

            {
                mapSizeMultiplier = new Optional<float>(configuration.dungeon.mapSizeMultiplier);
                List<DungeonFlowOptions> options = new List<DungeonFlowOptions>();
                foreach (var entry in configuration.dungeon.dungeonFlowConfigurations)
                {
                    int rarity = entry.Value.rarity.Value(0);
                    if (rarity > 0)
                    {
                        if (entry.Value.isSet())
                        {
                            DungeonFlowOptions option = new DungeonFlowOptions(entry.Value);
                            option.name = entry.Key;
                            option.id = entry.Key;
                            options.Add(option);
                        }
                        else
                        {
                            DungeonFlowOptions option = new DungeonFlowOptions(entry.Value);
                            option.name = entry.Key;
                            option.id = entry.Key;
                            option.factorySizeMultiplier = new Optional<float>(entry.Value.factorySizeMultiplier);
                            option.factorySizeMultiplier.set = false;
                            option.rarity = new Optional<int>(rarity, false);
                            options.Add(option);
                        }
                    }
                }
                if (options.Count > 0)
                {
                    dungeonFlows = new Optional<List<DungeonFlowOptions>>(options);
                }
            }

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
}

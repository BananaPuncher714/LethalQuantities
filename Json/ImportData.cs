using LethalQuantities.Objects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = System.Object;

namespace LethalQuantities.Json
{
    public class ImportData
    {
        public ExportData defaults;
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

                        levelPreset.riskLevel = set<string>(parents, nameof(Preset.riskLevel));
                        levelPreset.levelDescription = set<string>(parents, nameof(Preset.levelDescription));

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
                                        type.maxEnemyCount = set<int>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.maxEnemyCount));
                                        type.powerLevel = set<float>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.powerLevel));
                                        type.groupSpawnCount = set<int>(parents, nameof(Preset.enemies), option.id, nameof(EnemyTypeOptions.groupSpawnCount));
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
                                        MiniLogger.LogError($"Unable to find enemy type with id {option.id}");
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
                                        type.maxEnemyCount = set<int>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.maxEnemyCount));
                                        type.powerLevel = set<float>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.powerLevel));
                                        type.groupSpawnCount = set<int>(parents, nameof(Preset.daytimeEnemies), option.id, nameof(EnemyTypeOptions.groupSpawnCount));
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
                                        MiniLogger.LogError($"Unable to find enemy type with id {option.id}");
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
                                        type.maxEnemyCount = set<int>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.maxEnemyCount));
                                        type.powerLevel = set<float>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.powerLevel));
                                        type.groupSpawnCount = set<int>(parents, nameof(Preset.outsideEnemies), option.id, nameof(EnemyTypeOptions.groupSpawnCount));
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
                                        MiniLogger.LogError($"Unable to find enemy type with id {option.id}");
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
                                        MiniLogger.LogError($"Unable to find item with id {option.id}");
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
                                        MiniLogger.LogWarning($"Could not find trap with id {option.id}");
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
                                        MiniLogger.LogWarning($"Could not find matching SelectableLevel with id {option.id}");
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
                            MiniLogger.LogError($"Already added a preset for the level {entry.Key}!");
                        }
                    }
                    else
                    {
                        MiniLogger.LogError($"Unable to find preset {entry.Value} for level {entry.Key}");
                    }
                }
                else
                {
                    MiniLogger.LogError($"Unable to find a level with the name {entry.Key}, skipping");
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
                        FieldInfo info = objType.GetFields().Where(f => f.Name == name).FirstOrDefault();
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
}

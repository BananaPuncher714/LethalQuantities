using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalQuantities.Json
{

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

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalQuantities.Json
{
    // Quick easy to edit hardcoded ad-hoc json solution for now

    public class Optional<T>
    {
        public bool contains;
        public T value;

        public Optional()
        {
            contains = false;
        }

        public Optional(T value)
        {
            this.value = value;
            contains = true;
        }

        public bool Set(ref T variable)
        {
            if (contains)
            {
                variable = value;
            }
            return contains;
        }

        public static Optional<T> Empty()
        {
            return new Optional<T>();
        }
    }

    public class ItemOptions
    {
        public Optional<int> rarity;
    }

    public class EnemyTypeOptions
    {
        public Optional<int> rarity;
        public Optional<int> maxCount;
        public Optional<int> powerLevel;
        public Optional<AnimationCurve> spawnCurve;
        public Optional<float> stunTimeMultiplier;
        public Optional<float> doorSpeedMultiplier;
        public Optional<float> stunGameDifficultyMultiplier;
        public Optional<bool> stunnable;
        public Optional<bool> killable;
        public Optional<bool> health;
    }

    public class DungeonFlowOptions
    {
        public Optional<float> factorySizeMultiplier;
    }

    public class TrapOptions
    {
        public Optional<AnimationCurve> spawnCurve;
    }

    public class PriceOptions
    {
        public Optional<int> price;
    }

    public class Preset : FlowPreset
    {
        public Dictionary<string, DungeonFlowOptions> dungeonOptions { get; set; } = new Dictionary<string, DungeonFlowOptions>();
        public Dictionary<string, PriceOptions> priceOptions { get; set; } = new Dictionary<string, PriceOptions>();
    }

    public class FlowPreset
    {
        public Dictionary<string, ItemOptions> itemOptions { get; set; } = new Dictionary<string, ItemOptions>();
        public Dictionary<string, EnemyTypeOptions> enemyOptions { get; set; } = new Dictionary<string, EnemyTypeOptions>();
        public Dictionary<string, EnemyTypeOptions> daytimeEnemyOptions { get; set; } = new Dictionary<string, EnemyTypeOptions>();
        public Dictionary<string, EnemyTypeOptions> outsideEnemyOptions { get; set; } = new Dictionary<string, EnemyTypeOptions>();
        public Dictionary<string, TrapOptions> trapOptions { get; set; } = new Dictionary<string, TrapOptions>();

    }
}

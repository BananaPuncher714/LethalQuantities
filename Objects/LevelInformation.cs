using System;
using System.Collections.Generic;
using System.Text;

namespace LethalQuantities.Objects
{
    public class LevelInformation
    {
        public HashSet<EnemyType> allEnemyTypes { get; } = new HashSet<EnemyType>();
        public HashSet<Item> allItems { get; } = new HashSet<Item>();
    }
}

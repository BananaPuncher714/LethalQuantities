# Lethal Quantities
A per-moon enemy spawning customization mod. Please request features or report issues [here](https://github.com/BananaPuncher714/LethalQuantities/issues)
## Features
- Provides optional(disabled by default) settings to enhance your Lethal Company experience
- Some more control over how many enemies spawn, and on what moons
- Works with custom levels
- Works with custom enemies
- Should work with custom events and other mods that change enemy spawn settings
## Bugs
- May or may not play nicely with other mods that forcefully change spawning behavior
## Configuration
You must host or join a game at least once to generate the configuration files. Any missing or deleted files will be generated with the default options.
Complex config types:
- [`AnimationCurve`](https://docs.unity3d.com/Manual/animeditor-AnimationCurves.html) - Represents a curve comprised of Keyframes. Each frame represents a key, and a value at that key(generally time).
  - A curve with no frames is left blank
  - A curve with one keyframe is described with a single number
  - A curve with multiple keyframes is comprised of a keyframes(`key:value`) separated by commas
### Enemies
There are 3 different enemy configuration files:
- `Enemies.cfg` - Responsible for all enemies that spawn inside.
- `DaytimeEnemies.cfg` - Responsible for enemies that can spawn outside, but normally passive ones.
- `OutsideEnemies.cfg` - Responsible for all enemies that can spawn outside, but normally hostile ones.


These configuration files do _not_ interfere with each other, meaning enemies spawned based on one config will not count towards settings from another config(such as MaxEnemyCount).


**Options**
- General
  - `Enabled` - Allow/Disallow this config to modify enemy spawning. You **must** enable this option to change enemy spawning behavior.
  - `MaxPowerCount` - Maximum total power allowed for this category of enemies. Different enemy types have different power levels. The total power of a level is the sum of the power levels of all existing enemies.
  - `SpawnAmountCurve` - An AnimationCurve from 0 to 1. The key represents the percentage of time progressed in the current level. The value is the amount of enemies to spawn at the given time.
  - `SpawnAmountRange` - The range of enemies that can spawn. A value of 3 means that 3 more or 3 less enemies can spawn, based on the value returned by the `SpawnAmountCurve`.
- EnemyType - There is one section for each enemy. Invalid enemy types are ignored.
  - `Rarity` - The weight given to this enemy vs other enemies
  - `MaxEnemyCount` - The total amount of enemies of the given type that can spawn
  - `PowerLevel` - How much power an enemy of the given type counts for
  - `SpawnCurve` - An AnimationCurve from 0 to 1. The key represents the percentage of time progressed, much like `SpawnChanceCurve`. The value normally ranges from 0 to 1, and is multiplied by `Rarity` to find the weight.
  - `SpawnFalloffCurve` - An AnimationCurve describing how many enemies already exist, and the multiplier to use when determining the value of `SpawnChanceCurve`
  - `UseSpawnFalloff` - If true, then the resulting value from the `SpawnFalloffCurve` will be multiplied with the value from `SpawnCurve`


Exceptions:
- The `OutsideEnemies.cfg` option `SpawnChanceRange` has a hardcoded value of `3` in-game, and cannot be changed
### Scrap
There is 1 scrap configuration file.
- `Scrap.cfg` -  responsible for all scrap generation


This configuration file can be set per moon.

**Options**
- General
  - `Enabled` - Allow/Disallow this config to modify scrap spawning. You **must** enable this option to change scrap spawning behavior.
  - `MaxScrapCount` - Maximum total number of scrap items generated in a level. 
  - `MinScrapCount` - Minimum total number of scrap items generated in a level. 
  - `MaxTotalScrapValue` - The maximum total value for all scrap generated in a level.
  - `MinTotalScrapValue` - The minimum total value for all scrap generated in a level.
- ItemType - There is one section for each item.
  - `Rarity` - The weight of this item, relative to the total weight of all items.
 

## Spawn Logic
The enemy spawn logic in **Lethal Company** is a bit complex, and there are many additional variables that I did not make configurable. It would require a fairly large recode, and it would be more likely to cause problems with other mods. That said, here are some basic things to keep in mind when configuring enemy spawning:
- The number of vents does not limit how many enemies can spawn indoors
- The AnimationCurves dependent on time are always from 0 to 1. This includes if the total amount of hours is changed by another mod, or if the day progresses slower/faster.
- The AnimationCurves are not a linear progression from one frame to another. It is modelled by Unity's AnimationCurve, and therefore probably a smooth line
- The total enemy power level will _not_ exceed but can equal the maximum power count
- There is a minimum amount of enemies that will spawn if certain conditions are met, such as an eclipse
- No more than 20 enemies can spawn at once
- The total amount of enemies that can spawn is calculated first; the enemy types to spawn are determined afterwards
- The chance of an enemy spawning is `weight of enemy type / total weight`, where the total weight is the sum of the weights of all enemy types
- The weight of an enemy type is `rarity * spawn chance at the current time` . If `UseSpawnFalloff` is enabled(like baboon hawks), then it is `rarity * spawn chance at the current time * spawn falloff multiplier at the current time`. Spawn falloff multiplier has a curve with a key that is the total amount of that mob / 10.
## Roadmap
- Additional settings
- Customizable spawning formulas
- YAML/JSON config files for more complex options
- Random events, maybe

## Changelog
### 1.1.5
- Fixed invalid global dungeon generation config preventing the level from loading
### 1.1.4
- Fixed invalid global scrap config
- Fixed incompatibilty with LethalLevelLoader configs by only adding dungeon flows if set in the dungeon generation config
- Added trap config for interior map objects(turrets, landmines, etc)
- Changed enemy and moon config names to be more user friendly(like Eyeless dogs instead of MouthDog, 220 Assurance instead of AssuranceLevel)
### 1.1.3
- Fixed stuck screen when loading into certain moons
- Fixed unknown scrap amount default value
- Fixed improperly named enemy types for compatibility with Skinwalkers
- Fixed EGypt and any other LethalLevelLoader moon incompatabilities
- Formatted the config options to be slightly more readable
### 1.1.2
- Added factory size multipler per dungeon flow
- Sort some entries alphabetically to make them easier to locate, such as rarities
### 1.1.1
- Fixed invalid scrap config not loading
- Added configurable dungeon flows per moon
### 1.1.0 - IF UPDATING TO THIS VERSION THEN BACKUP ALL YOUR CONFIG FILES!
- Create global config files to reduce clutter instead of individual files per every moon
- Added optional per-moon configs for user selected moons
- Added GLOBAL and DEFAULT options for certain options to fallback to the global config or default moon values
- Added item weight and conductivity to the scrap config
- Added enemy stun multiplier, door speed multiplier, stun game difficulty multiplier, stunnable, killable, and enemy hp to the enemy configs
### 1.0.14
- Fixed numbers from being saved with commas instead of periods
### 1.0.13
- Fixed enemies being hidden from other mods(SpectateEnemies)
- Added store items to spawnable items
### 1.0.12
- Fixed re-loading saves causing errors with custom moons
### 1.0.11
- Fixed non interactive floating spider and other random enemies that may have appeared in strange locations
- Changed config saving to load into the level a little faster
- Updated configs with a QOL description including the user friendly name of the item/enemy
### 1.0.10
- Updated config key naming convention to upper camel case
### 1.0.9
- Removed MaxTotalValue and MinTotalValue scrap config options, as they did nothing
- Added per item MaxValue and MinValue scrap config options
- Added scrap config options for changing ScrapAmountMultiplier and ScrapValueMultiplier
### 1.0.8
- Don't add enemies with a max enemy count of 0, even if the rarity is not 0
- Changed enemy hideflags on spawn to work with other mods
### 1.0.7
- Fixed configs not getting saved/loaded
### 1.0.6
- Fixed randomly floating enemies in the middle of nowhere
- Fixed compatibility with other mods like ScanForEnemies
### 1.0.5
- Changed all enemy configs to use enemy name, instead of only outside enemies
### v1.0.4
You should delete your configs if updating, since the config table names are different
- Use GameObject name instead of item/enemy name
### v1.0.3
- Added configurable per level scrap options
### v1.0.2
- Fixed an issue where multiple levels may share the same scene name
### v1.0.1
- Change RoundManager patch to execute before LoadNewLevel with high priority to prevent overwriting other mods' events
- Changed moon name to use the name rather than scene name
- Fixed enemy types with a rarity of 0 from being added to the level as a potential spawn
### v1.0.0
- Initial release

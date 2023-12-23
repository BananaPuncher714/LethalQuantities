## Changelog
### 1.0.9
- Removed maxTotalValue and minTotalValue config options, as they did nothing
- Added per item maxValue and minValue config options
- Added config options for changing scrapAmountMultiplier and scrapValueMultiplier
### 1.0.8
- Don't add enemies with a max enemy count of 0, even if the rarity is not 0
- Changed enemy hideflags on spawn to worth with other mods
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

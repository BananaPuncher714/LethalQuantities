using HarmonyLib;
using LethalQuantities.Json;
using LethalQuantities.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalQuantities.Patches
{
    internal class StartOfRoundPatch
    {
        private static Dictionary<int, int> defaultPrices = new Dictionary<int, int>();

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ChangeLevel))]
        [HarmonyPriority(200)]
        [HarmonyPrefix]
        private static void onPlanetChange(StartOfRound __instance, int levelID)
        {
            updateMoonPrices(__instance.levels[levelID]);
        }

        public static void updateMoonPrices(SelectableLevel level)
        {
            try {
                Terminal terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
                TerminalKeyword routeWord = terminal.terminalNodes.allKeywords.First(w => w.word != null && w.word.ToLower() == "route");

                List<PriceConfiguration> priceConfigurations = new List<PriceConfiguration>();
                GlobalConfiguration configuration = Plugin.INSTANCE.configuration;
                if (configuration != null && configuration.priceConfiguration.enabled.Value)
                {
                    priceConfigurations.Add(configuration.priceConfiguration);
                    foreach (LevelConfiguration config in configuration.levelConfigs.Values)
                    {
                        if (config.price.enabled.Value)
                        {
                            priceConfigurations.Add(config.price);
                        }
                    }
                }

                LevelPreset preset = Plugin.INSTANCE.presets[level.getGuid()];
                bool wasDefault = defaultPrices.Count() == 0;
                if (preset.price.isSet())
                {
                    bool updatedConfigs = false;
                    bool updatedDefaultInfo = false;
                    Plugin.LETHAL_LOGGER.LogInfo("Modifying moon prices");
                    foreach (CompatibleNoun noun in routeWord.compatibleNouns)
                    {
                        TerminalNode result = noun.result;
                        if (result.terminalOptions == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Route subcommand {result.name} does not have any valid terminal options!");
                            continue;
                        }

                        CompatibleNoun confirmNoun = result.terminalOptions.First(n => n.noun.word.ToLower() == "confirm");
                        if (confirmNoun == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Unable to find a confirm option for route command {result.name}");
                            continue;
                        }
                        TerminalNode confirm = confirmNoun.result;

                        if (confirm == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Found a confirm option for route command {result.name}, but it has no result node!");
                            continue;
                        }
                        int levelId = confirm.buyRerouteToMoon;
                        if (StartOfRound.Instance.getLevelById(levelId, out SelectableLevel matched))
                        {
                            Guid matchedGuid = matched.getGuid();
                            if (wasDefault)
                            {
                                if (!defaultPrices.TryAdd(levelId, result.itemCost))
                                {
                                    Plugin.LETHAL_LOGGER.LogError($"Already changed price for TerminalNode {result.name} with level id {levelId}. Perhaps another mod has added it in twice??");
                                }
                            }

                            if (priceConfigurations.Count > 0)
                            {
                                bool didMoonPriceUpdate = false;
                                foreach (PriceConfiguration config in priceConfigurations)
                                {
                                    if (!config.moons.TryGetValue(matchedGuid, out MoonPriceConfiguration c))
                                    {
                                        Plugin.LETHAL_LOGGER.LogError("Unable to find a price config option for " + matched.name);
                                    }
                                    else
                                    {
                                        if (config.moons[matchedGuid].price.DefaultValue() == -1)
                                        {
                                            updatedConfigs = didMoonPriceUpdate = true;
                                            config.file.SaveOnConfigSet = false;
                                            config.moons[matchedGuid].price.setDefaultValue(result.itemCost);
                                        }
                                    }

                                }

                                if (didMoonPriceUpdate)
                                {
                                    Plugin.LETHAL_LOGGER.LogInfo($"Updated price configs with a new default price for level {matched.name}");
                                }
                            }
                            else
                            {
                                Plugin.LETHAL_LOGGER.LogError("Expected at least 1 enabled PriceConfiguration object, got none. Was this PriceConfiguration object loaded properly?");
                            }

                            if (preset.price.value.TryGetValue(matchedGuid, out LevelPresetPrice pricePreset))
                            {
                                int price = defaultPrices.GetValueOrDefault(levelId, result.itemCost);
                                pricePreset.price.update(ref price);

                                result.itemCost = price;
                                confirm.itemCost = price;
                            }

                            updatedDefaultInfo |= Plugin.INSTANCE.defaultInformation.updatePrice(matched, result.itemCost);
                        }
                        else
                        {
                            Plugin.LETHAL_LOGGER.LogWarning($"Unable to find moon for level {levelId} on CompatibleNoun {result.name}");
                        }
                    }

                    if (updatedConfigs)
                    {
                        foreach (PriceConfiguration config in priceConfigurations)
                        {
                            config.file.Save();
                            config.file.SaveOnConfigSet = true;
                        }
                    }

                    if (updatedDefaultInfo)
                    {
                        Plugin.INSTANCE.exportData();
                    }
                }
                else if (!wasDefault)
                {
                    // Reset the moons to their vanilla values, for this level
                    Plugin.LETHAL_LOGGER.LogInfo("Resetting moon prices back to the original values");
                    foreach (CompatibleNoun noun in routeWord.compatibleNouns)
                    {
                        TerminalNode result = noun.result;
                        if (result.terminalOptions == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Route subcommand {result.name} does not have any valid terminal options!");
                            continue;
                        }

                        CompatibleNoun confirmNoun = result.terminalOptions.First(n => n.noun.word.ToLower() == "confirm");
                        if (confirmNoun == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Unable to find a confirm option for route command {result.name}");
                            continue;
                        }
                        TerminalNode confirm = confirmNoun.result;

                        if (confirm == null)
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Found a confirm option for route command {result.name}, but it has no result node!");
                            continue;
                        }
                        int levelId = confirm.buyRerouteToMoon;

                        int price = defaultPrices.GetValueOrDefault(levelId, result.itemCost);

                        result.itemCost = price;
                        confirm.itemCost = price;
                    }
                    defaultPrices.Clear();
                }
            }
            catch (Exception e)
            {
                Plugin.LETHAL_LOGGER.LogError("Encountered an error while trying to update the moon prices");
                Plugin.LETHAL_LOGGER.LogError($"Please report this error to the mod developers: {e}");
            }
        }
    }

    public static class StartOfRoundExtension
    {
        public static bool getLevelById(this StartOfRound round, int id, out SelectableLevel level)
        {
            foreach (SelectableLevel moon in round.levels)
            {
                if (moon.levelID == id)
                {
                    level = moon;
                    return true;
                }
            }
            level = null;
            return false;
        }
    }
}

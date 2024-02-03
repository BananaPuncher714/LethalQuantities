﻿using HarmonyLib;
using LethalQuantities.Objects;
using System.Collections.Generic;
using System.Linq;

namespace LethalQuantities.Patches
{
    internal class StartOfRoundPatch
    {
        private static Dictionary<int, int> defaultPrices = new Dictionary<int, int>();

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ChangePlanet))]
        [HarmonyPriority(200)]
        [HarmonyPrefix]
        private static void onPlanetChange(StartOfRound __instance)
        {
            updateMoonPrices(__instance.currentLevel);
        }

        public static void updateMoonPrices(SelectableLevel level)
        {
            GlobalConfiguration configuration = Plugin.INSTANCE.configuration;
            Terminal terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
            TerminalKeyword routeWord = terminal.terminalNodes.allKeywords.First(w => w.word != null && w.word.ToLower() == "route");

            PriceConfiguration priceConfig = Plugin.INSTANCE.configuration.priceConfiguration;
            if (Plugin.INSTANCE.configuration.levelConfigs.TryGetValue(level.name, out LevelConfiguration localConfig))
            {
                if (localConfig.price.enabled.Value)
                {
                    priceConfig = localConfig.price;
                }
            }
            else
            {
                Plugin.LETHAL_LOGGER.LogError($"Did not find local config for ${level.name}({level.PlanetName}). Did the configuration files generate properly?");
                return;
            }

            List<PriceConfiguration> priceConfigurations = new List<PriceConfiguration>();
            if (configuration.priceConfiguration.enabled.Value)
            {
                priceConfigurations.Add(configuration.priceConfiguration);
            }

            foreach (LevelConfiguration config in configuration.levelConfigs.Values)
            {
                if (config.price.enabled.Value)
                {
                    priceConfigurations.Add(config.price);
                }
            }


            bool wasDefault = defaultPrices.Count() == 0;
            if (priceConfig.enabled.Value)
            {
                bool updatedConfigs = false;
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

                    if (wasDefault)
                    {
                        if (!defaultPrices.TryAdd(levelId, result.itemCost))
                        {
                            Plugin.LETHAL_LOGGER.LogError($"Already changed price for TerminalNode {result.name} with level id {levelId}. Perhaps another mod has added it in twice??");
                        }
                    }

                    if (StartOfRound.Instance.getLevelById(levelId, out SelectableLevel matched))
                    {
                        if (priceConfigurations.Count > 0)
                        {
                            bool didMoonPriceUpdate = false;
                            foreach (PriceConfiguration config in priceConfigurations)
                            {
                                if (config.moons[matched.name].price.DefaultValue() == -1)
                                {
                                    updatedConfigs = didMoonPriceUpdate = true;
                                    config.file.SaveOnConfigSet = false;
                                    config.moons[matched.name].price.setDefaultValue(result.itemCost);
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

                        if (priceConfig.moons.TryGetValue(matched.name, out MoonPriceConfiguration moonConfig))
                        {
                            int price = defaultPrices.GetValueOrDefault(levelId, result.itemCost);
                            moonConfig.price.Set(ref price);

                            result.itemCost = price;
                            confirm.itemCost = price;
                        }
                    }
                    else
                    {
                        Plugin.LETHAL_LOGGER.LogWarning($"Unable to find moon for level {levelId}");
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

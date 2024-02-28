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
            try
            {
                Terminal terminal = UnityEngine.Object.FindFirstObjectByType<Terminal>();
                TerminalKeyword routeWord = terminal.terminalNodes.allKeywords.First(w => w.name == "Route");

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

                bool wasDefault = defaultPrices.Count() == 0;
                if (Plugin.INSTANCE.presets.TryGetValue(level.getGuid(), out LevelPreset preset) && preset.price.isSet())
                {
                    bool updatedConfigs = false;
                    bool updatedDefaultInfo = false;
                    MiniLogger.LogInfo("Modifying moon prices");
                    foreach (CompatibleNoun noun in routeWord.compatibleNouns)
                    {
                        TerminalNode result = noun.result;
                        if (result.terminalOptions == null)
                        {
                            MiniLogger.LogError($"Route subcommand {result.name} does not have any valid terminal options!");
                            continue;
                        }

                        CompatibleNoun confirmNoun = result.terminalOptions.First(n => n.noun.name == "Confirm");
                        if (confirmNoun == null)
                        {
                            MiniLogger.LogError($"Unable to find a confirm option for route command {result.name}");
                            continue;
                        }
                        TerminalNode confirm = confirmNoun.result;

                        if (confirm == null)
                        {
                            MiniLogger.LogError($"Found a confirm option for route command {result.name}, but it has no result node!");
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
                                    MiniLogger.LogError($"Already changed price for TerminalNode {result.name} with level id {levelId}. Perhaps another mod has added it in twice??");
                                }
                            }

                            if (priceConfigurations.Count > 0)
                            {
                                bool didMoonPriceUpdate = false;
                                foreach (PriceConfiguration config in priceConfigurations)
                                {
                                    if (!config.moons.TryGetValue(matchedGuid, out MoonPriceConfiguration c))
                                    {
                                        MiniLogger.LogError("Unable to find a price config option for " + matched.name);
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
                                    MiniLogger.LogInfo($"Updated price configs with a new default price for level {matched.name}");
                                }
                            }
                            else
                            {
                                MiniLogger.LogError("Expected at least 1 enabled PriceConfiguration object, got none. Was this PriceConfiguration object loaded properly?");
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
                            MiniLogger.LogWarning($"Unable to find moon for level {levelId} on CompatibleNoun {result.name}");
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
                    MiniLogger.LogInfo("Resetting moon prices back to the original values");
                    foreach (CompatibleNoun noun in routeWord.compatibleNouns)
                    {
                        TerminalNode result = noun.result;
                        if (result.terminalOptions == null)
                        {
                            MiniLogger.LogError($"Route subcommand {result.name} does not have any valid terminal options!");
                            continue;
                        }

                        CompatibleNoun confirmNoun = result.terminalOptions.First(n => n.noun.name == "Confirm");
                        if (confirmNoun == null)
                        {
                            MiniLogger.LogError($"Unable to find a confirm option for route command {result.name}");
                            continue;
                        }
                        TerminalNode confirm = confirmNoun.result;

                        if (confirm == null)
                        {
                            MiniLogger.LogError($"Found a confirm option for route command {result.name}, but it has no result node!");
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
                MiniLogger.LogError("Encountered an error while trying to update the moon prices");
                MiniLogger.LogError($"Please report this error to the mod developers: {e}");
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

using HarmonyLib;
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
            } else
            {
                Plugin.LETHAL_LOGGER.LogError($"Did not find local config for ${level.name}({level.PlanetName}). Did the configuration files generate properly?");
                return;
            }

            bool wasDefault = defaultPrices.Count() == 0;
            if (priceConfig.enabled.Value)
            {
                Plugin.LETHAL_LOGGER.LogInfo("Modifying moon prices");
                foreach (CompatibleNoun noun in routeWord.compatibleNouns)
                {
                    TerminalNode result = noun.result;
                    TerminalNode confirm = result.terminalOptions.First(n => n.noun.word.ToLower() == "confirm").result;
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
            }
            else if (!wasDefault)
            {
                // Reset the moons to their vanilla values, for this level
                Plugin.LETHAL_LOGGER.LogInfo("Resetting moon prices back to the original values");
                foreach (CompatibleNoun noun in routeWord.compatibleNouns)
                {
                    TerminalNode result = noun.result;
                    TerminalNode confirm = result.terminalOptions.First(n => n.noun.word.ToLower() == "confirm").result;
                    int levelId = confirm.buyRerouteToMoon;

                    int price = defaultPrices.GetValueOrDefault(levelId, result.itemCost);

                    result.itemCost = price;
                    confirm.itemCost = price;
                }
                defaultPrices.Clear();
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

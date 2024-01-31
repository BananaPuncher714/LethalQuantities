using HarmonyLib;

namespace LethalQuantities.Patches
{
    internal class TerminalPatch
    {
        [HarmonyPatch(typeof(Terminal),"Start")]
        [HarmonyPrefix]
        private static void onTerminalStartPrefix(Terminal __instance)
        {
            StartOfRoundPatch.updateMoonPrices(StartOfRound.Instance.currentLevel);
        }
    }
}

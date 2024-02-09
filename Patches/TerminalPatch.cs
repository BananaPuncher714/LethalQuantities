using HarmonyLib;

namespace LethalQuantities.Patches
{
    internal class TerminalPatch
    {
        [HarmonyPatch(typeof(Terminal), "Start")]
        [HarmonyPriority(700)]
        [HarmonyPostfix]
        private static void onTerminalStartPrefix(Terminal __instance)
        {
            if (RoundManager.Instance.IsServer)
            {
                StartOfRoundPatch.updateMoonPrices(StartOfRound.Instance.currentLevel);
            }
        }
    }
}

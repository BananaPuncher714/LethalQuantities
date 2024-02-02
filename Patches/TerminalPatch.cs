using HarmonyLib;

namespace LethalQuantities.Patches
{
    internal class TerminalPatch
    {
        [HarmonyPatch(typeof(Terminal),"Start")]
        [HarmonyPrefix]
        private static void onTerminalStartPrefix(Terminal __instance)
        {
            if (RoundManager.Instance.IsServer)
            {
                StartOfRoundPatch.updateMoonPrices(StartOfRound.Instance.currentLevel);
            }
        }
    }
}

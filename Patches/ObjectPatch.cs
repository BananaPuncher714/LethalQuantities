using HarmonyLib;
using LethalQuantities.Objects;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class ObjectPatch
    {
        [HarmonyPatch(typeof(Object), nameof(Object.Instantiate), [typeof(GameObject), typeof(Vector3), typeof(Quaternion)])]
        // This patch really should run first, since it's attempting to restore vanilla behavior(that is, unhide objects that shouldn't be hidden and setting them active)
        [HarmonyPriority(50)]
        [HarmonyPrefix]
        static void onInstantiatePrefix(GameObject original)
        {
            RoundState state = getRoundState();
            if (state != null)
            {
                if (state.modifiedEnemyTypes.Contains(original) && !original.activeSelf)
                {
                    original.hideFlags = HideFlags.None;
                    original.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(Object), nameof(Object.Instantiate), [typeof(GameObject), typeof(Vector3), typeof(Quaternion)])]
        [HarmonyPriority(Priority.First)]
        [HarmonyPostfix]
        static void onInstantiatePostfix(GameObject original)
        {
            RoundState state = getRoundState();
            if (state != null)
            {
                if (state.modifiedEnemyTypes.Contains(original) && original.activeSelf)
                {
                    original.hideFlags = HideFlags.HideAndDontSave;
                    original.SetActive(false);
                }
            }
        }

        private static RoundState getRoundState()
        {
            GameObject obj = GameObject.Find("LevelModifier");
            if (obj != null)
            {
                return obj.GetComponent<RoundState>();
            }
            return null;
        }
    }
}

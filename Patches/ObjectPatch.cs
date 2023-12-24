using HarmonyLib;
using LethalQuantities.Objects;
using UnityEngine;

namespace LethalQuantities.Patches
{
    internal class ObjectPatch
    {
        [HarmonyPatch(typeof(Object), nameof(Object.Instantiate), [typeof(GameObject), typeof(Vector3), typeof(Quaternion)])]
        [HarmonyPriority(Priority.First)]
        [HarmonyPrefix]
        static void onInstantiatePrefix(GameObject original)
        {
            RoundState state = getRoundState();
            if (state != null)
            {
                if (state.modifiedEnemyTypes.Contains(original) && !original.activeSelf)
                {
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

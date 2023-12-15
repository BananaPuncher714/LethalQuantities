using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LethalQuantities.Objects
{
    public class RoundState : MonoBehaviour
    {
        public Plugin plugin { get; internal set; }
        public Scene scene { get; internal set; }
        public LevelConfiguration levelConfiguration { get; set; }

        public List<SpawnableEnemyWithRarity> enemies { get; } = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableEnemyWithRarity> daytimeEnemies { get; } = new List<SpawnableEnemyWithRarity>();
        public List<SpawnableEnemyWithRarity> outsideEnemies { get; } = new List<SpawnableEnemyWithRarity>();

        private List<GameObject> instantiatedEnemies = new List<GameObject>();

        public void initialize()
        {
            Plugin.LETHAL_LOGGER.LogInfo("Generating spawnable enemy options");
            if (levelConfiguration.enemies.enabled.Value)
            {
                populate(enemies, levelConfiguration.enemies.enemyTypes);
            }
            if (levelConfiguration.daytimeEnemies.enabled.Value)
            {
                populate(daytimeEnemies, levelConfiguration.daytimeEnemies.enemyTypes, true, true);
            }
            if (levelConfiguration.outsideEnemies.enabled.Value)
            {
                populate(outsideEnemies, levelConfiguration.outsideEnemies.enemyTypes, true);
            }
        }

        void OnDestroy()
        {
            foreach (GameObject enemy in instantiatedEnemies)
            {
                Destroy(enemy);
            }
        }

        private void populate(List<SpawnableEnemyWithRarity> enemiesList, List<EnemyTypeConfiguration> configs, bool isOutside = false, bool isDaytimeEnemy = false)
        {
            foreach (var item in configs)
            {
                int rarity = item.rarity.Value;
                if (rarity > 0)
                {
                    EnemyType type = Instantiate(item.type);
                    type.MaxCount = item.maxEnemyCount.Value;
                    type.PowerLevel = item.powerLevel.Value;
                    type.probabilityCurve = item.spawnCurve.Value;
                    type.numberSpawnedFalloff = item.spawnFalloffCurve.Value;
                    type.useNumberSpawnedFalloff = item.useSpawnFalloff.Value;
                    type.isOutsideEnemy = isOutside;
                    type.isDaytimeEnemy = isDaytimeEnemy;

                    instantiateEnemyTypeObject(type);

                    SpawnableEnemyWithRarity spawnable = new SpawnableEnemyWithRarity();
                    spawnable.enemyType = type;
                    spawnable.rarity = rarity;
                    enemiesList.Add(spawnable);
                }
            }
        }

        private void instantiateEnemyTypeObject(EnemyType type)
        {
            GameObject obj = type.enemyPrefab;
            bool isActive = obj.activeSelf;
            obj.SetActive(false);

            GameObject copy = Instantiate(obj, new Vector3(-1000000, -1000000, -1000000), Quaternion.identity);
            SceneManager.MoveGameObjectToScene(copy, SceneManager.GetSceneByName("SampleSceneRelay"));
            instantiatedEnemies.Add(copy);
            type.enemyPrefab = copy;
            copy.GetComponent<EnemyAI>().enemyType = type;
            obj.SetActive(isActive);
            copy.SetActive(isActive);
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Mkey
{
    [CreateAssetMenu(menuName = "Mkey/LevelConstructSet")]
    public class LevelConstructSet : BaseScriptable
    {
        public int levelNumber; // Номер уровня
        public List<EnemyWave> enemyWaves; // Список волн врагов

        [Serializable]
        public struct EnemyWave
        {
            public List<EnemyPrefabData> enemyPrefabs; // Массив префабов врагов для волны
            public float spawnDelay; // Задержка между спавном врагов (сек)
        }

        [Serializable]
        public struct EnemyPrefabData
        {
            public GameObject prefab; // Префаб врага
            public int count; // Количество врагов этого типа
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace Mkey
{
	public class EnemySpawner : MonoBehaviour
	{
		// Публичные переменные в инспекторе
		public int startWait; // Начальная задержка перед первой волной (сек)
		public int waveWait; // Задержка между волнами (сек)
		public bool randomSpawnPositions; // Случайные точки спавна

		// Приватные переменные
		private GameObject enemyParent; // Родительский объект для врагов
		private GameObject[] spawnObjects; // Точки спавна
		private int currentSpawnPosition; // Текущая точка спавна
		private List<Mkey.LevelConstructSet.EnemyWave> enemyWaves; // Волны врагов из LevelConstructSet

		void Awake()
		{
			enemyParent = new GameObject("Enemies"); // Создаёт родителя для врагов
			spawnObjects = GameObject.FindGameObjectsWithTag("Enemy spawn position"); // Находит точки спавна
		}

		void Start()
		{
			currentSpawnPosition = 0; // Устанавливает первую точку спавна
		}

		// Метод для установки данных уровня
		public void SetLevelData(List<Mkey.LevelConstructSet.EnemyWave> waves)
		{
			enemyWaves = waves; // Сохраняет волны
			StopAllCoroutines(); // Останавливает текущий спавн
			StartCoroutine(SpawnWaves()); // Запускает новый спавн
		}

		IEnumerator SpawnWaves()
		{
			yield return new WaitForSeconds(startWait); // Ждёт начальную задержку
			foreach (var wave in enemyWaves) // Для каждой волны
			{
				foreach (var enemyData in wave.enemyPrefabs) // Для каждого типа врага
				{
					for (int i = 0; i < enemyData.count; i++) // Для каждого врага
					{
						Instantiate(enemyData.prefab, spawnObjects[currentSpawnPosition].transform.position, spawnObjects[currentSpawnPosition].transform.rotation, enemyParent.transform); // Спавнит врага
						yield return new WaitForSeconds(wave.spawnDelay); // Ждёт задержку
					}
				}
				if (!randomSpawnPositions) // Если точки не случайные
				{
					currentSpawnPosition = (currentSpawnPosition + 1) % spawnObjects.Length; // Переключает точку спавна
				}
				else // Если случайные
				{
					currentSpawnPosition = Random.Range(0, spawnObjects.Length); // Выбирает случайную точку
				}
				yield return new WaitForSeconds(waveWait); // Ждёт между волнами
			}
		}
	}
}

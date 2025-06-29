using UnityEngine;
using System.Collections;
using UnityEngine.AI;

namespace Mkey
{
    public class Enemy : MonoBehaviour
    {
        // Данные врага, видимые в инспекторе
        public float lives; // Здоровье
        public float damage; // Урон атаки
        public float minAttackDistance; // Дальность атаки
        public float castleStoppingDistance; // Дистанция остановки перед замком
        public int addGold; // Золото за смерть
        public string attackTag; // Тег цели (например, "Knight")
        public string attackCastleTag; // Тег замка (например, "Player Castle")
        public ParticleSystem dieParticles; // Частицы при смерти
        public GameObject ragdoll; // Рэгдолл при смерти
        public AudioClip attackAudio; // Звук атаки
        public AudioClip runAudio; // Звук бега

        // Для волшебников
        public bool wizard; // Флаг: враг — волшебник
        public ParticleSystem spawnEffect; // Эффект спавна скелетов
        public GameObject skeleton; // Префаб скелета
        public int skeletonAmount; // Количество скелетов
        public float newSkeletonsWaitTime; // Задержка между спавнами

        // Приватные переменные
        private NavMeshAgent agent; // Компонент навигации
        private Transform currentTarget; // Текущая цель (войско или замок)
        private Vector3 castleAttackPosition; // Позиция для атаки замка
        private GameObject castle; // Ближайший замок
        private Animator[] animators; // Аниматоры врага
        private AudioSource source; // Компонент звука
        private bool wizardSpawns; // Флаг: волшебник спавнит скелетов

        void Start()
        {
            source = GetComponent<AudioSource>(); // Находит AudioSource
            agent = GetComponent<NavMeshAgent>(); // Находит NavMeshAgent
            animators = GetComponentsInChildren<Animator>(); // Находит аниматоры
            findClosestCastle(); // Находит замок
            if (wizard) // Если волшебник
                StartCoroutine(SpawnSkeletons()); // Запускает спавн скелетов
        }

        void Update()
        {
            if (lives < 1) // Если здоровье < 1
                StartCoroutine(Die()); // Запускает смерть

            if (castle == null) // Если замок не найден
                findClosestCastle(); // Ищет замок

            if (currentTarget == null) // Если нет цели
                findCurrentTarget(); // Ищет цель

            if (currentTarget != null && Vector3.Distance(transform.position, currentTarget.position) < minAttackDistance) // Если цель в радиусе
            {
                if (!wizardSpawns) // Если не спавнит скелеты
                    agent.isStopped = false; // Включает движение
                agent.destination = currentTarget.position; // Устанавливает цель
                if (Vector3.Distance(currentTarget.position, transform.position) <= agent.stoppingDistance) // Если достиг цели
                {
                    Vector3 targetPos = currentTarget.position; // Получает позицию цели
                    targetPos.y = transform.position.y; // Выравнивает по Y
                    transform.LookAt(targetPos); // Поворачивается к цели
                    foreach (Animator animator in animators) // Для каждого аниматора
                        animator.SetBool("Attacking", true); // Включает атаку
                    if (source.clip != attackAudio) // Если не звук атаки
                    {
                        source.clip = attackAudio; // Устанавливает звук
                        source.Play(); // Воспроизводит
                    }
                    currentTarget.GetComponent<PlayerHero>().Lives -= Time.deltaTime * damage; // Наносит урон
                }
                if (animators[0].GetBool("Attacking") && Vector3.Distance(currentTarget.position, transform.position) > agent.stoppingDistance && !wizardSpawns) // Если атакует, но далеко
                {
                    foreach (Animator animator in animators) // Для каждого аниматора
                        animator.SetBool("Attacking", false); // Отключает атаку
                    if (source.clip != runAudio) // Если не звук бега
                    {
                        source.clip = runAudio; // Устанавливает звук
                        source.Play(); // Воспроизводит
                    }
                }
            }
            else // Если цель вне радиуса
            {
                if (!wizardSpawns) // Если не спавнит скелеты
                    agent.isStopped = false; // Включает движение
                agent.destination = castleAttackPosition; // Устанавливает замок
                if (castle != null && Vector3.Distance(transform.position, castleAttackPosition) <= castleStoppingDistance + castle.GetComponent<Castle>().size) // Если у замка
                {
                    agent.isStopped = true; // Останавливает движение
                    foreach (Animator animator in animators) // Для каждого аниматора
                        animator.SetBool("Attacking", true); // Включает атаку
                    if (castle != null) // Если замок есть
                        castle.GetComponent<Castle>().lives -= Time.deltaTime * damage; // Наносит урон
                    if (source.clip != attackAudio) // Если не звук атаки
                    {
                        source.clip = attackAudio; // Устанавливает звук
                        source.Play(); // Воспроизводит
                    }
                }
                else if (!wizardSpawns) // Если не у замка
                {
                    agent.isStopped = false; // Включает движение
                    foreach (Animator animator in animators) // Для каждого аниматора
                        animator.SetBool("Attacking", false); // Отключает атаку
                    if (source.clip != runAudio) // Если не звук бега
                    {
                        source.clip = runAudio; // Устанавливает звук
                        source.Play(); // Воспроизводит
                    }
                }
            }
        }

        private void findClosestCastle() // Находит ближайший замок
        {
            GameObject[] castles = GameObject.FindGameObjectsWithTag(attackCastleTag); // Находит замки
            float closestDistance = Mathf.Infinity; // Инициализирует расстояние
            foreach (GameObject potentialCastle in castles) // Для каждого замка
            {
                if (potentialCastle != null && Vector3.Distance(transform.position, potentialCastle.transform.position) < closestDistance) // Если ближе
                {
                    closestDistance = Vector3.Distance(transform.position, potentialCastle.transform.position); // Обновляет расстояние
                    castle = potentialCastle; // Сохраняет замок
                }
            }
            if (castle != null) // Если замок найден
                castleAttackPosition = castle.transform.position; // Устанавливает позицию
        }

        private void findCurrentTarget() // Находит ближайшую цель
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(attackTag); // Находит цели
            float closestDistance = Mathf.Infinity; // Инициализирует расстояние
            foreach (GameObject potentialTarget in targets) // Для каждой цели
            {
                if (potentialTarget != null && Vector3.Distance(transform.position, potentialTarget.transform.position) < closestDistance) // Если ближе
                {
                    closestDistance = Vector3.Distance(transform.position, potentialTarget.transform.position); // Обновляет расстояние
                    if (!currentTarget || Vector3.Distance(transform.position, currentTarget.position) > 2) // Если нет цели или далеко
                        currentTarget = potentialTarget.transform; // Сохраняет цель
                }
            }
        }

        private IEnumerator SpawnSkeletons() // Спавнит скелетов для волшебников
        {
            spawnEffect.Stop(); // Останавливает эффект
            while (true) // Бесконечный цикл
            {
                if (animators[0].GetBool("Attacking") == false && (currentTarget == null || Vector3.Distance(transform.position, currentTarget.position) >= minAttackDistance)) // Если не атакует
                {
                    wizardSpawns = true; // Устанавливает флаг спавна
                    agent.isStopped = true; // Останавливает движение
                    animators[0].SetBool("Spawning", true); // Включает анимацию
                    yield return new WaitForSeconds(0.5f); // Ждёт 0.5 сек
                    spawnEffect.Play(); // Включает эффект
                    for (int i = 0; i < skeletonAmount; i++) // Для каждого скелета
                    {
                        SpawnSkeleton(); // Спавнит скелета
                        yield return new WaitForSeconds(2f / skeletonAmount); // Ждёт задержку
                    }
                    spawnEffect.Stop(); // Останавливает эффект
                    yield return new WaitForSeconds(0.5f); // Ждёт 0.5 сек
                    wizardSpawns = false; // Отключает флаг
                    animators[0].SetBool("Spawning", false); // Отключает анимацию
                }
                yield return new WaitForSeconds(newSkeletonsWaitTime); // Ждёт задержку
            }
        }

        private void SpawnSkeleton() // Спавнит одного скелета
        {
            Vector3 position = new Vector3(transform.position.x + Random.Range(1f, 2f), transform.position.y, transform.position.z + Random.Range(-0.5f, 0.5f)); // Случайная позиция
            Instantiate(skeleton, position, Quaternion.identity); // Создаёт скелета
        }

        private IEnumerator Die() // Логика смерти
        {
            if (ragdoll == null) // Если нет рэгдолла
            {
                Vector3 position = new Vector3(transform.position.x, transform.position.y + 2, transform.position.z); // Позиция частиц
                ParticleSystem particles = Instantiate(dieParticles, position, transform.rotation); // Создаёт частицы
                if (GameObject.Find("Manager") != null) // Если есть Manager
                    particles.transform.parent = GameObject.Find("Manager").transform; // Устанавливает родителя
            }
            else // Если есть рэгдолл
            {
                Instantiate(ragdoll, transform.position, transform.rotation); // Создаёт рэгдолл
            }
            CharacterManager.gold += addGold; // Добавляет золото
            Manager.enemiesKilled++; // Увеличивает счётчик убитых
            foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) // Для всех врагов
            {
                if (enemy != this) // Пропускает себя
                    enemy.findCurrentTarget(); // Обновляет цель
            }
            yield return new WaitForEndOfFrame(); // Ждёт конец кадра
            Destroy(gameObject); // Уничтожает врага
        }
    }
}

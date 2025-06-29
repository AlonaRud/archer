using UnityEngine; // Подключает основные функции Unity
using System.Collections; // Подключает корутины для асинхронных действий

namespace Mkey // Пространство имён Mkey, как ты просила
{
    public abstract class PlayerHero : MonoBehaviour // Абстрактный класс для всех героев игрока
    {
        // Публичные переменные, видимые в инспекторе
        [SerializeField] protected float lives; // Здоровье героя
        [SerializeField] protected float damage; // Базовый урон атаки
        [SerializeField] protected float attackRange; // Радиус атаки героя
        [SerializeField] protected float attackDelay; // Задержка между атаками (сек)
        [SerializeField] protected AudioClip attackAudio; // Звук атаки
        [SerializeField] protected AudioClip idleAudio; // Звук ожидания
        [SerializeField] protected ParticleSystem dieParticles; // Эффект частиц при смерти

        // Приватные переменные
        private Transform currentTarget; // Текущая цель (враг)
        private Animator animator; // Компонент анимации
        private AudioSource audioSource; // Компонент звука
        private bool isAttacking; // Флаг: герой атакует
        private float lastAttackTime; // Время последней атаки

        // Свойства для доступа к данным
        public float Lives { get => lives; set => lives = value; } // Доступ к здоровью
        public bool IsAlive => lives > 0; // Проверка, жив ли герой

        void Start() // Вызывается при старте
        {
            animator = GetComponent<Animator>(); // Находит аниматор на объекте
            audioSource = GetComponent<AudioSource>(); // Находит AudioSource
            lastAttackTime = -attackDelay; // Сбрасывает таймер атаки
            PlayIdleAudio(); // Воспроизводит звук ожидания
        }

        void Update() // Вызывается каждый кадр
        {
            if (!IsAlive) // Если герой мёртв
            {
                StartCoroutine(Die()); // Запускает корутину смерти
                return; // Прерывает Update
            }

            if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.position) > attackRange) // Если нет цели или она вне радиуса
                currentTarget = FindTarget(); // Ищет новую цель

            if (currentTarget != null && Time.time >= lastAttackTime + attackDelay) // Если есть цель и прошла задержка
            {
                Attack(currentTarget); // Выполняет атаку
                lastAttackTime = Time.time; // Обновляет время атаки
            }
            else if (!isAttacking) // Если не атакует
            {
                SetIdleAnimation(); // Устанавливает анимацию ожидания
                PlayIdleAudio(); // Воспроизводит звук ожидания
            }
        }

        private Transform FindTarget() // Находит ближайшего врага
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Находит всех врагов по тегу
            Transform closestTarget = null; // Ближайшая цель
            float closestDistance = Mathf.Infinity; // Минимальное расстояние

            foreach (GameObject enemy in enemies) // Для каждого врага
            {
                if (enemy != null) // Если враг существует
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position); // Вычисляет расстояние
                    if (distance < closestDistance && distance <= attackRange) // Если враг ближе и в радиусе
                    {
                        closestDistance = distance; // Обновляет расстояние
                        closestTarget = enemy.transform; // Сохраняет цель
                    }
                }
            }
            return closestTarget; // Возвращает ближайшую цель или null
        }

        private void Attack(Transform target) // Выполняет атаку
        {
            isAttacking = true; // Устанавливает флаг атаки
            SetAttackAnimation(); // Включает анимацию атаки
            PlayAttackAudio(); // Воспроизводит звук атаки
            UseAbility(target); // Вызывает уникальную способность героя
        }

        protected abstract void UseAbility(Transform target); // Абстрактный метод для способности героя

        private void SetAttackAnimation() // Устанавливает анимацию атаки
        {
            if (animator != null) // Если аниматор существует
                animator.SetBool("Attacking", true); // Включает анимацию атаки
        }

        private void SetIdleAnimation() // Устанавливает анимацию ожидания
        {
            if (animator != null) // Если аниматор существует
            {
                animator.SetBool("Attacking", false); // Отключает анимацию атаки
                isAttacking = false; // Сбрасывает флаг атаки
            }
        }

        private void PlayAttackAudio() // Воспроизводит звук атаки
        {
            if (audioSource != null && attackAudio != null && audioSource.clip != attackAudio) // Если есть источник и звук атаки
            {
                audioSource.clip = attackAudio; // Устанавливает звук
                audioSource.Play(); // Воспроизводит
            }
        }

        private void PlayIdleAudio() // Воспроизводит звук ожидания
        {
            if (audioSource != null && idleAudio != null && audioSource.clip != idleAudio) // Если есть источник и звук ожидания
            {
                audioSource.clip = idleAudio; // Устанавливает звук
                audioSource.Play(); // Воспроизводит
            }
        }

        private IEnumerator Die() // Корутина для смерти героя
        {
            if (dieParticles != null) // Если есть эффект частиц
            {
                Vector3 position = new Vector3(transform.position.x, transform.position.y + 2, transform.position.z); // Позиция частиц
                ParticleSystem particles = Instantiate(dieParticles, position, transform.rotation); // Создаёт частицы
                if (GameObject.Find("Manager") != null) // Если есть Manager
                    particles.transform.parent = GameObject.Find("Manager").transform; // Устанавливает родителя
            }
            yield return new WaitForEndOfFrame(); // Ждёт конец кадра
            Destroy(gameObject); // Уничтожает героя
        }
    }
}

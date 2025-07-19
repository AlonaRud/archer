#if ENABLE_PLAYFABADMIN_API // Условная компиляция для PlayFab.
using CBS.Models; // Импорт для CBSTask.
using CBS.Utils; // Импорт для AchievementsTXTHandler.
using UnityEngine; // Импорт для Unity-компонентов.
using UnityEngine.UI; // Импорт для UI-компонентов.

namespace CBS.UI // Пространство имён для UI.
{
    public class TechInfo : BaseTaskUI<CBSTask> // Компонент для окна информации о задаче.
    {
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Text DescriptionText; // Текст для описания задачи.
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Text ProgressText; // Текст для прогресса (например, "5/10").
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Text LevelText; // Текст для уровня (например, "Уровень 1").
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Text StudyTimeText; // Текст для времени изучения.
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Slider ProgressSlider; // Слайдер для прогресса.
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Button ActionButton; // Кнопка для действий (изучение/награда).
        [SerializeField] // Позволяет редактировать в инспекторе.
        private Text ActionButtonText; // Текст на кнопке.

        private CBSAchievementsModule Achievements { get; set; } // Модуль ачивок.
        private CBSCurrencyModule Currency { get; set; } // Модуль валюты.
        private CBSProfileModule Profile { get; set; } // Модуль профиля.

        protected override string LockText => AchievementsTXTHandler.GetLockText(Task.LockLevel, Task.LevelFilter); // Текст для заблокированной задачи.
        protected override string NotCompleteText => AchievementsTXTHandler.NotCompleteText; // Текст для невыполненной задачи.
        protected override string CompleteText => AchievementsTXTHandler.CompleteText; // Текст для выполненной задачи.

        protected override void OnInit() // Инициализация окна.
        {
            base.OnInit(); // Базовая инициализация.
            Achievements = CBSModule.Get<CBSAchievementsModule>(); // Получает модуль ачивок.
            Currency = CBSModule.Get<CBSCurrencyModule>(); // Получает модуль валюты.
            Profile = CBSModule.Get<CBSProfileModule>(); // Получает модуль профиля.
            ActionButton.onClick.AddListener(OnActionButton); // Обработчик клика на кнопку.
        }

        private void OnDestroy() // Вызывается при уничтожении.
        {
            ActionButton.onClick.RemoveListener(OnActionButton); // Удаляет обработчик.
        }

        public override void Display(CBSTask task) // Отображает данные задачи.
        {
            base.Display(task); // Базовое отображение (например, название).
            DescriptionText.text = task.Description; // Устанавливает описание задачи.
            var tier = task.TierList != null && task.TierIndex < task.TierList.Count ? task.TierList[task.TierIndex] : null; // Получает текущий уровень.
            ProgressText.text = tier != null ? $"{tier.CurrentSteps}/{tier.StepsToComplete}" : "0/0"; // Показывает прогресс.
            LevelText.text = $"Уровень {task.TierIndex + 1}"; // Показывает уровень.
            if (tier != null && (tier.StudyHours > 0 || tier.StudyMinutes > 0)) // Если есть время изучения.
            {
                StudyTimeText.text = $"{tier.StudyHours}ч {tier.StudyMinutes}м"; // Показывает время.
            }
            else // Если времени нет.
            {
                StudyTimeText.text = "0ч 0м"; // Показывает нули.
            }
            if (tier != null) // Если уровень есть.
            {
                ProgressSlider.value = (float)tier.CurrentSteps / tier.StepsToComplete; // Устанавливает слайдер.
            }
            else // Если уровня нет.
            {
                ProgressSlider.value = 0; // Сбрасывает слайдер.
            }

            bool canStartStudy = false; // Флаг, можно ли начать изучение.
            if (tier != null && !task.IsActive && task.IsAvailable && tier.CurrentSteps < tier.StepsToComplete) // Если задача доступна.
            {
                if (tier.Cost > 0 && !string.IsNullOrEmpty(tier.CurrencyCode)) // Если есть стоимость.
                {
                    Currency.GetProfileCurrencies(Profile.ProfileID, currencyResult => // Запрашивает валюту.
                    {
                        if (currencyResult.IsSuccess) // Если запрос успешен.
                        {
                            var currency = currencyResult.Currencies.ContainsKey(tier.CurrencyCode) ? currencyResult.Currencies[tier.CurrencyCode] : null; // Получает валюту.
                            canStartStudy = currency != null && currency.Value >= tier.Cost; // Проверяет, хватает ли валюты.
                            UpdateButtonState(canStartStudy, task, tier); // Обновляет кнопку.
                        }
                        else // Если ошибка.
                        {
                            UpdateButtonState(false, task, tier); // Отключает кнопку.
                        }
                    });
                }
                else // Если стоимость не требуется.
                {
                    canStartStudy = true; // Можно начать изучение.
                    UpdateButtonState(true, task, tier); // Обновляет кнопку.
                }
            }
            else // Если задача недоступна.
            {
                UpdateButtonState(false, task, tier); // Отключает кнопку.
            }
        }

        private void UpdateButtonState(bool canStartStudy, CBSTask task, TaskTier tier) // Обновляет состояние кнопки.
        {
            bool rewardAvailable = task.IsComplete && task.IsRewardAvailable(); // Проверяет доступность награды.
            ActionButtonText.text = rewardAvailable ? "Забрать" : (task.IsActive ? "В процессе" : "Начать изучение"); // Устанавливает текст кнопки.
            ActionButton.interactable = (canStartStudy && !task.IsActive) || rewardAvailable; // Включает/выключает кнопку.
        }

        private void OnActionButton() // Обработчик клика на кнопку действия.
        {
            var taskID = Task.ID; // Получает ID задачи.
            var tierIndex = Task.TierIndex; // Получает индекс уровня.
            bool rewardAvailable = Task.IsComplete && Task.IsRewardAvailable(); // Проверяет доступность награды.
            if (rewardAvailable) // Если награда доступна.
            {
                Achievements.PickupAchievementReward(taskID, onPick => // Запрашивает награду.
                {
                    if (onPick.IsSuccess) // Если успешно.
                    {
                        var updatedTask = onPick.Achievement; // Получает обновлённую задачу.
                        Display(updatedTask); // Обновляет отображение.
                    }
                    else // Если ошибка.
                    {
                        new PopupViewer().ShowFabError(onPick.Error); // Показывает ошибку.
                    }
                });
            }
            else // Если награда недоступна.
            {
                Achievements.StartTechStudy(taskID, tierIndex, onStudy => // Начинает изучение.
                {
                    if (onStudy.IsSuccess) // Если успешно.
                    {
                        var updatedTask = onStudy.Achievement; // Получает обновлённую задачу.
                        Display(updatedTask); // Обновляет отображение.
                    }
                    else // Если ошибка.
                    {
                        new PopupViewer().ShowFabError(onStudy.Error); // Показывает ошибку.
                    }
                });
            }
        }
    }
}
#endif

#if ENABLE_PLAYFABADMIN_API // Условная компиляция для PlayFab API.
using CBS.Scriptable; // Импорт ScriptableObject (TasksCategoryPrefabs и других).
using UnityEngine; // Импорт Unity-компонентов.
using UnityEngine.SceneManagement; // Импорт для загрузки сцен.

namespace CBS.UI // Пространство имён для UI CBS.
{
    public class IconsPanel : MonoBehaviour // Класс для управления панелью иконок UI.
    {
        [SerializeField] // Позволяет редактировать в инспекторе.
        private string GameScene; // Название сцены для загрузки игры.

        public void ShowStore() // Открывает окно магазина.
        {
            var prefabs = CBSScriptable.Get<StorePrefabs>(); // Загружает префабы магазина.
            var storePrefab = prefabs.StoreWindows; // Получает префаб окна магазина.
            UIView.ShowWindow(storePrefab); // Открывает окно магазина.
        }

        public void ShowInvertory() // Открывает окно инвентаря.
        {
            var prefabs = CBSScriptable.Get<InventoryPrefabs>(); // Загружает префабы инвентаря.
            var invertoryPrefab = prefabs.Inventory; // Получает префаб инвентаря.
            UIView.ShowWindow(invertoryPrefab); // Открывает окно инвентаря.
        }

        public void ShowEquipment() // Открывает окно экипировки.
        {
            var prefabs = CBSScriptable.Get<EquipmentPrefabs>(); // Загружает префабы экипировки.
            var equipmentPrefab = prefabs.EquipmentInventory; // Получает префаб экипировки.
            UIView.ShowWindow(equipmentPrefab); // Открывает окно экипировки.
        }

        public void ShowLootBox() // Открывает окно лутбоксов.
        {
            var prefabs = CBSScriptable.Get<LootboxPrefabs>(); // Загружает префабы лутбоксов.
            var lootBoxPrefab = prefabs.LootBoxes; // Получает префаб лутбоксов.
            UIView.ShowWindow(lootBoxPrefab); // Открывает окно лутбоксов.
        }

        public void ShowChat() // Открывает окно чата.
        {
            var prefabs = CBSScriptable.Get<ChatPrefabs>(); // Загружает префабы чата.
            var chatPrefab = prefabs.ChatWindow; // Получает префаб чата.
            UIView.ShowWindow(chatPrefab); // Открывает окно чата.
        }

        public void ShowFriends() // Открывает окно друзей.
        {
            var prefabs = CBSScriptable.Get<FriendsPrefabs>(); // Загружает префабы друзей.
            var friendsPrefab = prefabs.FriendsWindow; // Получает префаб друзей.
            UIView.ShowWindow(friendsPrefab); // Открывает окно друзей.
        }

        public void ShowClan() // Открывает окно клана.
        {
            var prefabs = CBSScriptable.Get<ClanPrefabs>(); // Загружает префабы клана.
            var windowPrefab = prefabs.WindowLoader; // Получает префаб клана.
            UIView.ShowWindow(windowPrefab); // Открывает окно клана.
        }

        public void ShowLeaderboards() // Открывает окно лидербордов.
        {
            var prefabs = CBSScriptable.Get<LeaderboardPrefabs>(); // Загружает префабы лидербордов.
            var leaderboardsPrefab = prefabs.LeaderboardsWindow; // Получает префаб лидербордов.
            UIView.ShowWindow(leaderboardsPrefab); // Открывает окно лидербордов.
        }

        public void ShowTournament() // Открывает окно турниров (пустой метод).
        {
            // Ничего не делает, метод не реализован.
        }

        public void ShowDailyBonus() // Открывает окно ежедневных бонусов.
        {
            var prefabs = CBSScriptable.Get<CalendarPrefabs>(); // Загружает префабы календаря.
            var dailyBonusPrefab = prefabs.CalendarWindow; // Получает префаб календаря.
            UIView.ShowWindow(dailyBonusPrefab); // Открывает окно календаря.
        }

        public void ShowRoulette() // Открывает окно рулетки.
        {
            var prefabs = CBSScriptable.Get<RoulettePrefabs>(); // Загружает префабы рулетки.
            var roulettePrefab = prefabs.RouletteWindow; // Получает префаб рулетки.
            UIView.ShowWindow(roulettePrefab); // Открывает окно рулетки.
        }

        public void ShowMatchmaking() // Открывает окно матчмейкинга.
        {
            var prefabs = CBSScriptable.Get<MatchmakingPrefabs>(); // Загружает префабы матчмейкинга.
            var matchmakingPrefab = prefabs.MatchmakingWindow; // Получает префаб матчмейкинга.
            UIView.ShowWindow(matchmakingPrefab); // Открывает окно матчмейкинга.
        }

        public void ShowAchievements() // Открывает окно ачивок.
        {
            var prefabs = CBSScriptable.Get<AchievementsPrefabs>(); // Загружает префабы ачивок.
            var achievementsWindow = prefabs.AchievementsWindow; // Получает префаб ачивок.
            UIView.ShowWindow(achievementsWindow); // Открывает окно ачивок.
        }

        public void ShowDailyTasks() // Открывает окно ежедневных задач.
        {
            var prefabs = CBSScriptable.Get<ProfileTasksPrefabs>(); // Загружает префабы ежедневных задач.
            var tasksWindow = prefabs.ProfileTasksWindow; // Получает префаб ежедневных задач.
            UIView.ShowWindow(tasksWindow); // Открывает окно ежедневных задач.
        }

        public void ShowForge() // Открывает окно кузницы.
        {
            var prefabs = CBSScriptable.Get<CraftPrefabs>(); // Загружает префабы кузницы.
            var craftWindow = prefabs.CraftWindow; // Получает префаб кузницы.
            UIView.ShowWindow(craftWindow); // Открывает окно кузницы.
        }

        public void ShowNotification() // Открывает окно уведомлений.
        {
            var prefabs = CBSScriptable.Get<NotificationPrefabs>(); // Загружает префабы уведомлений.
            var notificationWindow = prefabs.NotificationWindow; // Получает префаб уведомлений.
            UIView.ShowWindow(notificationWindow); // Открывает окно уведомлений.
        }

        public void ShowEvents() // Открывает окно событий.
        {
            var prefabs = CBSScriptable.Get<EventsPrefabs>(); // Загружает префабы событий.
            var eventsWindow = prefabs.EventWindow; // Получает префаб событий.
            UIView.ShowWindow(eventsWindow); // Открывает окно событий.
        }

        public void ShowTechnologies() // Открывает окно технологий.
        {
            var prefabs = CBSScriptable.Get<TasksCategoryPrefabs>(); // Загружает префабы технологий.
            var techWindow = prefabs.TechWindow; // Получает префаб TechWindow.
            UIView.ShowWindow(techWindow); // Открывает окно технологий.
        }

        public void LoadGame() // Загружает игровую сцену.
        {
            SceneManager.LoadScene(GameScene); // Загружает сцену с названием GameScene.
        }
    }
}
#endif

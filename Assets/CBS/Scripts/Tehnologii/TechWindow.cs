#if false
using CBS.Core;
using CBS.Models;
using CBS.Scriptable;
using CBS.Utils;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CBS.UI
{
    public class TechWindow : MonoBehaviour
    {
        [SerializeField]
        private ToggleGroup CategoryGroup;
        [SerializeField]
        private BaseScroller CategoryScroller;

        private string[] CurrentCategories { get; set; } = new[] { "MilitaryAffairs", "Economy", "Blacksmithing", "Treasuries", "MagicalCreatures", "WorldMap" };
        private string CurrentCategory { get; set; } = "MilitaryAffairs";
        private Dictionary<string, CBSTask> CachedTasks { get; set; }
        private TechData TechData { get; set; } // Локальное хранилище статических данных

        private TasksCategoryPrefabs TasksPrefabs { get; set; }
        private InventoryPrefabs InventoryPrefabs { get; set; }
        private CBSAchievementsModule Achievements { get; set; }

        private void Awake()
        {
            TasksPrefabs = CBSScriptable.Get<TasksCategoryPrefabs>();
            InventoryPrefabs = CBSScriptable.Get<InventoryPrefabs>();
            Achievements = CBSModule.Get<CBSAchievementsModule>();
            CachedTasks = new Dictionary<string, CBSTask>();
            TechData = Resources.Load<TechData>("TechData"); // Загружаем TechData
            if (TechData != null)
            {
                // Инициализируем CachedTasks статическими данными
                foreach (var task in TechData.Tasks)
                {
                    CachedTasks[task.ID] = task;
                }
            }
            else
            {
                Debug.LogError("TechWindow: TechData not found in Resources!");
            }
            CategoryScroller.OnSpawn += OnCategorySpawned;
        }

        private void OnDestroy()
        {
            CategoryScroller.OnSpawn -= OnCategorySpawned;
        }

        private void OnEnable()
        {
            Achievements.OnCompleteAchievementTier += OnTaskUpdated;
            Achievements.OnProfileRewarded += OnRewardPicked;
            DisplayCategories();
            LoadTasks(); // Загружаем динамическое состояние
        }

        private void OnDisable()
        {
            Achievements.OnCompleteAchievementTier -= OnTaskUpdated;
            Achievements.OnProfileRewarded -= OnRewardPicked;
        }

        private void DisplayCategories()
        {
            CategoryGroup.SetAllTogglesOff();
            int count = CurrentCategories.Length;
            var categoryPrefab = InventoryPrefabs.CategoryTab;
            CategoryScroller.SpawnItems(categoryPrefab, count);
        }

        private void OnCategorySpawned(GameObject uiItem, int index)
        {
            var scroll = CategoryScroller.GetComponent<ScrollRect>();
            float contentWidth = scroll.GetComponent<RectTransform>().sizeDelta.x;
            int categoriesCount = CurrentCategories.Length;
            float tabWidth = contentWidth / categoriesCount;

            var rectComponent = uiItem.GetComponent<RectTransform>();
            rectComponent.sizeDelta = new Vector2(tabWidth, rectComponent.sizeDelta.y);
            var toggleComponent = uiItem.GetComponent<Toggle>();
            toggleComponent.group = CategoryGroup;
            toggleComponent.isOn = index == 0;

            var tabComponent = uiItem.GetComponent<CategoryTab>();
            tabComponent.TabObject = CurrentCategories[index];
            tabComponent.SetSelectAction(OnCategorySelected);
        }

        private void OnCategorySe тех слот/сех юи и тех виндовс lected(string categoryID)
        {
            CurrentCategory = categoryID;
            GameObject categoryPrefab = GetCategoryPrefab(categoryID);
            if (categoryPrefab != null)
            {
                var uiObject = UIView.ShowWindow(categoryPrefab);
                if (uiObject != null)
                {
                    var slots = uiObject.GetComponentsInChildren<TechSlot>();
                    foreach (var slot in slots)
                    {
                        if (CachedTasks.ContainsKey(slot.TaskID) && CachedTasks[slot.TaskID].Tag == categoryID)
                        {
                            slot.Init(CachedTasks[slot.TaskID], task =>
                            {
                                var uiPrefab = TasksPrefabs.TechInfo;
                                var uiObjectInfo = UIView.ShowWindow(uiPrefab);
                                if (uiObjectInfo != null)
                                {
                                    var infoComponent = uiObjectInfo.GetComponent<TechInfo>();
                                    if (infoComponent != null)
                                    {
                                        infoComponent.Display(task);
                                    }
                                    else
                                    {
                                        Debug.LogError($"TechWindow: TechInfo component not found for TaskID {task.ID}");
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"TechWindow: Failed to open TechInfo for TaskID {task.ID}");
                                }
                            });
                        }
                        else
                        {
                            slot.InitAsLocked(); // Показываем слот как заблокированный
                        }
                    }
                }
                else
                {
                    Debug.LogError($"TechWindow: Failed to open category prefab for {categoryID}");
                }
            }
            else
            {
                Debug.LogError($"TechWindow: No prefab found for category: {categoryID}");
            }
        }

        private void LoadTasks()
        {
            Achievements.GetAchievementsTable(result =>
            {
                if (result.IsSuccess)
                {
                    var tasks = result.AchievementsData.GetTasks();
                    Debug.Log($"TechWindow: Loaded {tasks.Count} tasks from PlayFab");
                    foreach (var task in tasks)
                    {
                        if (CachedTasks.ContainsKey(task.ID))
                        {
                            // Обновляем только динамические данные
                            CachedTasks[task.ID].IsActive = task.IsActive;
                            CachedTasks[task.ID].IsCompleted = task.IsCompleted;
                            CachedTasks[task.ID].TierIndex = task.TierIndex;
                            if (task.TierList != null && task.TierIndex < task.TierList.Count)
                            {
                                CachedTasks[task.ID].TierList[task.TierIndex].CurrentSteps = task.TierList[task.TierIndex].CurrentSteps;
                            }
                        }
                    }
                    OnCategorySelected(CurrentCategory); // Обновляем UI
                }
                else
                {
                    Debug.LogError($"TechWindow: Failed to load tasks: {result.Error.Message}");
                    OnCategorySelected(CurrentCategory); // Показываем UI с локальными данными
                }
            });
        }

        private void OnTaskUpdated(CBSTask task)
        {
            if (CachedTasks.ContainsKey(task.ID))
            {
                CachedTasks[task.ID] = task; // Обновляем задачу в кэше
                if (CurrentCategory == task.Tag)
                {
                    OnCategorySelected(CurrentCategory); // Обновляем UI
                }
            }
        }

        private void OnRewardPicked(GrantRewardResult reward)
        {
            LoadTasks(); // Обновляем состояние после получения награды
        }

        private GameObject GetCategoryPrefab(string categoryID)
        {
            switch (categoryID)
            {
                case "MilitaryAffairs": return TasksPrefabs.MilitaryAffairsCategory;
                case "Economy": return TasksPrefabs.EconomyCategory;
                case "Blacksmithing": return TasksPrefabs.BlacksmithingCategory;
                case "Treasuries": return TasksPrefabs.TreasuriesCategory;
                case "MagicalCreatures": return TasksPrefabs.MagicalCreaturesCategory;
                case "WorldMap": return TasksPrefabs.WorldMapCategory;
                default: return null;
            }
        }
    }
}
#endif

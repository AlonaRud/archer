#if false

using CBS.Models;
using CBS.Scriptable;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CBS.UI
{
    public class TechSlot : MonoBehaviour
    {
        [SerializeField]
        public string TaskID;

        [SerializeField]
        private Button SlotButton;

        [SerializeField]
        private Image SlotIcon;

        private TasksCategoryPrefabs TasksPrefabs { get; set; }
        private CBSAchievementsModule Achievements { get; set; }
        private CBSTask CurrentTask { get; set; }

        private void Awake()
        {
            TasksPrefabs = CBSScriptable.Get<TasksCategoryPrefabs>();
            Achievements = CBSModule.Get<CBSAchievementsModule>();
            if (SlotButton == null)
            {
                Debug.LogError($"TechSlot: SlotButton is not assigned for TaskID {TaskID}");
                return;
            }
            SlotButton.onClick.AddListener(OnSlotClick);
            LoadTask();
        }

        private void OnDestroy()
        {
            if (SlotButton != null)
            {
                SlotButton.onClick.RemoveListener(OnSlotClick);
            }
        }

        private void LoadTask()
        {
            Achievements.GetAchievementsTable(result =>
            {
                if (result.IsSuccess)
                {
                    var task = result.AchievementsData.GetTasks()?.Find(t => t.ID == TaskID);
                    if (task == null)
                    {
                        Debug.LogError($"TechSlot: Task not found for TaskID {TaskID}");
                        gameObject.SetActive(false);
                        return;
                    }
                    CurrentTask = task;
                    if (SlotIcon == null)
                    {
                        Debug.LogError($"TechSlot: SlotIcon is not assigned for TaskID {TaskID}");
                        gameObject.SetActive(false);
                        return;
                    }
                    var sprite = task.GetSprite();
                    if (sprite == null)
                    {
                        Debug.LogWarning($"TechSlot: No sprite found for TaskID {TaskID}");
                        SlotIcon.enabled = false;
                    }
                    else
                    {
                        SlotIcon.sprite = sprite;
                        SlotIcon.enabled = true;
                    }
                }
                else
                {
                    Debug.LogError($"TechSlot: Failed to load tasks for TaskID {TaskID}: {result.Error.Message}");
                    gameObject.SetActive(false);
                }
            });
        }

        public void OnSlotClick()
        {
            if (TasksPrefabs == null || TasksPrefabs.TechInfo == null)
            {
                Debug.LogError($"TechSlot: TechInfo prefab is not set for TaskID {TaskID}");
                return;
            }
            Achievements.GetAchievementsTable(result =>
            {
                if (result.IsSuccess)
                {
                    var task = result.AchievementsData.GetTasks()?.Find(t => t.ID == TaskID);
                    if (task == null)
                    {
                        Debug.LogError($"TechSlot: Task not found for TaskID {TaskID}");
                        return;
                    }
                    var uiObject = UIView.ShowWindow(TasksPrefabs.TechInfo);
                    if (uiObject == null)
                    {
                        Debug.LogError($"TechSlot: Failed to open TechInfo for TaskID {TaskID}");
                        return;
                    }
                    var infoComponent = uiObject.GetComponent<TechInfo>();
                    if (infoComponent == null)
                    {
                        Debug.LogError($"TechSlot: TechInfo component not found for TaskID {TaskID}");
                        return;
                    }
                    infoComponent.Display(task);
                }
                else
                {
                    Debug.LogError($"TechSlot: Failed to load tasks for TaskID {TaskID}: {result.Error.Message}");
                }
            });
        }
    }
}
#endif
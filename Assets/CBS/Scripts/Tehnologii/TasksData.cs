using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CBS.Models;
using CBS.Scriptable;
using CBS;

[CreateAssetMenu(fileName = "TasksData", menuName = "CBS/TasksData")]
public class TasksData : IconsData
{
    public override string ResourcePath => "TasksData";
    public override string EditorPath => "Assets/CBS_External/Resources";
    public override string EditorAssetName => "TasksData.asset";

    [System.Serializable]
    public class TierData
    {
        public int Index;
        public int StudyHours;
        public int StudyMinutes;
        public string BonusType;
        public int BonusValue;
        public string TargetUnitType;
        public int Cost;
        public string CurrencyCode;
        public int StepsToComplete;
        public int CurrentSteps;
        public bool OverrideDescription;
        public string Description; // Описание для уровня
        public List<Dependency> Dependencies;
        public List<ResourceCost> ResourceCosts;
        public string ProductionResourceID;
        public int ProductionRate;
    }

    [System.Serializable]
    public class TaskData
    {
        public string TaskID;
        public Sprite Icon;
        public string Tag;
        public string Title;
        public string Description; // Общее описание
        public List<TierData> Tier; // Список всех уровней
    }

    [SerializeField]
    private List<TaskData> Tasks = new List<TaskData>(); // Список задач, явно сериализуемый

    public TaskData GetTask(string taskID)
    {
        if (Tasks == null)
        {
            Tasks = new List<TaskData>();
            Debug.LogWarning("Tasks list was null, initialized new list");
        }
        var task = Tasks.FirstOrDefault(t => t.TaskID == taskID);
        if (task == null)
            Debug.LogWarning($"Task with ID {taskID} not found in TasksData");
        return task;
    }

    public void SaveTask(CBSTask task, Sprite icon)
    {
        if (task == null)
        {
            Debug.LogError("Task is null, cannot save");
            return;
        }

        // Твой изначальный список категорий
        var validCategories = new[] { "MilitaryAffairs", "Economy", "Blacksmithing", "Treasuries", "MagicalCreatures", "WorldMap" };
        if (!validCategories.Contains(task.Tag))
        {
            Debug.LogWarning($"Task {task.ID} skipped, not a technology: {task.Tag}. Valid categories: {string.Join(", ", validCategories)}");
            return;
        }

        if (Tasks == null)
        {
            Tasks = new List<TaskData>();
            Debug.Log("Initialized Tasks list");
        }

        Debug.Log($"Saving task {task.ID}, Current Tasks count: {Tasks.Count}");
        Debug.Log($"TierList count: {task.TierList?.Count ?? 0}");
        var tierDataList = new List<TierData>();
        if (task.TierList != null && task.TierList.Count > 0)
        {
            for (int i = 0; i < task.TierList.Count; i++)
            {
                var tier = task.TierList[i];
                Debug.Log($"Tier {i}: StudyHours={tier.StudyHours}, StudyMinutes={tier.StudyMinutes}, BonusType={tier.Reward?.Type}, BonusValue={tier.Reward?.Value}, TargetUnitType={tier.Reward?.TargetUnitType}, Cost={tier.Cost}, CurrencyCode={tier.CurrencyCode}, StepsToComplete={tier.StepsToComplete}, CurrentSteps={tier.CurrentSteps}, OverrideDescription={tier.OverrideDescription}, Description={tier.Description}, ProductionResourceID={tier.ProductionResourceID}, ProductionRate={tier.ProductionRate}");

                var tierData = new TierData
                {
                    Index = tier.Index,
                    StudyHours = tier.StudyHours,
                    StudyMinutes = tier.StudyMinutes,
                    BonusType = tier.Reward != null ? tier.Reward.Type : "",
                    BonusValue = tier.Reward != null ? tier.Reward.Value : 0,
                    TargetUnitType = tier.Reward != null ? tier.Reward.TargetUnitType : "",
                    Cost = tier.Cost,
                    CurrencyCode = tier.CurrencyCode,
                    StepsToComplete = tier.StepsToComplete,
                    CurrentSteps = tier.CurrentSteps,
                    OverrideDescription = tier.OverrideDescription,
                    Description = tier.Description,
                    Dependencies = tier.Dependencies != null ? new List<Dependency>(tier.Dependencies) : new List<Dependency>(),
                    ResourceCosts = tier.ResourceCosts != null ? new List<ResourceCost>(tier.ResourceCosts) : new List<ResourceCost>(),
                    ProductionResourceID = tier.ProductionResourceID,
                    ProductionRate = tier.ProductionRate
                };
                tierDataList.Add(tierData);
            }
        }
        else
        {
            Debug.LogWarning($"Task {task.ID} has no TierList, saving with empty Tier");
            tierDataList = new List<TierData>(); // Пустой список уровней
        }

        var taskData = new TaskData
        {
            TaskID = task.ID,
            Icon = icon,
            Tag = task.Tag,
            Title = task.Title,
            Description = task.Description,
            Tier = tierDataList
        };

        Debug.Log($"Saving to taskData: TaskID={taskData.TaskID}, Title={taskData.Title}, Tag={taskData.Tag}, Tier count={taskData.Tier.Count}");
        foreach (var tier in taskData.Tier)
        {
            Debug.Log($"Tier {tier.Index}: StudyHours={tier.StudyHours}, StudyMinutes={tier.StudyMinutes}, BonusType={tier.BonusType}, BonusValue={tier.BonusValue}, TargetUnitType={tier.TargetUnitType}, Cost={tier.Cost}, CurrencyCode={tier.CurrencyCode}, StepsToComplete={tier.StepsToComplete}, CurrentSteps={tier.CurrentSteps}, OverrideDescription={tier.OverrideDescription}, Description={tier.Description}, ProductionResourceID={tier.ProductionResourceID}, ProductionRate={tier.ProductionRate}");
        }

        // Проверяем существующие задачи, чтобы не сбросить список
        var existingTask = Tasks.FirstOrDefault(t => t.TaskID == task.ID);
        if (existingTask != null)
        {
            Debug.Log($"Updating existing task {task.ID}");
            Tasks[Tasks.IndexOf(existingTask)] = taskData;
        }
        else
        {
            Debug.Log($"Adding new task {task.ID}");
            Tasks.Add(taskData);
        }

#if UNITY_EDITOR
        // Помечаем объект как измененный и сохраняем
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"TasksData saved, Tasks count: {Tasks.Count}");
        // Проверяем все задачи после сохранения
        foreach (var savedTask in Tasks)
        {
            Debug.Log($"Saved Task: TaskID={savedTask.TaskID}, Title={savedTask.Title}, Tier count={savedTask.Tier.Count}");
            foreach (var tier in savedTask.Tier)
            {
                Debug.Log($"  Tier {tier.Index}: StudyHours={tier.StudyHours}, StudyMinutes={tier.StudyMinutes}, BonusType={tier.BonusType}, OverrideDescription={tier.OverrideDescription}, Description={tier.Description}");
            }
        }
#endif
    }

    public void RemoveTask(string taskID)
    {
        if (Tasks == null)
        {
            Tasks = new List<TaskData>();
            Debug.Log("Initialized Tasks list for RemoveTask");
            return;
        }

        var task = Tasks.FirstOrDefault(t => t.TaskID == taskID);
        if (task != null)
        {
            Tasks.Remove(task);
            Debug.Log($"Removed task {taskID}, Tasks count: {Tasks.Count}");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        else
        {
            Debug.LogWarning($"Task {taskID} not found for removal");
        }
    }
}
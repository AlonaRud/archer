#if ENABLE_PLAYFABADMIN_API
using CBS.Models;
using CBS.Scriptable;
using CBS.Utils;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CBS.Editor.Window
{
    public class AddTaskWindow<TTask> : EditorWindow where TTask : CBSTask, new()
    {
        private static Action<TTask> AddCallback { get; set; }
        protected static TTask CurrentData { get; set; }
        private static ItemAction Action { get; set; }
        private static string PoolID { get; set; }
        private static List<string> Categories { get; set; }
        private static int CategoryAtStart { get; set; }

        protected string[] Titles = new string[] { "Info", "Configs", "Linked Data" };
        protected string AddTitle = "Add Task";
        protected string SaveTitle = "Save Task";

        private string ID { get; set; }
        private string Title { get; set; }
        private string Description { get; set; }
        private string ExternalUrl { get; set; }
        private string ItemTag { get; set; }
        private Sprite IconSprite { get; set; }
        private GameObject LinkedPrefab { get; set; }
        private ScriptableObject LinkedScriptable { get; set; }
        private TaskType TaskType { get; set; }
        private int Steps { get; set; }
        private bool LockedByLevel { get; set; }
        private int LockedLevel { get; set; }
        private int SelectedCategoryIndex { get; set; }

        private Vector2 ScrollPos { get; set; }
        private bool IsInited { get; set; } = false;
        private int SelectedToolBar { get; set; }
        private TasksIcons Icons { get; set; }
        private Texture2D TierTex { get; set; }
        private EditorData EditorData { get; set; }
        private ObjectCustomDataDrawer<CBSTaskCustomData> CustomDataDrawer { get; set; }

        public static void Show<T>(TTask current, string poolID, ItemAction action, Action<TTask> addCallback, List<string> categories = null, int categoryIndex = 0) where T : EditorWindow
        {
            AddCallback = addCallback;
            CurrentData = current ?? new TTask(); // Убедимся, что CurrentData не null
            Action = action;
            PoolID = poolID;
            Categories = categories ?? new List<string> { CBSConstants.UndefinedCategory };
            CategoryAtStart = categoryIndex;

            var window = ScriptableObject.CreateInstance<T>();
            window.maxSize = new Vector2(400, 700);
            window.minSize = window.maxSize;
            window.ShowUtility();
        }

        private void Hide()
        {
            this.Close();
        }

        protected virtual void Init()
        {
            Icons = CBSScriptable.Get<TasksIcons>();
            EditorData = CBSScriptable.Get<EditorData>();
            TierTex = EditorUtils.MakeColorTexture(EditorData.UpgradeTitle);
            CustomDataDrawer = new ObjectCustomDataDrawer<CBSTaskCustomData>(PlayfabUtils.DEFAULT_CUSTOM_DATA_SIZE, 380f);

            ID = CurrentData.ID ?? Guid.NewGuid().ToString(); // Уникальный ID для новых задач
            Title = CurrentData.Title;
            Description = CurrentData.Description;
            ExternalUrl = CurrentData.ExternalUrl;
            ItemTag = CurrentData.Tag;
            TaskType = CurrentData.Type;
            IconSprite = CurrentData.GetSprite();
            Steps = CurrentData.Steps;
            LockedByLevel = CurrentData.IsLockedByLevel;
            LockedLevel = CurrentData.LockLevel;
            LinkedPrefab = CBSScriptable.Get<LinkedPrefabData>().GetLinkedData(ID);
            LinkedScriptable = CBSScriptable.Get<LinkedScriptableData>().GetLinkedData(ID);
            SelectedCategoryIndex = Categories.Contains(ItemTag) ? Categories.IndexOf(ItemTag) : CategoryAtStart;

            // Инициализация TierList, если null
            if (CurrentData.TierList == null)
                CurrentData.TierList = new List<TaskTier>();

            IsInited = true;
        }

        protected virtual void CheckInputs()
        {
            CurrentData.ID = ID;
            CurrentData.Title = Title;
            CurrentData.Description = Description;
            CurrentData.ExternalUrl = ExternalUrl;
            CurrentData.Tag = string.IsNullOrEmpty(Categories[SelectedCategoryIndex]) || Categories[SelectedCategoryIndex] == "All" ? CBSConstants.UndefinedCategory : Categories[SelectedCategoryIndex];
            CurrentData.Type = TaskType;
            CurrentData.Steps = TaskType == TaskType.ONE_SHOT ? 0 : Steps;
            CurrentData.IsLockedByLevel = LockedByLevel;
            CurrentData.LockLevel = LockedLevel;
            CurrentData.PoolID = PoolID;

            // Логирование для отладки
            Debug.Log($"CheckInputs: ID={CurrentData.ID}, Title={CurrentData.Title}, Tag={CurrentData.Tag}, TierList count={CurrentData.TierList?.Count ?? 0}");
        }

        void OnGUI()
        {
            using (var areaScope = new GUILayout.AreaScope(new Rect(0, 0, 400, 700)))
            {
                ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
                SelectedToolBar = GUILayout.Toolbar(SelectedToolBar, Titles);

                if (!IsInited)
                {
                    Init();
                }

                switch (SelectedToolBar)
                {
                    case 0:
                        DrawInfo();
                        break;
                    case 1:
                        DrawConfigs();
                        break;
                    case 2:
                        DrawLinkedData();
                        break;
                    default:
                        break;
                }

                GUILayout.FlexibleSpace();
                string buttonTitle = Action == ItemAction.ADD ? AddTitle : SaveTitle;
                if (GUILayout.Button(buttonTitle))
                {
                    if (IsInputValid())
                    {
                        var prefabData = CBSScriptable.Get<LinkedPrefabData>();
                        var scriptableData = CBSScriptable.Get<LinkedScriptableData>();
                        var tasksData = CBSScriptable.Get<TasksData>();

                        if (IconSprite == null)
                        {
                            var avatarID = PoolID + ID;
                            Icons.RemoveSprite(avatarID);
                        }
                        else
                        {
                            var avatarID = PoolID + ID;
                            Icons.SaveSprite(avatarID, IconSprite);
                        }

                        if (LinkedPrefab == null)
                        {
                            prefabData.RemoveAsset(ID);
                        }
                        else
                        {
                            prefabData.SaveAssetData(ID, LinkedPrefab);
                        }

                        if (LinkedScriptable == null)
                        {
                            scriptableData.RemoveAsset(ID);
                        }
                        else
                        {
                            scriptableData.SaveAssetData(ID, LinkedScriptable);
                        }

                        // Сохранение задачи в TasksData
                        CheckInputs();
                        tasksData.SaveTask(CurrentData, IconSprite);

                        Debug.Log($"Saving task: ID={CurrentData.ID}, TierList count={CurrentData.TierList?.Count ?? 0}");
                        if (CurrentData.TierList != null)
                        {
                            for (int i = 0; i < CurrentData.TierList.Count; i++)
                            {
                                Debug.Log($"Tier {i}: StudyHours={CurrentData.TierList[i].StudyHours}, StudyMinutes={CurrentData.TierList[i].StudyMinutes}, BonusType={CurrentData.TierList[i].Reward?.Type}, OverrideDescription={CurrentData.TierList[i].OverrideDescription}, Description={CurrentData.TierList[i].Description}");
                            }
                        }

                        AddCallback?.Invoke(CurrentData);
                        Hide();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawLinkedData()
        {
            var titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 12;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Sprite", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            IconSprite = (Sprite)EditorGUILayout.ObjectField((IconSprite as UnityEngine.Object), typeof(Sprite), false, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            EditorGUILayout.HelpBox("Sprite for game task. ATTENTION! The sprite is not saved on the server, it will be included in the build", MessageType.Info);

            var iconTexture = IconSprite == null ? null : IconSprite.texture;
            GUILayout.Button(iconTexture, GUILayout.Width(100), GUILayout.Height(100));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Prefab", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            LinkedPrefab = (GameObject)EditorGUILayout.ObjectField((LinkedPrefab as UnityEngine.Object), typeof(GameObject), false, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            EditorGUILayout.HelpBox("Prefab for game task (e.g., building model). ATTENTION! The prefab is not saved on the server, it will be included in the build", MessageType.Info);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Scriptable", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            LinkedScriptable = (ScriptableObject)EditorGUILayout.ObjectField((LinkedScriptable as UnityEngine.Object), typeof(ScriptableObject), false, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            EditorGUILayout.HelpBox("Scriptable data for game task (e.g., building stats). ATTENTION! The data is not saved on the server, it will be included in the build", MessageType.Info);

            GUILayout.Space(10);
            ExternalUrl = EditorGUILayout.TextField("External Icon URL", ExternalUrl);
            EditorGUILayout.HelpBox("You can use it for example for remote texture url", MessageType.Info);
        }

        private void DrawInfo()
        {
            GUILayout.Space(15);
            if (Action == ItemAction.ADD)
            {
                ID = EditorGUILayout.TextField("Task ID", ID);
            }
            if (Action == ItemAction.EDIT)
            {
                EditorGUILayout.LabelField("Task ID", ID);
            }
            EditorGUILayout.HelpBox("Unique id for task", MessageType.Info);
            if (string.IsNullOrEmpty(ID))
            {
                EditorGUILayout.HelpBox("ID can not be empty", MessageType.Error);
            }

            GUILayout.Space(15);
            Title = EditorGUILayout.TextField("Title", Title);
            EditorGUILayout.HelpBox("Full name of the task", MessageType.Info);

            GUILayout.Space(15);
            var descriptionTitle = new GUIStyle(GUI.skin.textField);
            descriptionTitle.wordWrap = true;
            EditorGUILayout.LabelField("Description");
            Description = EditorGUILayout.TextArea(Description, descriptionTitle, new GUILayoutOption[] { GUILayout.Height(150) });

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Category");
            SelectedCategoryIndex = EditorGUILayout.Popup(SelectedCategoryIndex, Categories.ToArray());
            EditorGUILayout.HelpBox("Task category", MessageType.Info);

            GUILayout.Space(15);
            CustomDataDrawer.Draw(CurrentData);
        }

        protected virtual void DrawConfigs()
        {
            var descriptionTitle = new GUIStyle(GUI.skin.textField);
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 12;
            GUIStyle recipeBoxStyle = new GUIStyle("HelpBox");

            GUILayout.Space(15);
            TaskType = (TaskType)EditorGUILayout.EnumPopup("Task Type", TaskType);
            EditorGUILayout.HelpBox("Task type. If OneShot is selected, the task will be completed the first time it is executed. If Steps - you need to perform several steps of the actions specified by you. If BUILDING - configure resources, time, and production.", MessageType.Info);
            if (TaskType == TaskType.STEPS)
            {
                GUILayout.Space(15);
                Steps = EditorGUILayout.IntField("Steps", Steps);
                EditorGUILayout.HelpBox("Number of steps to complete the task", MessageType.Info);
                if (Steps <= 0)
                    Steps = 1;
            }
            else if (TaskType == TaskType.TIERED)
            {
                GUILayout.Space(10);

                var tiers = CurrentData.TierList ?? new List<TaskTier>();
                for (int i = 0; i < tiers.Count; i++)
                {
                    recipeBoxStyle.normal.background = TierTex;
                    using (var horizontalScope = new GUILayout.VerticalScope(recipeBoxStyle))
                    {
                        EditorGUILayout.LabelField("Tier " + i.ToString(), style);
                        tiers[i].StepsToComplete = EditorGUILayout.IntField("Steps", tiers[i].StepsToComplete);
                        if (tiers[i].StepsToComplete <= 0)
                            tiers[i].StepsToComplete = 1;
                        tiers[i].OverrideDescription = EditorGUILayout.Toggle("Override Description?", tiers[i].OverrideDescription);
                        if (tiers[i].OverrideDescription)
                        {
                            EditorGUILayout.LabelField("Description");
                            tiers[i].Description = EditorGUILayout.TextArea(tiers[i].Description, descriptionTitle, new GUILayoutOption[] { GUILayout.Height(50) });
                        }

                        // Study time in hours and minutes
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Study Time", style);
                        tiers[i].StudyHours = EditorGUILayout.IntField("Hours", tiers[i].StudyHours);
                        tiers[i].StudyMinutes = EditorGUILayout.IntField("Minutes", tiers[i].StudyMinutes);
                        if (tiers[i].StudyHours < 0) tiers[i].StudyHours = 0;
                        if (tiers[i].StudyMinutes < 0) tiers[i].StudyMinutes = 0;
                        EditorGUILayout.HelpBox("Время в часах и минутах (например, 1 час 30 минут или 10 минут)", MessageType.Info);

                        // Dependencies
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Dependencies", style);
                        tiers[i].Dependencies = tiers[i].Dependencies ?? new List<Dependency>();
                        int depCount = EditorGUILayout.IntField("Number of Dependencies", tiers[i].Dependencies.Count);
                        while (tiers[i].Dependencies.Count < depCount)
                            tiers[i].Dependencies.Add(new Dependency());
                        while (tiers[i].Dependencies.Count > depCount)
                            tiers[i].Dependencies.RemoveAt(tiers[i].Dependencies.Count - 1);
                        for (int j = 0; j < tiers[i].Dependencies.Count; j++)
                        {
                            EditorGUILayout.LabelField($"Dependency {j + 1}", style);
                            var dep = tiers[i].Dependencies[j];
                            dep.TechID = EditorGUILayout.TextField("Tech/Building ID", dep.TechID);
                            dep.Level = EditorGUILayout.IntField("Level", dep.Level);
                            if (dep.Level < 1)
                                dep.Level = 1;
                            tiers[i].Dependencies[j] = dep;
                        }
                        EditorGUILayout.HelpBox("Specify dependent technology or building ID and required level (e.g., ArmorTech or Barracks, Level 2)", MessageType.Info);

                        // Bonus in Reward
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Bonus", style);
                        tiers[i].Reward = tiers[i].Reward ?? new CBS.RewardObject();
                        tiers[i].Reward.Type = EditorGUILayout.TextField("Bonus Type", tiers[i].Reward.Type ?? "");
                        tiers[i].Reward.Value = EditorGUILayout.IntField("Bonus Value (%)", tiers[i].Reward.Value);
                        tiers[i].Reward.TargetUnitType = EditorGUILayout.TextField("Target Unit Type", tiers[i].Reward.TargetUnitType ?? "");
                        EditorGUILayout.HelpBox("Enter bonus type (e.g., TroopsStrength, HeroHealth, EconomyIncome), value (e.g., 5 for +5%), and target unit type (e.g., Archer, Cavalry, leave empty for non-troop bonuses)", MessageType.Info);

                        // Cost and Currency
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Cost", style);
                        tiers[i].Cost = EditorGUILayout.IntField("Cost", tiers[i].Cost);
                        if (tiers[i].Cost < 0) tiers[i].Cost = 0;
                        tiers[i].CurrencyCode = EditorGUILayout.TextField("Currency Code", tiers[i].CurrencyCode ?? "");
                        EditorGUILayout.HelpBox("Укажите стоимость уровня и код валюты (например, GD для золота). Валюта должна быть настроена в системе.", MessageType.Info);
                    }
                    GUILayout.Space(2);
                }
                CurrentData.TierList = tiers;

                GUILayout.Space(3);
                if (GUILayout.Button("Remove last"))
                {
                    CurrentData.RemoveLastTier();
                }
                GUILayout.Space(10);
                var nextIndex = CurrentData.GetNextTierIndex();
                if (GUILayout.Button("Add new tier"))
                {
                    CurrentData.AddNewTier(new TaskTier { Index = nextIndex });
                }
            }
            else if (TaskType == TaskType.BUILDING)
            {
                GUILayout.Space(10);
                var tiers = CurrentData.TierList ?? new List<TaskTier>();
                for (int i = 0; i < tiers.Count; i++)
                {
                    recipeBoxStyle.normal.background = TierTex;
                    using (var horizontalScope = new GUILayout.VerticalScope(recipeBoxStyle))
                    {
                        EditorGUILayout.LabelField("Tier " + i.ToString(), style);

                        // Время строительства
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Build Time", style);
                        tiers[i].StudyHours = EditorGUILayout.IntField("Hours", tiers[i].StudyHours);
                        tiers[i].StudyMinutes = EditorGUILayout.IntField("Minutes", tiers[i].StudyMinutes);
                        if (tiers[i].StudyHours < 0) tiers[i].StudyHours = 0;
                        if (tiers[i].StudyMinutes < 0) tiers[i].StudyMinutes = 0;
                        EditorGUILayout.HelpBox("Время строительства в часах и минутах", MessageType.Info);

                        // Ресурсы (предметы)
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Resources", style);
                        tiers[i].ResourceCosts = tiers[i].ResourceCosts ?? new List<ResourceCost>();
                        int resCount = EditorGUILayout.IntField("Number of Resources", tiers[i].ResourceCosts.Count);
                        while (tiers[i].ResourceCosts.Count < resCount)
                            tiers[i].ResourceCosts.Add(new ResourceCost());
                        while (tiers[i].ResourceCosts.Count > resCount)
                            tiers[i].ResourceCosts.RemoveAt(tiers[i].ResourceCosts.Count - 1);
                        for (int j = 0; j < tiers[i].ResourceCosts.Count; j++)
                        {
                            EditorGUILayout.LabelField($"Resource {j + 1}", style);
                            var res = tiers[i].ResourceCosts[j];
                            res.ItemId = EditorGUILayout.TextField("Item ID", res.ItemId);
                            res.Amount = EditorGUILayout.IntField("Amount", res.Amount);
                            if (res.Amount < 0) res.Amount = 0;
                            tiers[i].ResourceCosts[j] = res;
                        }
                        EditorGUILayout.HelpBox("ID предмета (например, WOOD, STONE) и количество", MessageType.Info);

                        // Зависимости
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Dependencies", style);
                        tiers[i].Dependencies = tiers[i].Dependencies ?? new List<Dependency>();
                        int depCount = EditorGUILayout.IntField("Number of Dependencies", tiers[i].Dependencies.Count);
                        while (tiers[i].Dependencies.Count < depCount)
                            tiers[i].Dependencies.Add(new Dependency());
                        while (tiers[i].Dependencies.Count > depCount)
                            tiers[i].Dependencies.RemoveAt(tiers[i].Dependencies.Count - 1);
                        for (int j = 0; j < tiers[i].Dependencies.Count; j++)
                        {
                            EditorGUILayout.LabelField($"Dependency {j + 1}", style);
                            var dep = tiers[i].Dependencies[j];
                            dep.TechID = EditorGUILayout.TextField("Building/Tech ID", dep.TechID);
                            dep.Level = EditorGUILayout.IntField("Level", dep.Level);
                            if (dep.Level < 1) dep.Level = 1;
                            tiers[i].Dependencies[j] = dep;
                        }
                        EditorGUILayout.HelpBox("ID здания/технологии и требуемый уровень", MessageType.Info);

                        // Производство
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Production", style);
                        tiers[i].ProductionResourceID = EditorGUILayout.TextField("Resource ID", tiers[i].ProductionResourceID ?? "");
                        tiers[i].ProductionRate = EditorGUILayout.IntField("Rate (per hour)", tiers[i].ProductionRate);
                        if (tiers[i].ProductionRate < 0) tiers[i].ProductionRate = 0;
                        EditorGUILayout.HelpBox("Производимый ресурс (например, GOLD, WOOD) и количество в час", MessageType.Info);

                        // Бонус
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Bonus", style);
                        tiers[i].Reward = tiers[i].Reward ?? new CBS.RewardObject();
                        tiers[i].Reward.Type = EditorGUILayout.TextField("Bonus Type", tiers[i].Reward.Type ?? "");
                        tiers[i].Reward.Value = EditorGUILayout.IntField("Bonus Value (%)", tiers[i].Reward.Value);
                        tiers[i].Reward.TargetUnitType = EditorGUILayout.TextField("Target Unit Type", tiers[i].Reward.TargetUnitType ?? "");
                        EditorGUILayout.HelpBox("Тип бонуса (например, TroopsTrainingSpeed, TechStudySpeed), значение (%), тип юнита (если применимо)", MessageType.Info);
                    }
                    GUILayout.Space(2);
                }
                CurrentData.TierList = tiers;

                GUILayout.Space(3);
                if (GUILayout.Button("Remove last"))
                {
                    CurrentData.RemoveLastTier();
                }
                GUILayout.Space(10);
                var nextIndex = CurrentData.GetNextTierIndex();
                if (GUILayout.Button("Add new tier"))
                {
                    CurrentData.AddNewTier(new TaskTier { Index = nextIndex });
                }
            }

            GUILayout.Space(15);
            LockedByLevel = EditorGUILayout.Toggle("Locked by level?", LockedByLevel);
            EditorGUILayout.HelpBox("Determines whether the task will be locked by player level.", MessageType.Info);
            if (LockedByLevel)
            {
                GUILayout.Space(15);
                LockedLevel = EditorGUILayout.IntField("Level", LockedLevel);
                EditorGUILayout.HelpBox("Level to lock task", MessageType.Info);
                if (LockedLevel < 0)
                    LockedLevel = 0;
                GUILayout.Space(15);
                EditorGUILayout.LabelField("Availability filter", GUILayout.Width(100));
                CurrentData.LevelFilter = (IntFilter)EditorGUILayout.EnumPopup(CurrentData.LevelFilter, new GUILayoutOption[] { GUILayout.Width(150) });
                EditorGUILayout.HelpBox("Parameter for defining accessibility by level", MessageType.Info);
            }
        }

        private bool IsInputValid()
        {
            var validID = !string.IsNullOrEmpty(ID);
            if (TaskType == TaskType.BUILDING)
            {
                var tiers = CurrentData.TierList;
                if (tiers != null)
                {
                    foreach (var tier in tiers)
                    {
                        if (tier.ResourceCosts != null)
                        {
                            foreach (var res in tier.ResourceCosts)
                            {
                                if (string.IsNullOrEmpty(res.ItemId) || res.Amount < 0)
                                    return false;
                            }
                        }
                        if (!string.IsNullOrEmpty(tier.ProductionResourceID) && tier.ProductionRate < 0)
                            return false;
                        if (!string.IsNullOrEmpty(tier.Reward.Type) && tier.Reward.Value < 0)
                            return false;
                    }
                }
            }
            return validID;
        }
    }
}
#endif
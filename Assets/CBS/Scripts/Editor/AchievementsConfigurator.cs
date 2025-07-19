#if ENABLE_PLAYFABADMIN_API
using CBS.Editor.Window;
using CBS.Models;
using CBS.Scriptable;
using CBS.Utils;
using PlayFab;
using PlayFab.AdminModels;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CBS.Editor
{
    public class AchievementsConfigurator : BaseTasksConfigurator<CBSTask, AchievementsData, AddAchievementWindow>
    {
        protected override string Title => "Achievements";
        protected override string TASK_TITLE_ID => TitleKeys.AchievementsTitleKey;
        protected override string[] Titles => new string[] { "Achievements", "Additional configs" };
        protected override string ItemKey => "task";

        private Rect CategoriesRect = new Rect(0, 55, 150, 620);
        private Rect ItemsRect = new Rect(150, 115, 935, 600);
        private Vector2 TitleScroll { get; set; }
        private Vector2 TasksScroll { get; set; }
        private int CategoryIndex { get; set; }
        private string SelectedCategory { get; set; }
        private Categories CachedCategories { get; set; }
        private int LocalSelectedToolBar { get; set; }

        public override void Init(MenuTitles title)
        {
            base.Init(title);
            GetTitleData();
        }

        protected override void OnDrawInside()
        {
            LocalSelectedToolBar = GUILayout.Toolbar(LocalSelectedToolBar, Titles, GUILayout.MaxWidth(1200));

            if (TasksData == null)
                return;

            DrawCategories();
            using (var areaScope = new GUILayout.AreaScope(ItemsRect))
            {
                TasksScroll = GUILayout.BeginScrollView(TasksScroll);
                if (LocalSelectedToolBar == 0)
                {
                    DrawTasks();
                }
                else if (LocalSelectedToolBar == 1)
                {
                    DrawConfigs();
                }
                GUILayout.Space(110);
                GUILayout.EndScrollView();
            }
        }

        private void DrawCategories()
        {
            using (var areaScope = new GUILayout.AreaScope(CategoriesRect))
            {
                var levelTitleStyle = new GUIStyle(GUI.skin.label);
                levelTitleStyle.fontStyle = FontStyle.Bold;
                levelTitleStyle.fontSize = 14;

                GUILayout.BeginVertical();
                GUILayout.Space(112);
                EditorGUILayout.LabelField("Categories", levelTitleStyle);

                int categoryHeight = 30;
                var categoriesMenu = CachedCategories.List.ToList();
                categoriesMenu.Remove(CBSConstants.UndefinedCategory);
                categoriesMenu.Insert(0, "All");
                var gridRect = new Rect(0, 142, 150, categoryHeight * categoriesMenu.Count);
                var scrollRect = gridRect;
                scrollRect.height += categoryHeight * 4;
                scrollRect.width = 0;
                TitleScroll = GUI.BeginScrollView(CategoriesRect, TitleScroll, scrollRect);

                CategoryIndex = GUI.SelectionGrid(gridRect, CategoryIndex, categoriesMenu.ToArray(), 1);
                SelectedCategory = categoriesMenu[CategoryIndex] == "All" ? string.Empty : categoriesMenu[CategoryIndex];

                GUILayout.Space(30);
                var oldColor = GUI.color;
                GUI.backgroundColor = EditorData.AddColor;
                var style = new GUIStyle(GUI.skin.button);
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 12;
                if (GUI.Button(new Rect(0, 170 + categoryHeight * categoriesMenu.Count, 150, categoryHeight), "Add category", style))
                {
                    ModifyCateroriesWindow.Show(onModify =>
                    {
                        CachedCategories.List = onModify.List;
                        SaveCategories(onModify.List);
                    }, CachedCategories);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = oldColor;

                GUILayout.EndVertical();
                GUI.EndScrollView();
            }
        }

        protected override void DrawTasks()
        {
            GUILayout.BeginVertical(GUILayout.MaxWidth(1200));

            GUILayout.Space(20);

            var titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 14;

            var tierStyle = new GUIStyle(GUI.skin.label);
            tierStyle.fontStyle = FontStyle.Bold;
            tierStyle.fontSize = 12;

            var middleStyle = new GUIStyle(GUI.skin.label);
            middleStyle.fontStyle = FontStyle.Bold;
            middleStyle.fontSize = 14;
            middleStyle.alignment = TextAnchor.MiddleCenter;

            // draw add queue 
            GUILayout.BeginHorizontal();

            // draw name
            GUILayout.Space(27);
            EditorGUILayout.LabelField("Name", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });

            // draw count
            GUILayout.Space(155);
            EditorGUILayout.LabelField("Steps", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });

            // draw mode
            GUILayout.Space(40);
            EditorGUILayout.LabelField("Level", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(100) });

            GUILayout.FlexibleSpace();
            if (EditorUtils.DrawButton("Add new " + ItemKey, EditorData.AddColor, 12, new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(30) }))
            {
                AddTaskWindow<CBSTask>.Show<AddAchievementWindow>(new CBSTask(), TASK_TITLE_ID, ItemAction.ADD, onAdd =>
                {
                    TasksData.Add(onAdd);
                    SaveTasksTable(TasksData);
                }, CachedCategories.List, CategoryIndex);
                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();

            EditorUtils.DrawUILine(Color.black, 2, 20);

            GUILayout.Space(20);

            if (TasksData == null)
            {
                GUILayout.EndVertical();
                return;
            }           

            var taskList = TasksData.GetTasks();
            if (!string.IsNullOrEmpty(SelectedCategory))
            {
                taskList = taskList.Where(x => x.Tag == SelectedCategory).ToList();
            }

            for (int i = 0; i < taskList.Count; i++)
            {
                var task = taskList[i];
                GUILayout.BeginHorizontal();
                string positionString = (i + 1).ToString();
                var positionDetail = task;

                EditorGUILayout.LabelField(positionString, titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(20), GUILayout.Height(50) });

                var actvieSprite = task.GetSprite();
                var iconTexture = actvieSprite == null ? null : actvieSprite.texture;
                GUILayout.Button(iconTexture, GUILayout.Width(50), GUILayout.Height(50));

                // draw title
                EditorGUILayout.LabelField(task.Title, titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150), GUILayout.Height(50) });

                // draw steps
                var stepsLabel = task.Type == TaskType.ONE_SHOT ? "One shot" : task.Steps.ToString();
                if (task.Type == TaskType.TIERED)
                    stepsLabel = string.Empty;
                GUILayout.Space(90);
                EditorGUILayout.LabelField(stepsLabel.ToString(), middleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(70), GUILayout.Height(50) });

                // draw level
                var levelLabel = task.IsLockedByLevel ? task.LockLevel.ToString() : "--";
                GUILayout.Space(148);
                EditorGUILayout.LabelField(levelLabel.ToString(), titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(50), GUILayout.Height(50) });

                GUILayout.FlexibleSpace();

                GUILayout.Space(50);

                if (task.Type != TaskType.TIERED)
                {
                    DrawTaskRewardsButtons(task);
                }

                // draw edit button
                if (EditorUtils.DrawButton("Edit", EditorData.EditColor, 12, new GUILayoutOption[] { GUILayout.MaxWidth(80), GUILayout.Height(50) }))
                {
                    AddTaskWindow<CBSTask>.Show<AddAchievementWindow>(task, TASK_TITLE_ID, ItemAction.EDIT, onEdit =>
                    {
                        var tasks = TasksData.GetTasks();
                        var index = tasks.FindIndex(x => x.ID == task.ID);
                        if (index >= 0)
                        {
                            tasks[index] = onEdit;
                            SaveTasksTable(TasksData);
                        }
                    }, CachedCategories.List, CategoryIndex);
                    GUIUtility.ExitGUI();
                }

                // draw remove button
                if (EditorUtils.DrawButton("Remove", EditorData.RemoveColor, 12, new GUILayoutOption[] { GUILayout.MaxWidth(60), GUILayout.Height(50) }))
                {
                    string questionsText = string.Format("Are you sure you want to remove this {0}?", ItemKey);
                    int option = EditorUtility.DisplayDialogComplex("Warning",
                        questionsText,
                        "Yes",
                        "No",
                        string.Empty);
                    switch (option)
                    {
                        case 0:
                            RemoveTask(task);
                            break;
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginVertical();
                // draw tiers
                if (task.Type == TaskType.TIERED)
                {
                    var tiers = task.TierList;
                    if (tiers != null)
                    {
                        for (int j = 0; j < tiers.Count; j++)
                        {
                            GUILayout.Space(3);
                            GUILayout.BeginHorizontal();

                            var tier = tiers[j];

                            GUILayout.Space(25);
                            GUILayout.Label("Tier " + j.ToString(), tierStyle, GUILayout.Width(50));

                            GUILayout.Space(270);
                            GUILayout.Label(tier.StepsToComplete.ToString(), tierStyle, GUILayout.Width(100));

                            GUILayout.FlexibleSpace();
                            DrawTierTaskRewardsButtons(tier);
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();

                EditorUtils.DrawUILine(Color.grey, 1, 20);

                GUILayout.Space(10);
            }

            GUILayout.EndVertical();
        }

        private void GetTitleData()
        {
            ShowProgress();
            var keys = new List<string> { TitleKeys.AchievementsTitleKey, TitleKeys.AchievementsCategoriesKey };
            var request = new GetTitleDataRequest { Keys = keys };
            PlayFabAdminAPI.GetTitleInternalData(request, OnGetTitleData, OnGetTitleDataFailed);
        }

        private void OnGetTitleData(GetTitleDataResult result)
        {
            HideProgress();
            var dictionary = result.Data;
            bool keyExist = dictionary.ContainsKey(TitleKeys.AchievementsTitleKey);
            var rawData = keyExist ? dictionary[TitleKeys.AchievementsTitleKey] : JsonPlugin.EMPTY_JSON;
            try
            {
                TasksData = JsonPlugin.FromJsonDecompress<AchievementsData>(rawData);
            }
            catch
            {
                TasksData = JsonPlugin.FromJson<AchievementsData>(rawData);
            }
            if (TasksData.GetTasks() == null)
            {
                TasksData.NewInstance();
            }

            if (dictionary.ContainsKey(TitleKeys.AchievementsCategoriesKey))
            {
                var categoriesRawData = dictionary[TitleKeys.AchievementsCategoriesKey];
                CachedCategories = JsonUtility.FromJson<Categories>(categoriesRawData);
                if (!CachedCategories.List.Contains(CBSConstants.UndefinedCategory))
                {
                    CachedCategories.List.Insert(0, CBSConstants.UndefinedCategory);
                }
            }
            else
            {
                CachedCategories = new Categories
                {
                    List = new List<string> { CBSConstants.UndefinedCategory },
                    TitleKey = TitleKeys.AchievementsCategoriesKey
                };
            }
        }

        private void OnGetTitleDataFailed(PlayFabError error)
        {
            AddErrorLog(error);
            HideProgress();
        }

        private void SaveCategories(List<string> categories)
        {
            ShowProgress();
            var categoriesData = new Categories { List = categories, TitleKey = TitleKeys.AchievementsCategoriesKey };
            if (categoriesData.List.Contains(CBSConstants.UndefinedCategory))
            {
                categoriesData.List.Remove(CBSConstants.UndefinedCategory);
            }
            string rawData = JsonUtility.ToJson(categoriesData);
            var request = new SetTitleDataRequest
            {
                Key = TitleKeys.AchievementsCategoriesKey,
                Value = rawData
            };
            PlayFabAdminAPI.SetTitleInternalData(request, OnCategoriesSaved, OnSaveCategoriesFailed);
        }

        private void OnCategoriesSaved(SetTitleDataResult result)
        {
            HideProgress();
            GetTitleData();
        }

        private void OnSaveCategoriesFailed(PlayFabError error)
        {
            AddErrorLog(error);
            HideProgress();
        }

        private void RemoveTask(CBSTask task)
        {
            TasksData.Remove(task);
            SaveTasksTable(TasksData);
        }
    }
}
#endif
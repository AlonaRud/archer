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
    public class CaptainsConfigurator : BaseConfigurator
    {
        protected override string Title => "Captains";

        protected override bool DrawScrollView => false;

        private List<CatalogItem> Captains { get; set; }
        private Categories CaptainCategories { get; set; }
        private string SelectedCategory { get; set; }
        private int CategoryIndex { get; set; }
        private Vector2 CategoryScroll { get; set; }
        private Vector2 CaptainsScroll { get; set; }
        private CaptainIcons Icons { get; set; }
        private LinkedPrefabData LinkedPrefabData { get; set; }
        public CBS.Models.CBSCaptainLevelsContainer Levels { get; set; } = new CBS.Models.CBSCaptainLevelsContainer();
        private EditorData EditorData { get; set; }
        private Rect CategoriesRect = new Rect(0, 55, 150, 620);
        private Rect CaptainsRect = new Rect(150, 115, 935, 600);
        private GUILayoutOption[] AddButtonOptions { get; set; } = { GUILayout.Height(30), GUILayout.Width(140) };
        private bool IsInited { get; set; }

        public override void Init(MenuTitles title)
        {
            base.Init(title);
            Icons = CBSScriptable.Get<CaptainIcons>();
            LinkedPrefabData = CBSScriptable.Get<LinkedPrefabData>();
            EditorData = CBSScriptable.Get<EditorData>();
            AllConfigurator.Add(this);
            InitConfigurator();
        }

        protected override void OnDrawInside()
        {
            if (!IsInited || Captains == null || CaptainCategories == null)
                return;

            DrawCategories();
            DrawCaptains();
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

                var categoriesMenu = CaptainCategories.List.ToList();
                categoriesMenu.Remove(CBSConstants.UndefinedCategory);
                categoriesMenu.Insert(0, "All");
                int categoryHeight = 30;
                var gridRect = new Rect(0, 142, 150, categoryHeight * categoriesMenu.Count);
                var scrollRect = gridRect;
                scrollRect.height += categoryHeight * 4;
                scrollRect.width = 0;
                CategoryScroll = GUI.BeginScrollView(CategoriesRect, CategoryScroll, scrollRect);

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
                        SaveCategories(onModify);
                    }, CaptainCategories);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = oldColor;

                GUILayout.EndVertical();
                GUI.EndScrollView();
            }
        }

        private void DrawCaptains()
        {
            using (var areaScope = new GUILayout.AreaScope(CaptainsRect))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.alignment = TextAnchor.MiddleLeft;
                titleStyle.fontStyle = FontStyle.Bold;
                titleStyle.fontSize = 14;
                GUILayout.Label("ID", titleStyle, GUILayout.Width(100));
                GUILayout.Label("Sprite", titleStyle, GUILayout.Width(118));
                GUILayout.Label("Name", titleStyle, GUILayout.Width(150));
                GUILayout.Label("Max Level", titleStyle, GUILayout.Width(100));
                GUILayout.Label("Max XP", titleStyle, GUILayout.Width(100));
                GUILayout.Label("Custom Data", titleStyle, GUILayout.Width(150));

                if (EditorUtils.DrawButton("Add new captain", EditorData.AddColor, 12, AddButtonOptions))
                {
                    AddCaptainWindow.Show<AddCaptainWindow>(new CatalogItem(), newCaptain =>
                    {
                        AddNewCaptain(newCaptain);
                    }, ItemAction.ADD, CaptainCategories.List, CategoryIndex);
                }
                GUILayout.EndHorizontal();

                EditorUtils.DrawUILine(Color.grey, 2, 20);

                CaptainsScroll = GUILayout.BeginScrollView(CaptainsScroll);

                float cellHeight = 100;

                foreach (var captain in Captains)
                {
                    bool tagExist = captain.Tags != null && captain.Tags.Count != 0;
                    var category = tagExist ? captain.Tags[0] : CBSConstants.UndefinedCategory;

                    if (!string.IsNullOrEmpty(SelectedCategory) && category != SelectedCategory)
                        continue;

                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    EditorGUILayout.LabelField(captain.ItemId, new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(cellHeight) });

                    var activeSprite = Icons.GetSprite(captain.ItemId);
                    var iconTexture = activeSprite == null ? null : activeSprite.texture;
                    GUILayout.Button(iconTexture, GUILayout.Width(cellHeight), GUILayout.Height(cellHeight));

                    GUILayout.Space(20);
                    EditorGUILayout.LabelField(captain.DisplayName, new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(cellHeight) });

                    var levels = Levels.GetLevels(captain.ItemId);
                    string maxLevel = levels != null && levels.Count > 0 ? levels.Max(x => x.Level).ToString() : "N/A";
                    string maxXP = levels != null && levels.Count > 0 ? levels.Max(x => x.XPRequired).ToString() : "N/A";
                    EditorGUILayout.LabelField(maxLevel, new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(cellHeight) });
                    EditorGUILayout.LabelField(maxXP, new GUILayoutOption[] { GUILayout.Width(100), GUILayout.Height(cellHeight) });

                    string customData = string.IsNullOrEmpty(captain.CustomData) ? "None" : "Custom";
                    EditorGUILayout.LabelField(customData, new GUILayoutOption[] { GUILayout.Width(150), GUILayout.Height(cellHeight) });

                    GUILayout.BeginVertical(GUILayout.Height(cellHeight));
                    GUILayout.FlexibleSpace();
                    if (EditorUtils.DrawButton("Edit", EditorData.EditColor, 12, GUILayout.Width(75)))
                    {
                        AddCaptainWindow.Show<AddCaptainWindow>(captain, newCaptain =>
                        {
                            AddNewCaptain(newCaptain);
                        }, ItemAction.EDIT, CaptainCategories.List, CategoryIndex);
                    }
                    if (EditorUtils.DrawButton("Remove", EditorData.RemoveColor, 12, GUILayout.Width(75)))
                    {
                        int option = EditorUtility.DisplayDialogComplex("Warning", "Are you sure you want to remove this captain?", "Yes", "No", string.Empty);
                        if (option == 0)
                        {
                            RemoveCaptain(captain);
                        }
                    }
                    if (EditorUtils.DrawButton("Duplicate", EditorData.DuplicateColor, 12, GUILayout.Width(75)))
                    {
                        EditorInputDialog.Show("Duplicate captain?", "Please enter new ID", captain.ItemId + "_Copy", newId =>
                        {
                            var newCaptain = captain.Duplicate(newId);
                            AddNewCaptain(newCaptain);
                            Levels.Duplicate(captain.ItemId, newId);
                        });
                        GUIUtility.ExitGUI();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();
                    EditorUtils.DrawUILine(Color.grey, 1, 20);
                }

                GUILayout.Space(110);
                GUILayout.EndScrollView();
            }
        }

        private void InitConfigurator()
        {
            ShowProgress();
            GetTitleData(result =>
            {
                OnGetTitleData(result);
                GetCaptainsCatalog(catalogResult =>
                {
                    OnCaptainsCatalogGetted(catalogResult);
                    IsInited = true;
                    HideProgress();
                });
            });
        }

        private void GetTitleData(Action<GetTitleDataResult> finish = null)
        {
            ShowProgress();
            var dataRequest = new GetTitleDataRequest
            {
                Keys = new List<string> { "CaptainCategories", "CaptainLevels" }
            };
            PlayFabAdminAPI.GetTitleInternalData(dataRequest, finish ?? OnGetTitleData, OnGetTitleDataFailed);
        }

        private void OnGetTitleData(GetTitleDataResult result)
        {
            if (result.Data.ContainsKey("CaptainCategories"))
            {
                CaptainCategories = JsonUtility.FromJson<Categories>(result.Data["CaptainCategories"]);
                if (!CaptainCategories.List.Contains(CBSConstants.UndefinedCategory))
                {
                    CaptainCategories.List.Insert(0, CBSConstants.UndefinedCategory);
                }
                CaptainCategories.TitleKey = "CaptainCategories";
            }
            else
            {
                CaptainCategories = new Categories { List = new List<string> { CBSConstants.UndefinedCategory }, TitleKey = "CaptainCategories" };
            }

            if (result.Data.ContainsKey("CaptainLevels"))
            {
                var rawData = result.Data["CaptainLevels"];
                try
                {
                    Levels = JsonPlugin.FromJsonDecompress<CBS.Models.CBSCaptainLevelsContainer>(rawData);
                }
                catch
                {
                    Levels = JsonPlugin.FromJson<CBS.Models.CBSCaptainLevelsContainer>(rawData);
                }
            }
            else
            {
                Levels = new CBS.Models.CBSCaptainLevelsContainer();
            }
            HideProgress();
        }

        private void OnGetTitleDataFailed(PlayFabError error)
        {
            HideProgress();
            AddErrorLog(error);
        }

        private void SaveCategories(Categories categories)
        {
            ShowProgress();
            var list = categories.List.ToList();
            if (list.Contains(CBSConstants.UndefinedCategory))
            {
                list.Remove(CBSConstants.UndefinedCategory);
            }
            categories.List = list;
            string rawData = JsonUtility.ToJson(categories);
            var dataRequest = new SetTitleDataRequest
            {
                Key = categories.TitleKey,
                Value = rawData
            };
            PlayFabAdminAPI.SetTitleInternalData(dataRequest, OnCategoriesSaved, OnSaveDataFailed);
        }

        private void OnCategoriesSaved(SetTitleDataResult result)
        {
            HideProgress();
            GetTitleData();
        }

        private void OnSaveDataFailed(PlayFabError error)
        {
            HideProgress();
            AddErrorLog(error);
        }

        private void GetCaptainsCatalog(Action<GetCatalogItemsResult> finish = null)
        {
            ShowProgress();
            var dataRequest = new GetCatalogItemsRequest
            {
                CatalogVersion = "Captains"
            };
            PlayFabAdminAPI.GetCatalogItems(dataRequest, finish ?? OnCaptainsCatalogGetted, OnGetCatalogFailed);
        }

        private void OnCaptainsCatalogGetted(GetCatalogItemsResult result)
        {
            Captains = result.Catalog.Where(x => x.Tags != null && x.Tags.Contains("Captain")).ToList();
            HideProgress();
        }

        private void OnGetCatalogFailed(PlayFabError error)
        {
            HideProgress();
            AddErrorLog(error);
        }

        private void AddNewCaptain(CatalogItem captain)
        {
            if (!Captains.Any(x => x.ItemId == captain.ItemId))
            {
                Captains.Add(captain);
            }
            SaveCatalog();
        }

        private void RemoveCaptain(CatalogItem captain)
        {
            Captains.Remove(captain);
            Icons.RemoveSprite(captain.ItemId);
            LinkedPrefabData.RemoveAsset(captain.ItemId);
            Levels.RemoveLevels(captain.ItemId);
            SaveCatalog();
        }

        private void SaveCatalog()
        {
            ShowProgress();
            var dataRequest = new UpdateCatalogItemsRequest
            {
                Catalog = Captains,
                CatalogVersion = "Captains"
            };
            PlayFabAdminAPI.UpdateCatalogItems(dataRequest, OnCatalogUpdated, OnCatalogUpdatedFailed);
        }

        private void OnCatalogUpdated(UpdateCatalogItemsResult result)
        {
            HideProgress();
            SaveLevelsData();
        }

        private void OnCatalogUpdatedFailed(PlayFabError error)
        {
            HideProgress();
            AddErrorLog(error);
        }

        private void SaveLevelsData()
        {
            ShowProgress();
            string levelsRawData = JsonPlugin.ToJsonCompress(Levels);
            var levelsRequest = new SetTitleDataRequest
            {
                Key = "CaptainLevels",
                Value = levelsRawData
            };
            PlayFabAdminAPI.SetTitleInternalData(levelsRequest, OnLevelsDataSaved, OnSaveDataFailed);
        }

        private void OnLevelsDataSaved(SetTitleDataResult result)
        {
            HideProgress();
            GetTitleData();
        }
    }
}
#endif
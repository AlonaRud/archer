#if ENABLE_PLAYFABADMIN_API
using CBS.Models;
using CBS.Scriptable;
using PlayFab;
using PlayFab.AdminModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CBS.Editor
{
    public class AddCaptainWindow : EditorWindow
    {
        private static Action<CatalogItem> AddCallback { get; set; }
        private static CatalogItem CurrentData { get; set; }
        private static ItemAction Action { get; set; }
        private static List<string> Categories { get; set; }
        private static int CategoryAtStart { get; set; }

        private string ID { get; set; }
        private string DisplayName { get; set; }
        private string Description { get; set; }
        private string ExternalUrl { get; set; }
        private string ItemCategory { get; set; }
        private string RawCustomData { get; set; }
        private Sprite IconSprite { get; set; }
        private GameObject LinkedPrefab { get; set; }
        private ScriptableObject LinkedScriptable { get; set; }
        private List<CaptainLevelData> Levels { get; set; }
        private int LevelCount { get; set; }

        private Vector2 ScrollPos { get; set; }
        private bool IsInited { get; set; }
        private int SelectedCategoryIndex { get; set; }
        private int SelectedToolBar { get; set; }
        private string[] Titles = new string[] { "Info", "Upgrades", "Linked Data" };
        private string AddTitle = "Add Captain";
        private string SaveTitle = "Save Captain";
        private CaptainIcons Icons { get; set; }
        private LinkedPrefabData PrefabData { get; set; }
        private LinkedScriptableData ScriptableData { get; set; }
        private CBS.Models.CBSCaptainLevelsContainer LevelsContainer { get; set; }
        private List<Type> AllDataTypes { get; set; } = new List<Type>();
        private List<Type> AllLevelDataTypes { get; set; } = new List<Type>();
        private int SelectedTypeIndex { get; set; }
        private int MaxRawBytes = 10000;

        public static void Show<T>(CatalogItem current, Action<CatalogItem> addCallback, ItemAction action, List<string> categories, int categoryIndex) where T : EditorWindow
        {
            AddCallback = addCallback;
            CurrentData = current;
            Action = action;
            Categories = categories;
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
            AllDataTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(CBSItemCustomData))).ToList();

            AllLevelDataTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type == typeof(CBSCaptainLevelCustomData) || type.IsSubclassOf(typeof(CBSCaptainLevelCustomData))).ToList(); // Включаем сам CBSCaptainLevelCustomData

            Icons = CBSScriptable.Get<CaptainIcons>();
            PrefabData = CBSScriptable.Get<LinkedPrefabData>();
            ScriptableData = CBSScriptable.Get<LinkedScriptableData>();
            LevelsContainer = BaseConfigurator.Get<CaptainsConfigurator>().Levels;

            ID = CurrentData.ItemId;
            DisplayName = CurrentData.DisplayName;
            Description = CurrentData.Description;
            ExternalUrl = CurrentData.ItemImageUrl;
            RawCustomData = CurrentData.CustomData;
            try
            {
                RawCustomData = Compressor.Decompress(RawCustomData);
            }
            catch { }
            RawCustomData = JsonPlugin.IsValidJson(RawCustomData) ? RawCustomData : JsonPlugin.EMPTY_JSON;

            bool tagExist = CurrentData.Tags != null && CurrentData.Tags.Count != 0;
            ItemCategory = tagExist ? CurrentData.Tags[0] : CBSConstants.UndefinedCategory;
            if (Action == ItemAction.ADD)
            {
                ItemCategory = Categories == null || Categories.Count == 0 ? CBSConstants.UndefinedCategory : Categories[CategoryAtStart];
            }
            SelectedCategoryIndex = Categories.Contains(ItemCategory) ? Categories.IndexOf(ItemCategory) : 0;
            var itemClassType = AllDataTypes.FirstOrDefault(x => x.Name == "CaptainData") ?? AllDataTypes.FirstOrDefault();
            SelectedTypeIndex = AllDataTypes.IndexOf(itemClassType);

            IconSprite = Icons.GetSprite(ID);
            LinkedPrefab = PrefabData.GetLinkedData(ID);
            LinkedScriptable = ScriptableData.GetLinkedData(ID);

            Levels = LevelsContainer.GetLevels(ID) ?? new List<CaptainLevelData>();
            LevelCount = Levels.Count > 0 ? Levels.Max(x => x.Level) : 1;

            IsInited = true;
        }

        void OnGUI()
        {
            var titleStyle = new GUIStyle(GUI.skin.button);
            using (var areaScope = new GUILayout.AreaScope(new Rect(0, 0, 400, 700)))
            {
                ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);

                SelectedToolBar = GUILayout.Toolbar(SelectedToolBar, Titles, titleStyle, GUI.ToolbarButtonSize.Fixed, GUILayout.Width(380));

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
                        DrawUpgrades();
                        break;
                    case 2:
                        DrawLinkedData();
                        break;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(30);
                string buttonTitle = Action == ItemAction.ADD ? AddTitle : SaveTitle;
                if (GUILayout.Button(buttonTitle))
                {
                    if (IsInputValid())
                    {
                        if (IconSprite == null)
                            Icons.RemoveSprite(ID);
                        else
                            Icons.SaveSprite(ID, IconSprite);

                        if (LinkedPrefab == null)
                            PrefabData.RemoveAsset(ID);
                        else
                            PrefabData.SaveAssetData(ID, LinkedPrefab);

                        if (LinkedScriptable == null)
                            ScriptableData.RemoveAsset(ID);
                        else
                            ScriptableData.SaveAssetData(ID, LinkedScriptable);

                        CheckInputs();
                        LevelsContainer.AddOrUpdateLevelInfo(ID, Levels);
                        AddCallback?.Invoke(CurrentData);
                        Hide();
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawInfo()
        {
            GUILayout.Space(15);
            if (Action == ItemAction.ADD)
            {
                ID = EditorGUILayout.TextField("Captain ID", ID);
            }
            else
            {
                EditorGUILayout.LabelField("Captain ID", ID);
            }
            EditorGUILayout.HelpBox("Unique ID for captain.", MessageType.Info);

            GUILayout.Space(15);
            DisplayName = EditorGUILayout.TextField("Name", DisplayName);
            EditorGUILayout.HelpBox("Full name of the captain.", MessageType.Info);

            GUILayout.Space(15);
            var descriptionTitle = new GUIStyle(GUI.skin.textField);
            descriptionTitle.wordWrap = true;
            EditorGUILayout.LabelField("Description");
            Description = EditorGUILayout.TextArea(Description, descriptionTitle, new GUILayoutOption[] { GUILayout.Height(150) });

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Category");
            SelectedCategoryIndex = EditorGUILayout.Popup(SelectedCategoryIndex, Categories.ToArray());
            EditorGUILayout.HelpBox("Captain category.", MessageType.Info);

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Custom Data", EditorStyles.boldLabel);
            try
            {
                RawCustomData = Compressor.Decompress(RawCustomData);
            }
            catch { }
            RawCustomData = JsonPlugin.IsValidJson(RawCustomData) ? RawCustomData : JsonPlugin.EMPTY_JSON;
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(RawCustomData);
            float difValue = (float)byteCount / (float)MaxRawBytes;
            string progressTitle = byteCount.ToString() + "/" + MaxRawBytes.ToString() + " bytes";
            float lastY = GUILayoutUtility.GetLastRect().y;
            var lastColor = GUI.color;
            if (byteCount > MaxRawBytes)
            {
                GUI.color = Color.red;
            }
            EditorGUI.ProgressBar(new Rect(3, lastY + 25, position.width - 6, 20), difValue, progressTitle);
            GUI.color = lastColor;

            GUILayout.Space(35);
            if (AllDataTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No custom data types available for captain.", MessageType.Warning);
            }
            else
            {
                SelectedTypeIndex = EditorGUILayout.Popup(SelectedTypeIndex, AllDataTypes.Select(x => x.Name).ToArray());
                var selectedType = AllDataTypes[SelectedTypeIndex];
                var dataObject = JsonUtility.FromJson(RawCustomData, selectedType) ?? Activator.CreateInstance(selectedType);

                foreach (var f in selectedType.GetFields().Where(f => f.IsPublic))
                {
                    if (f.FieldType == typeof(string))
                    {
                        string stringTitle = f.Name;
                        string stringValue = f.GetValue(dataObject)?.ToString() ?? string.Empty;
                        var text = EditorGUILayout.TextField(stringTitle, stringValue);
                        f.SetValue(dataObject, text);
                    }
                    else if (f.FieldType == typeof(int))
                    {
                        string stringTitle = f.Name;
                        int intValue = (int)(f.GetValue(dataObject) ?? 0);
                        var iValue = EditorGUILayout.IntField(stringTitle, intValue);
                        f.SetValue(dataObject, iValue);
                    }
                    else if (f.FieldType == typeof(float))
                    {
                        string stringTitle = f.Name;
                        float floatValue = (float)(f.GetValue(dataObject) ?? 0f);
                        var fl = EditorGUILayout.FloatField(stringTitle, floatValue);
                        f.SetValue(dataObject, fl);
                    }
                }

                RawCustomData = JsonUtility.ToJson(dataObject);
                try
                {
                    RawCustomData = Compressor.Compress(RawCustomData);
                }
                catch { }
            }

            EditorGUILayout.HelpBox("Enter custom data for captain characteristics (e.g., ResearchPower, ResourceGathering). Levels are configured in Upgrades tab.", MessageType.Info);
        }

        private void DrawUpgrades()
        {
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Level Upgrades", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Define XP required and custom data for each level.", MessageType.Info);

            GUILayout.Space(10);
            LevelCount = EditorGUILayout.IntField("Number of Levels", LevelCount);
            if (LevelCount < 1) LevelCount = 1;

            if (Levels == null)
                Levels = new List<CaptainLevelData>();
            while (Levels.Count < LevelCount)
                Levels.Add(new CaptainLevelData { Level = Levels.Count + 1 });
            while (Levels.Count > LevelCount)
                Levels.RemoveAt(Levels.Count - 1);

            for (int i = 0; i < Levels.Count; i++)
            {
                var levelData = Levels[i];
                GUILayout.Space(5);
                EditorGUILayout.LabelField($"Level {levelData.Level}", EditorStyles.boldLabel);

                string xpInput = EditorGUILayout.TextField("XP Required", levelData.XPRequired.ToString());
                long.TryParse(xpInput, out levelData.XPRequired);
                if (levelData.XPRequired < 0) levelData.XPRequired = 0;

                GUILayout.Space(5);
                EditorGUILayout.LabelField("Level Custom Data", EditorStyles.boldLabel);
                string customData = levelData.CustomRawData;
                try
                {
                    customData = Compressor.Decompress(customData);
                }
                catch { }
                customData = JsonPlugin.IsValidJson(customData) ? customData : JsonPlugin.EMPTY_JSON;
                var customDataType = AllLevelDataTypes.FirstOrDefault(x => x.Name == levelData.CustomDataClassName) ?? AllLevelDataTypes.FirstOrDefault();
                int selectedLevelTypeIndex = AllLevelDataTypes.Count > 0 ? AllLevelDataTypes.IndexOf(customDataType) : -1;
                if (AllLevelDataTypes.Count == 0)
                {
                    EditorGUILayout.HelpBox("No custom data types available for level.", MessageType.Warning);
                }
                else
                {
                    selectedLevelTypeIndex = EditorGUILayout.Popup(selectedLevelTypeIndex, AllLevelDataTypes.Select(x => x.Name).ToArray());
                    customDataType = AllLevelDataTypes[selectedLevelTypeIndex];
                    levelData.CustomDataClassName = customDataType.Name;
                    var customDataObject = JsonUtility.FromJson(customData, customDataType) ?? Activator.CreateInstance(customDataType);

                    foreach (var f in customDataType.GetFields().Where(f => f.IsPublic))
                    {
                        if (f.FieldType == typeof(string))
                        {
                            string stringTitle = f.Name;
                            string stringValue = f.GetValue(customDataObject)?.ToString() ?? string.Empty;
                            var text = EditorGUILayout.TextField(stringTitle, stringValue);
                            f.SetValue(customDataObject, text);
                        }
                        else if (f.FieldType == typeof(int))
                        {
                            string stringTitle = f.Name;
                            int intValue = (int)(f.GetValue(customDataObject) ?? 0);
                            var fieldValue = EditorGUILayout.IntField(stringTitle, intValue);
                            f.SetValue(customDataObject, fieldValue);
                        }
                        else if (f.FieldType == typeof(float))
                        {
                            string stringTitle = f.Name;
                            float floatValue = (float)(f.GetValue(customDataObject) ?? 0f);
                            var fl = EditorGUILayout.FloatField(stringTitle, floatValue);
                            f.SetValue(customDataObject, fl);
                        }
                    }
                    levelData.CustomRawData = JsonUtility.ToJson(customDataObject);
                    if (levelData.CompressCustomData)
                    {
                        try
                        {
                            levelData.CustomRawData = Compressor.Compress(levelData.CustomRawData);
                        }
                        catch { }
                    }
                }
            }

            GUILayout.Space(10);
            if (EditorUtils.DrawButton("Add Level", Color.green, 12))
            {
                LevelCount++;
                Levels.Add(new CaptainLevelData { Level = LevelCount });
            }
            if (Levels.Count > 1 && EditorUtils.DrawButton("Remove Last Level", Color.red, 12))
            {
                LevelCount--;
                Levels.RemoveAt(Levels.Count - 1);
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
            EditorGUILayout.HelpBox("Sprite for captain. Stored locally in build.", MessageType.Info);

            var iconTexture = IconSprite == null ? null : IconSprite.texture;
            GUILayout.Button(iconTexture, GUILayout.Width(100), GUILayout.Height(100));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Prefab", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            LinkedPrefab = (GameObject)EditorGUILayout.ObjectField((LinkedPrefab as UnityEngine.Object), typeof(GameObject), false, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            EditorGUILayout.HelpBox("3D model prefab for captain. Stored locally in build.", MessageType.Info);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("ScriptableObject", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            LinkedScriptable = (ScriptableObject)EditorGUILayout.ObjectField((LinkedScriptable as UnityEngine.Object), typeof(ScriptableObject), false, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            EditorGUILayout.HelpBox("Scriptable data for captain. Stored locally in build.", MessageType.Info);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("External Icon URL", titleStyle, new GUILayoutOption[] { GUILayout.MaxWidth(150) });
            ExternalUrl = EditorGUILayout.TextField(ExternalUrl);
            EditorGUILayout.HelpBox("Optional remote texture URL.", MessageType.Info);
        }

        private void CheckInputs()
        {
            CurrentData.ItemId = ID;
            CurrentData.DisplayName = DisplayName;
            CurrentData.Description = Description;
            CurrentData.ItemImageUrl = ExternalUrl;
            CurrentData.CustomData = RawCustomData;

            if (!string.IsNullOrEmpty(ItemCategory))
            {
                if (CurrentData.Tags == null)
                {
                    CurrentData.Tags = new List<string>();
                }
                ItemCategory = Categories[SelectedCategoryIndex];
                if (CurrentData.Tags.Count == 0)
                {
                    CurrentData.Tags.Add(ItemCategory);
                    CurrentData.Tags.Add("Captain");
                }
                else
                {
                    CurrentData.Tags[0] = ItemCategory;
                    if (!CurrentData.Tags.Contains("Captain"))
                        CurrentData.Tags.Add("Captain");
                }
            }
        }

        private bool IsInputValid()
        {
            int byteCount = System.Text.Encoding.UTF8.GetByteCount(RawCustomData);
            return byteCount < MaxRawBytes && !string.IsNullOrEmpty(ID);
        }
    }
}
#endif
       

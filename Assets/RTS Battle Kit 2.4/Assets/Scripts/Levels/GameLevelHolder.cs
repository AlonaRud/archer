using UnityEngine;
using CBS;
using CBS.Models;
using UnityEngine.Events;
using System.Collections.Generic;
namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/GameLevelHolder")]
    public class GameLevelHolder : SingletonScriptableObject<GameLevelHolder>
    {
        [SerializeField]
        private string saveKey = "TopPassedLevel"; // Ключ для CBS UserData

        private static bool loaded = false;
        private static int _count;
       

        private CBSProfileModule ProfileModule => CBSModule.Get<CBSProfileModule>();

        public static int TopPassedLevel
        {
            get { if (!loaded) Instance.Load(); return _count; }
            private set { _count = value; }
        }

        public static int CurrentLevel;

        public UnityEvent<int> ChangePassedEvent;
        public UnityEvent<int> LoadEvent;
        public UnityEvent<int> PassLevelEvent;
        public UnityEvent<int> StartLevelEvent;

        private void Awake()
        {
            Load();
            Debug.Log("Awake: " + this + " ;top passed level: " + TopPassedLevel);
            CurrentLevel = 0;
        }

        public void Load()
        {
            loaded = true;
            // Загрузка из CBS UserData
            ProfileModule.GetProfileData(saveKey, result =>
            {
                if (result.IsSuccess && result.Data.ContainsKey(saveKey))
                {
                    TopPassedLevel = int.Parse(result.Data[saveKey].Value);
                }
                else
                {
                    TopPassedLevel = -1; // По умолчанию
                }
                LoadEvent?.Invoke(TopPassedLevel);
            });
        }

        public void Save()
        {
            // Сохранение в CBS UserData
            ProfileModule.SaveProfileData(saveKey, TopPassedLevel.ToString(), result =>
            {
                if (result.IsSuccess)
                {
                    Debug.Log("Saved TopPassedLevel: " + TopPassedLevel);
                }
                else
                {
                    Debug.LogError("Failed to save: " + result.Error?.Message);
                }
            });
        }

        public void SetDefaultData()
        {
            TopPassedLevel = -1;
            Save();
            ChangePassedEvent?.Invoke(TopPassedLevel);
        }

        public void PassLevel()
        {
            if (CurrentLevel > TopPassedLevel)
            {
                TopPassedLevel = CurrentLevel;
                ChangePassedEvent?.Invoke(TopPassedLevel);
            }
            Save();
            PassLevelEvent?.Invoke(CurrentLevel);
        }

        public static void StartLevel()
        {
            if (Instance) Instance.StartLevelEvent?.Invoke(CurrentLevel);
        }
    }
}

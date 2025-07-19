using UnityEngine;
using System.Collections.Generic;
using CBS.Models;

namespace CBS.Example
{
    public class EquipmentBonusManager : MonoBehaviour
    {
        private ICBSInventory inventory;

        private void Awake()
        {
            inventory = CBSModule.Get<CBSInventoryModule>();
            inventory.OnItemEquiped += OnItemEquipped;
            inventory.OnItemUnEquiped += OnItemUnEquipped;
        }

        private void OnDestroy()
        {
            inventory.OnItemEquiped -= OnItemEquipped;
            inventory.OnItemUnEquiped -= OnItemUnEquipped;
        }

        private void OnItemEquipped(CBSInventoryItem item)
        {
            // Получаем ID капитана, на которого надета экипировка (временно через пример)
            string captainID = "guard_cap"; // Позже это будет из UI или CaptainEquipment
            ApplyBonuses(item, captainID, true);
        }

        private void OnItemUnEquipped(CBSInventoryItem item)
        {
            // Получаем ID капитана
            string captainID = "guard_cap"; // Позже это будет из UI или CaptainEquipment
            ApplyBonuses(item, captainID, false);
        }

        private void ApplyBonuses(CBSInventoryItem item, string captainID, bool apply)
        {
            // Читаем Custom Data экипировки
            string customData = item.CustomData ?? "{}";
            var dataObject = JsonUtility.FromJson<EquipmentCustomData>(customData);

            // Читаем текущие данные капитана из Title Data
            var captainData = GetCaptainData();
            int captainIndex = captainData.CaptainIDs.IndexOf(captainID);
            if (captainIndex < 0) return;

            // Применяем или убираем бонусы
            float multiplier = apply ? 1f : -1f;

            // Обновляем характеристики (лидерство, мастерство, превосходство, могущество)
            captainData.CaptainStats[captainIndex * 4] += dataObject.BonusLeadership * multiplier;
            captainData.CaptainStats[captainIndex * 4 + 1] += dataObject.BonusMastery * multiplier;
            captainData.CaptainStats[captainIndex * 4 + 2] += dataObject.BonusSuperiority * multiplier;
            captainData.CaptainStats[captainIndex * 4 + 3] += dataObject.BonusMight * multiplier;

            // Обновляем особенности (например, guard_health)
            string features = captainData.CaptainFeatures[captainIndex];
            Dictionary<string, float> featureDict = ParseFeatures(features);
            featureDict["guard_health"] = featureDict.GetValueOrDefault("guard_health", 0) + dataObject.BonusGuardHealth * multiplier;
            featureDict["guard_strength"] = featureDict.GetValueOrDefault("guard_strength", 0) + dataObject.BonusGuardStrength * multiplier;
            captainData.CaptainFeatures[captainIndex] = FeaturesToString(featureDict);

            // Сохраняем обновленные данные капитана
            SaveCaptainData(captainData);
        }

        private CaptainData GetCaptainData()
        {
            // Заглушка: читаем из Title Data через CBS API
            // Реально нужно использовать CBS API, например, CBSModule.GetTitleData
            return new CaptainData
            {
                CaptainIDs = new List<string> { "guard_cap", "rider_cap" },
                CaptainStats = new List<float> { 10, 20, 30, 40, 15, 25, 35, 45 },
                CaptainFeatures = new List<string> { "guard_health:50,guard_strength:60", "rider_health:70,rider_strength:80" }
            };
        }

        private void SaveCaptainData(CaptainData data)
        {
            // Заглушка: сохраняем в Title Data через CBS API
            // Реально нужно использовать CBSModule.SetTitleData
            Debug.Log("Сохранение CaptainData: " + JsonUtility.ToJson(data));
        }

        private Dictionary<string, float> ParseFeatures(string features)
        {
            var dict = new Dictionary<string, float>();
            if (string.IsNullOrEmpty(features)) return dict;
            var pairs = features.Split(',');
            foreach (var pair in pairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && float.TryParse(parts[1], out float value))
                {
                    dict[parts[0]] = value;
                }
            }
            return dict;
        }

        private string FeaturesToString(Dictionary<string, float> features)
        {
            var pairs = new List<string>();
            foreach (var kvp in features)
            {
                pairs.Add($"{kvp.Key}:{kvp.Value}");
            }
            return string.Join(",", pairs);
        }
    }

    // Вспомогательный класс для Custom Data экипировки
    public class EquipmentCustomData
    {
        public float BonusLeadership;
        public float BonusMastery;
        public float BonusSuperiority;
        public float BonusMight;
        public float BonusGuardHealth;
        public float BonusGuardStrength;
    }
}

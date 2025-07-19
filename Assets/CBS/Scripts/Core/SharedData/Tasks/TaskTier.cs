namespace CBS.Models
{
    using System;
    using System.Collections.Generic;

    [System.Serializable]
    public class TaskTier
    {
        public int Index;
        public int StepsToComplete;
        public int CurrentSteps;
        public bool OverrideDescription;
        public string Description;
        public CBS.RewardObject Reward;
        public ProfileEventContainer Events;
        public CBS.RewardObject AdditionalReward;
        public ClanEventContainer ClanEvents;
        public int StudyHours { get; set; }
        public int StudyMinutes { get; set; }
        public List<Dependency> Dependencies;
        public int Cost; // Новое поле для стоимости
        public string CurrencyCode; // Новое поле для кода валюты
        public List<ResourceCost> ResourceCosts; // Список ресурсов (предметов)
    public string ProductionResourceID; // ID производимого ресурса (валюта или предмет)
    public int ProductionRate; // Количество/час
    public string BonusType; //

        public bool AddPoints(int points, int currentSteps)
        {
            currentSteps += points;
            if (currentSteps >= StepsToComplete)
                return true;
            return false;
        }
    }

    [System.Serializable]
    public struct Dependency
    {
        public string TechID;
        public int Level;
    }
    [System.Serializable]
    public struct ResourceCost
    {
        public string ItemId; // ID предмета из CatalogItem (WOOD, STONE)
        public int Amount; // Количество
    }

}

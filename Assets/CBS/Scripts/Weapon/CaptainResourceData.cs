using System;
using CBS.Core;

namespace CBS.Models
{
    [Serializable]
    public class CaptainResourceData : CBSItemCustomData
    {
        public float Leadership; // Лидерство (%)
        public float Mastery; // Мастерство (%)
        public float Superiority; // Превосходство (%)
        public float Might; // Могущество (%)
        public float ResourceGatheringSpeed; // Скорость добычи ресурсов (%)
        public float ArmyCarryingCapacity; // Грузоподъемность армии (%)
        public string TroopType; // Тип войск
    }
}

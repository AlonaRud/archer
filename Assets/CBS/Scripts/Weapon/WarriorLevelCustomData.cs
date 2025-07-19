using System;
using CBS.Core;

namespace CBS.Models
{
    [Serializable]
    public class WarriorLevelCustomData : CBSCaptainLevelCustomData
    {
        public float Leadership; // Лидерство (%)
        public float Mastery; // Мастерство (%)
        public float Superiority; // Превосходство (%)
        public float Might; // Могущество (%)
        public float StrengthBonus; // Бонус силы (%)
        public float HealthBonus; // Бонус здоровья (%)
    }
}

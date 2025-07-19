using System;
using CBS.Core;

namespace CBS.Models
{
    [Serializable]
    public class CaptainResearchData : CBSItemCustomData
    {
        public float Leadership; // Лидерство (%)
        public float Mastery; // Мастерство (%)
        public float Superiority; // Превосходство (%)
        public float Might; // Могущество (%)
        public float ResearchEfficiency; // Эффективность исследований (%)
        
        public float OilReserve; // Запас масла (%)
        public string TroopType; // Тип войск
    }
}
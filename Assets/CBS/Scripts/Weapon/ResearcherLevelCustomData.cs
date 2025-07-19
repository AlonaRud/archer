using System;
using CBS.Core;

namespace CBS.Models
{
    [Serializable]
    public class ResearcherLevelCustomData : CBSCaptainLevelCustomData
    {
        public float Leadership; // Лидерство (%)
        public float Mastery; // Мастерство (%)
        public float Superiority; // Превосходство (%)
        public float Might; // Могущество (%)
        public float ResearchEfficiency; // Эффективность исследований (%)
        public float TreasuryProtection; // Сохранность сокровищниц (%)
        public float OilReserve; // Запас масла (%)
    }
}

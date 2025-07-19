using CBS.Models;

namespace CBS
{
    public class EconomyEquipmentUpgradeData : CBSUpgradeItemCustomData
    {
        public int ResearchEfficiency; // Эффективность исследования
        public int OilReserve; // Запас масла
        public int OilProduction; // Производство масла
        public int Durability; // Прочность
    }
}

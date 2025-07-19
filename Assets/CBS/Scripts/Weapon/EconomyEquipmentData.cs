using CBS.Models;

namespace CBS
{
    public class EconomyEquipmentData : CBSItemCustomData
    {
        public int ResearchEfficiency; // Эффективность исследования
        public int OilReserve; // Запас масла
        public int OilProduction; // Производство масла
        public int Durability; // Прочность
        public string SlotType; // Тип слота (Helmet, Weapon и т.д.)
        public string SubCategory; // Подкатегория (Engineers, Mages и т.д.)
    }
}

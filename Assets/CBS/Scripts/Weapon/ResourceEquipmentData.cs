using CBS.Models;

namespace CBS
{
    public class ResourceEquipmentData : CBSItemCustomData
    {
        public int ResourceGatherSpeed; // Скорость сбора ресурсов
        public int CarryingCapacity; // Грузоподъемность
        public int MarchSpeed; // Скорость марша
        public int Durability; // Прочность
        public string SlotType; // Тип слота (Helmet, Weapon и т.д.)
        public string SubCategory; // Подкатегория (Engineers, Mages и т.д.)
    }
}

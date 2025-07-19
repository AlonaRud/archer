using CBS.Models;

namespace CBS
{
    public class ResourceEquipmentUpgradeData : CBSUpgradeItemCustomData
    {
        public int ResourceGatherSpeed; // Скорость сбора ресурсов
        public int CarryingCapacity; // Грузоподъемность
        public int MarchSpeed; // Скорость марша
        public int Durability; // Прочность
    }
}

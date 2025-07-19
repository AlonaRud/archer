namespace CBS.Example
{
    public class WeaponData : CBSItemCustomData
    {
        public float StrengthBonus; // Бонус силы в процентах (например, 5.0 для +5%)
        public float HealthBonus;   // Бонус здоровья в процентах (например, 3.0 для +3%)
        public int Durability;     // Прочность (бои)
        public float DoubleDamageChance; // Шанс двойного урона (%)
        public string SlotType;    // Тип слота (Helmet, Weapon и т.д.)
        public string SubCategory; // Подкатегория (Engineers, Mages и т.д.)
        public string TargetUnitType; // Тип войск (Engineers, Mages, Cavalry, Archers, Swordsmen, Beasts, Dragons)
    }
}
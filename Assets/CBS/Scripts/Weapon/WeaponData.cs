namespace CBS.Example
{
    public class WeaponData : CBSItemCustomData
    {
        public int Strength; // Сила
        public int Health; // Здоровье
        public int Durability; // Прочность (бои)
        public float DoubleDamageChance; // Шанс двойного урона (%)
        public string SlotType; // Тип слота (Helmet, Weapon и т.д.)
        public string SubCategory; // Подкатегория (Engineers, Mages и т.д.)
    }
}

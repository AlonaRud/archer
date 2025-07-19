#if ENABLE_PLAYFABADMIN_API
using UnityEngine;

namespace CBS.Scriptable
{
    [CreateAssetMenu(fileName = "TasksCategoryPrefabs", menuName = "CBS/Add new Tasks Category Prefabs")]
    public class TasksCategoryPrefabs : CBSScriptable
    {
        public override string ResourcePath => "Scriptable/TasksCategoryPrefabs";

        // Общие префабы для заданий
        
        public GameObject TechInfo; // Префаб информации о технологии
        public GameObject TechWindow; // Префаб окна технологии

        // Префабы для категорий
        public GameObject MilitaryAffairsCategory; // Префаб для категории "Military Affairs"
        public GameObject EconomyCategory; // Префаб для категории "Economy"
        public GameObject BlacksmithingCategory; // Префаб для категории "Blacksmithing"
        public GameObject TreasuriesCategory; // Префаб для категории "Treasuries"
        public GameObject MagicalCreaturesCategory; // Префаб для категории "Magical Creatures"
        public GameObject WorldMapCategory; // Префаб для категории "World Map"
    }
}
#endif

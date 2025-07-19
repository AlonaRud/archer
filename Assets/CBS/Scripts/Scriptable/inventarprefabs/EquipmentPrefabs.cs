using UnityEngine;

namespace CBS.Scriptable
{
    [CreateAssetMenu(fileName = "EquipmentPrefabs", menuName = "CBS/Add new Equipment Prefabs")]
    public class EquipmentPrefabs : CBSScriptable
    {
        public override string ResourcePath => "Scriptable/EquipmentPrefabs";

        public GameObject EquipmentInventory; // Префаб для инвентаря экипировки
    }
}

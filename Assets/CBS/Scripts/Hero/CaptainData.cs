using System.Collections.Generic;

namespace CBS.Example
{
    public class CaptainData : TitleCustomData
    {
        public List<string> CaptainIDs; // ["guard_cap", "rider_cap"]
        public List<int> CaptainLevels; // [1, 1]
        public List<float> CaptainExp; // [0, 0]
        public List<float> ExpPerLevel; // [1000, 1500, 2000]
        public List<float> CaptainStats; // [10, 20, 30, 40, 15, 25, 35, 45] (лидерство, мастерство, превосходство, могущество)
        public List<string> CaptainFeatures; // ["guard_health:50,guard_strength:60", "rider_health:70,rider_strength:80"]
        public List<string> IconPaths; // ["Sprites/GuardIcon", "Sprites/RiderIcon"]
    }
}

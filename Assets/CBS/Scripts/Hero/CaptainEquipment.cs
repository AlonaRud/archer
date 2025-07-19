using System.Collections.Generic;
namespace CBS.Example
{
    public class CaptainEquipment : TitleCustomData
    {
        public List<string> CaptainIDs; // ["guard_cap", "rider_cap"]
        public List<string> EquippedItems; // ["Guard_Armor", "Rider_Helmet"]
    }
}

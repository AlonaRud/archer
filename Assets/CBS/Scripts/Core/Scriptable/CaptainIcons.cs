using UnityEngine;

namespace CBS.Scriptable
{
    [CreateAssetMenu(fileName = "CaptainIcons", menuName = "CBS/Add new Captains Sprite pack")]
    public class CaptainIcons : IconsData
    {
        public override string ResourcePath => "CaptainIcons";

        public override string EditorPath => "Assets/CBS_External/Resources";

        public override string EditorAssetName => "CaptainIcons.asset";
    }
}

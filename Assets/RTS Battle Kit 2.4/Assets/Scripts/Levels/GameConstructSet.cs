using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Mkey
{
    [CreateAssetMenu(menuName = "Mkey/GameConstructSet")]
    public class GameConstructSet : BaseScriptable
    {
        [SerializeField]
        private List<LevelConstructSet> levelSets; // Список уровней

        public int LevelCount => levelSets != null ? levelSets.Count : 0;

        public LevelConstructSet GetLevelConstructSet(int level)
        {
            if (levelSets != null && level >= 0 && level < levelSets.Count)
                return levelSets[level];
            return levelSets != null ? levelSets[levelSets.Count - 1] : null;
        }

        public void AddLevel(LevelConstructSet level)
        {
            if (levelSets == null) levelSets = new List<LevelConstructSet>();
            levelSets.Add(level);
            SetAsDirty();
        }

        public void RemoveLevel(int index)
        {
            if (levelSets != null && index >= 0 && index < levelSets.Count)
            {
                levelSets.RemoveAt(index);
                SetAsDirty();
            }
        }

        public void Clean()
        {
            if (levelSets != null)
            {
                levelSets = levelSets.Where(item => item != null).ToList();
                SetAsDirty();
            }
        }
    }
}

using System.Collections.Generic;

namespace CBS.Models
{
    public class CBSCaptainLevelsContainer
    {
        public Dictionary<string, List<CaptainLevelData>> Levels;

        public bool HasLevels(string captainID)
        {
            if (Levels == null)
                return false;
            return Levels.ContainsKey(captainID);
        }

        public List<CaptainLevelData> GetLevels(string captainID)
        {
            if (Levels == null)
                return null;
            try
            {
                return Levels[captainID];
            }
            catch
            {
                return null;
            }
        }

        public void AddOrUpdateLevelInfo(string captainID, List<CaptainLevelData> levelInfo)
        {
            if (Levels == null)
                Levels = new Dictionary<string, List<CaptainLevelData>>();
            Levels[captainID] = levelInfo;
        }

        public void RemoveLevels(string captainID)
        {
            if (Levels == null || string.IsNullOrEmpty(captainID))
                return;
            if (Levels.ContainsKey(captainID))
            {
                Levels.Remove(captainID);
            }
        }

        public void Duplicate(string fromCaptainID, string toCaptainID)
        {
            if (Levels == null)
                return;
            if (Levels.TryGetValue(fromCaptainID, out var levelData))
            {
                Levels[toCaptainID] = levelData;
            }
        }
    }
}

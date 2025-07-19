using System;
using System.Collections.Generic;
using UnityEngine;
using CBS.Core;

namespace CBS.Models
{
    [Serializable]
    public class CaptainData : CBSItemCustomData
    {
        public int ResearchPower;
        public float ResourceGathering;
    }

    [Serializable]
    public class CaptainLevelData : ICustomData<CBSCaptainLevelCustomData>
    {
        public int Level;
        public long XPRequired;
        public string CustomDataClassName { get; set; }
        public string CustomRawData { get; set; }
        public bool CompressCustomData => true;

        public T GetCustomData<T>() where T : CBSCaptainLevelCustomData
        {
            if (CompressCustomData)
                return JsonPlugin.FromJsonDecompress<T>(CustomRawData);
            else
                return JsonPlugin.FromJson<T>(CustomRawData);
        }
    }

    [Serializable]
    public class CBSCaptainLevelCustomData : CBSBaseCustomData // Наследуем от CBSBaseCustomData
    {
        public int ResearchPower;
        public float ResourceGathering;
    }
}

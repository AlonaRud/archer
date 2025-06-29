using UnityEngine;

namespace Mkey
{
    public class BaseScriptable : ScriptableObject
    {
        protected void SetAsDirty()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}

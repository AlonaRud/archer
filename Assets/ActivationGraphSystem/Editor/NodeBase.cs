using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// One node is represented by this class in the node editor.
    /// </summary>
    public class NodeBase : ScriptableObject {
        public Rect NodeRect;
        public ActivationGraphEditor TheEditor;
        public BaseNode TheBaseNode;
        public float Width = 100;
        public float height = 100;
        public bool IsSelected = false;
    }
}


using UnityEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Collections;

namespace ActivationGraphSystem {

    /// <summary>
    /// Base node for all task nodes.
    /// </summary>
    public class BaseNode : MonoBehaviour {

        // Coordinate for the node editor.
        public Vector2 PosInNodeEditor = new Vector2(10000, 10000);
        // For the editor. Determines the visibility of this node in the editor.
        public bool IsHidden = false;

        // Expand this enum for differentiating the type of the tasks.
        // This is needed for e.g. to just show up in a specific UI the
        // right types of tasks. E.g. in a tech tree, just the tech tree
        // typed task nodes will be shown. The colors for the editor are 
        // defined in the ActivationGraphManager.
        public enum Types:int {
            None, MissionNode, TechNode, BuildNode, CraftingNode, UnitNode, SkillNode
        }
        [Header("Extend the enum with your own types.")]
        public Types Type = Types.None;

        // Select the instantiated script.
        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtActivate;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtActivate;
        protected Action<BaseNode> activateAction;

        // The ActivationGraphManager will set itself in the awake method.
        public ActivationGraphManager AGM;


        /// <summary>
        /// Creates the actions.
        /// </summary>
        protected virtual void Awake() {
            // Activate the trigger.
            if (ScriptAtActivate != null && !string.IsNullOrEmpty(MethodNameAtActivate)) {
                MethodInfo mi = ScriptAtActivate.GetType().GetMethod(MethodNameAtActivate);
                if (mi == null) {
                    Debug.LogError("Method '" + MethodNameAtActivate + "' cannot be called in " + ScriptAtActivate + " on node: " + name);
                }
                activateAction = (Action<BaseNode>)Delegate.CreateDelegate(typeof(Action<BaseNode>), ScriptAtActivate, mi);
                if (activateAction == null) {
                    Debug.LogError("Failed creating action!");
                }
            }
        }

        /// <summary>
        /// This method is called after the task reaches its end state (Sucess or Failure).
        /// </summary>
        public virtual bool PostProcess() {
            return false;
        }

        /// <summary>
        /// Returns the short description.
        /// </summary>
        /// <returns></returns>
        public virtual string GetShortDescription() {
            return "TaskNode";
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public virtual void Reset() {
            // Do nothing.
        }

    }

}

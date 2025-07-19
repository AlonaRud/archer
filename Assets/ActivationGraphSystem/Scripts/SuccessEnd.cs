using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the victory node.
    /// </summary>
    public class SuccessEnd : TaskBase {

        [TextArea(3, 3)]
        public string ShortDescription = "Success End";
        public bool IsActivated = false;
        [Header("Also shuts down the task system.")]
        public bool StopTaskSystem = true;


        /// <summary>
        /// Triggers the activate action and shuts down the task system if StopTaskSystem is true.
        /// </summary>
        public override bool StartTask(bool force = false) {
            IsActivated = true;

            if (activateAction != null) {
                activateAction(this);
            }

            if (StopTaskSystem) {
                AGM.ShutDown();
            }

            return true;
        }

        /// <summary>
        /// Overrides StopTask with empty implementation.
        /// </summary>
        override public void StopTask() {
        }

        /// <summary>
        /// Returns the short description.
        /// </summary>
        /// <returns></returns>
        public override string GetShortDescription() {
            return ShortDescription;
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            IsActivated = false;
        }
    }
}

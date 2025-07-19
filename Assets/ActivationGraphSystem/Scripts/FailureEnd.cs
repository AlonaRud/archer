using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the failure node.
    /// </summary>
    public class FailureEnd : TaskBase {

        [TextArea(3, 3)]
        public string ShortDescription = "Failure End";
        public bool IsActivated = false;
        [Header("Also shuts down the task system.")]
        public bool StopTaskSystem = true;


        /// <summary>
        /// Trigger the activate action and shut down the task system if StopTaskSystem is true.
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

        public override void SetStateExternal(Task.TaskResult state) {
            switch (state) {
                case TaskResult.InactiveType:
                    IsActivated = false;
                    AGM.TaskDisabled(this);
                    break;
                case TaskResult.RunningType:
                    IsActivated = false;
                    StartTask(true);
                    break;
                case TaskResult.SuccessType:
                    StartTask();
                    AGM.TaskSuccessed(this);
                    break;
                case TaskResult.FailureType:
                    StartTask();
                    AGM.TaskFailed(this);
                    break;
                case TaskResult.DisabledType:
                    AGM.TaskDisabled(this);
                    break;
                default:
                    break;
            }
        }
    }
}

using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the task node with all the descriptions. This is the only one node,
    /// which informations should be visible for the user. E.g. the mission dialog contains 
    /// just this kind of nodes.
    /// </summary>
    public class TaskDataNode : Task {

        [TextArea(3, 2)]
        public string TaskName = "Task name";
        // For right up side, preview.
        [TextArea(3, 3)]
        public string TaskDescShort = "Task Short description";
        [TextArea(3, 10)]
        public string TaskDesc = "Task Description";
        [TextArea(3, 10)]
        public string TaskSuccDesc = "Task Completed!";
        [TextArea(3, 10)]
        public string TaskFailDesc = "Task failed!";

        /// <summary>
        /// Returns the short description.
        /// </summary>
        /// <returns></returns>
        public override string GetShortDescription() {
            return TaskName;
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            setState(TaskResult.InactiveType);
            foreach (ConditionBase cond in Conditions) {
                cond.Reset();
            }
        }

        /// <summary>
        /// Sets the state for the task.
        /// </summary>
        /// <param name="state"></param>
        protected override bool setState(TaskResult state) {
            bool stateChanged = base.setState(state);
            // Notfify the manager to actualize the connected UI's.
            if (AGM)
                AGM.NotifyTaskStateChanged(this);

            return stateChanged;
        }

    }
}

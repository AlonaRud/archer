using System.Collections.Generic;
using UnityEngine;

namespace ActivationGraphSystem {
    /// <summary>
    /// Base node for task oriented nodes.
    /// </summary>
    public class TaskBase : BaseNode {

        public bool ActivateOutgoingsManually = false;
        // Activations for success and fail.
        public List<TaskBase> TasksActivatedAfterSuccess = new List<TaskBase>();
        public List<TaskBase> TasksActivatedAfterFailed = new List<TaskBase>();
        // Force state flags.
        protected bool forceSuccess = false;
        protected bool forceFailed = false;
        protected bool forceDisable = false;

        public bool Restartable = false;

        // States for this kind of nodes.
        public enum TaskResult {
            InactiveType, RunningType, SuccessType, FailureType, DisabledType
        };
        public TaskResult Result = TaskResult.InactiveType;
        public List<TaskBase> TaskPredecessors = new List<TaskBase>();


        virtual public bool StartTask(bool force = false) {
            return false;
        }

        virtual public void StopTask() {
        }

        virtual public void ActivateOutgoingsExternally(bool flag) {
        }

        /// <summary>
        /// Adds a ancestor with a success edge.
        /// </summary>
        /// <param name="entry"></param>
        public virtual void AddSuccess(TaskBase entry) {
            if (!TasksActivatedAfterSuccess.Contains(entry))
                TasksActivatedAfterSuccess.Add(entry);

            if (!entry.TaskPredecessors.Contains(this))
                entry.TaskPredecessors.Add(this);
        }

        /// <summary>
        /// Adds a ancestor with a failure edge.
        /// </summary>
        /// <param name="entry"></param>
        public virtual void AddFailed(TaskBase entry) {
            if (!TasksActivatedAfterFailed.Contains(entry))
                TasksActivatedAfterFailed.Add(entry);

            if (!entry.TaskPredecessors.Contains(this))
                entry.TaskPredecessors.Add(this);
        }

        /// <summary>
        /// Removes an ancestor.
        /// </summary>
        /// <param name="entry"></param>
        public virtual void Remove(TaskBase entry) {
            if (TasksActivatedAfterSuccess.Contains(entry))
                TasksActivatedAfterSuccess.Remove(entry);
            if (TasksActivatedAfterFailed.Contains(entry))
                TasksActivatedAfterFailed.Remove(entry);

            if (entry.TaskPredecessors.Contains(this))
                entry.TaskPredecessors.Remove(this);
        }

        /// <summary>
        /// Removes all connections.
        /// </summary>
        public virtual void ClearConnections() {
            foreach (TaskBase b in TasksActivatedAfterSuccess) {
                if (b.TaskPredecessors.Contains(this))
                    b.TaskPredecessors.Remove(this);
            }

            foreach (TaskBase b in TasksActivatedAfterFailed) {
                if (b.TaskPredecessors.Contains(this))
                    b.TaskPredecessors.Remove(this);
            }

            TasksActivatedAfterSuccess.Clear();
            TasksActivatedAfterFailed.Clear();
            TaskPredecessors.Clear();
        }

        /// <summary>
        /// Forces success state.
        /// </summary>
        public virtual void SetSuccessed() {
            // Cannot implement here.
        }

        /// <summary>
        /// Forces failed state.
        /// </summary>
        public virtual void SetFailed() {
            // Cannot implement here.
        }

        /// <summary>
        /// Forces diasble state.
        /// </summary>
        public virtual void SetDisabled() {
            // Cannot implement here.
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            // Do nothing.
        }

        /// <summary>
        /// Tasks need a second reset, which switch them to the desired type.
        /// </summary>
        /// <param name="state"></param>
        public virtual void ResetTask(TaskResult state) {
        }

        public virtual void SetStateExternal(Task.TaskResult state) {
        }

        /// <summary>
        /// Some nodes must be actualized, that means e.g. and operator node must
        /// check its condition and its dependencies to be in the right state.
        /// In game situation: building destroyed and the buildings which depends on it
        /// cannot be build, so these nodes must be actualized to an inactive state.
        /// </summary>
        /// <param name="state"></param>
        public virtual void Actualize() {
        }
    }

}

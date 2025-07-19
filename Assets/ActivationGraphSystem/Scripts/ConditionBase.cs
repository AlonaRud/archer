using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace ActivationGraphSystem {
    /// <summary>
    /// Base node for the conditions.
    /// </summary>
    public class ConditionBase : BaseNode {

        public string Name = "Condition";
        [TextArea(3, 3)]
        public string ShortDescription = "This is a condition node.";

        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtSuccess;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtSuccess;

        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtFailure;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtFailure;

        public enum ConditionResult {
            InactiveType, RunningType, SuccessType, FailureType
        };
        public ConditionResult Result = ConditionResult.InactiveType;

        protected Action<BaseNode> successAction;
        protected Action<BaseNode> failureAction;

        // Triggers failure, red line.
        public bool TriggerFailure = false;

        // In the next check period the condition switches back to running mode.
        public bool IsTrigger = false;

        [Header("Count down from this time to 0, then trigger.")]
        public bool EnableTimer = false;
        public float TimerValue = 1;
        protected float startTime;
        public float CurrentTimerValue = 0;

        protected bool stopCondition = false;

        public List<Task> tasks = new List<Task>();

        bool doDebugState = false;


        /// <summary>
        /// Creates the actions.
        /// </summary>
        override protected void Awake() {
            base.Awake();

            // Activate the trigger.
            if (ScriptAtSuccess != null && !string.IsNullOrEmpty(MethodNameAtSuccess)) {
                MethodInfo mi = ScriptAtSuccess.GetType().GetMethod(MethodNameAtSuccess);
                if (mi == null) {
                    Debug.LogError("Method '" + MethodNameAtSuccess + "' cannot be called in " + ScriptAtSuccess + " on node: " + name);
                }
                successAction = (Action<BaseNode>)Delegate.CreateDelegate(typeof(Action<BaseNode>), ScriptAtSuccess, mi);
            }

            // Activate the trigger.
            if (ScriptAtFailure != null && !string.IsNullOrEmpty(MethodNameAtFailure)) {
                MethodInfo mi = ScriptAtFailure.GetType().GetMethod(MethodNameAtFailure);
                if (mi == null) {
                    Debug.LogError("Method '" + MethodNameAtFailure + "' cannot be called in " + ScriptAtFailure + " on node: " + name);
                }
                failureAction = (Action<BaseNode>)Delegate.CreateDelegate(typeof(Action<BaseNode>), ScriptAtFailure, mi);
            }
        }

        /// <summary>
        /// Starts the condition, normally at starting the task.
        /// </summary>
        virtual public void StartCondition(Task task) {
            // This tasks will be notified about the state changes.
            if (!tasks.Contains(task))
                tasks.Add(task);

            // Start timer
            startTime = Time.time;
            CurrentTimerValue = TimerValue;
            stopCondition = false;

            if (activateAction != null) {
                activateAction(this);
            }

            // If the state is already marked as successfully/failed or disabled, then do not change it.
            if (Result == ConditionResult.InactiveType)
                setState(ConditionResult.RunningType);
        }

        /// <summary>
        /// Concrete classes implement this method.
        /// </summary>
        /// <returns></returns>
        virtual public ConditionResult CheckState(bool updateNonPeriodicallyCheckedConditions) {
            // Check timer.
            if (EnableTimer)
                CurrentTimerValue = TimerValue - (Time.time - startTime);

            return Result;
        }

        /// <summary>
        /// Sets a flag for stopping the conditions.
        /// </summary>
        virtual public void StopCondition() {
            stopCondition = true;
        }

        /// <summary>
        /// Sets the state for the condition.
        /// </summary>
        /// <param name="state"></param>
        protected void setState(ConditionResult state) {
            if (Result == state || !Application.isPlaying)
                return;

            if (doDebugState)
                Debug.Log("old state: " + Result + " new state: " + state + " name: " + this.Name);

            switch (state) {
                case ConditionResult.InactiveType:
                    break;
                case ConditionResult.RunningType:
                    if (activateAction != null) {
                        activateAction(this);
                    }
                    break;
                case ConditionResult.SuccessType:
                    if (successAction != null) {
                        successAction(this);
                    }
                    break;
                case ConditionResult.FailureType:
                    if (failureAction != null) {
                        failureAction(this);
                    }
                    break;
                default:
                    break;
            }

            Result = state;

            // Notify the manager, sometimes the condition changes must be checked.
            AGM.NotifyConditionStateChanged(this);
            // Notify the connected tasks.
            NotityConnectedTasks();
        }

        /// <summary>
        /// Notify the connected tasks.
        /// </summary>
        public void NotityConnectedTasks() {
            foreach (Task task in tasks) {
                task.ConditionStateChanged(this);
            }
        }

        /// <summary>
        /// Returns the short description.
        /// </summary>
        /// <returns></returns>
        public override string GetShortDescription() {
            return ShortDescription;
        }

        /// <summary>
        /// Forces setting the state to successful.
        /// This depends fully on external calculations.
        /// </summary>
        public void SetSuccessful() {
            setState(ConditionResult.SuccessType);
        }

        /// <summary>
        /// Forces setting the state to failure.
        /// This depends fully on external calculations.
        /// </summary>
        public void SetFailed() {
            setState(ConditionResult.FailureType);
        }

        /// <summary>
        /// Forces setting the state to runninig, checks the conditions again.
        /// This depends fully on external calculations.
        /// </summary>
        public void SetRunning() {
            setState(ConditionResult.RunningType);
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            setState(ConditionResult.InactiveType);
        }

        /// <summary>
        /// This method is called after the task reaches its end state for waiting a dalay.
        /// </summary>
        public virtual bool IsBlocking() {
            return false;
        }

    }
}

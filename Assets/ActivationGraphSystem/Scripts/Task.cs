using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;


namespace ActivationGraphSystem {
    /// <summary>
    /// Base node for complex task nodes like TaskDataNode and OperatorNode.
    /// </summary>
    public class Task : TaskBase {

        [SerializeField]
        public List<ConditionBase> Conditions = new List<ConditionBase>();

        // Scripts and methods for the actions.
        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtSuccess;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtSuccess;

        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtFailure;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtFailure;

        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtDisable;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtDisable;

        // Activation limits.
        public bool EnableActivationLimit = false;
        public int ActivationLimit = 1;
        public bool EnableActivateAfterNActivations = false;
        public int ActivateAfterNActivations = 1;

        // Defines the coroutine for checking the conditions.
        protected Coroutine checkCondCo;

        // The actions which are created by the scripts and methods.
        // This is a fast way to call callback methods.
        protected Action<BaseNode> successAction;
        protected Action<BaseNode> failureAction;
        protected Action<BaseNode> disableAction;

        // Check the conditions periodically, in sec.
        public float CheckPeriod = 0.1f;
        // Counter for activations.
        public int CurrentActivations = 0;

        // List of internal user conditions to avoid unnecessary nodes on the screen.
        public List<ConditionUser> userConditions = new List<ConditionUser>();

        protected bool doCheckCondDebug = false;


        /// <summary>
        /// Return all the user conditions.
        /// </summary>
        /// <returns></returns>
        public List<ConditionUser> GetUserConditions() {
            return userConditions;
        }

        /// <summary>
        /// Add a new user condition at the end of the userConditions list.
        /// </summary>
        /// <returns></returns>
        public ConditionUser AddUserCondition() {
            ConditionUser user = gameObject.AddComponent<ConditionUser>();
            userConditions.Add(user);
            Add(user);
            user.IsHidden = true;
            return user;
        }

        /// <summary>
        /// Inserts the user condition to the certein position.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="user"></param>
        public void InsertUserCondition(int index, ConditionUser user) {
            userConditions.Insert(index, user);
        }

        /// <summary>
        /// Inserts a new user condition to a ceratin position.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ConditionUser InsertUserCondition(int index) {
            ConditionUser user = gameObject.AddComponent<ConditionUser>();
            userConditions.Insert(index, user);
            Add(user);
            user.IsHidden = true;
            return user;
        }

        /// <summary>
        /// Removes the internal user condition.
        /// </summary>
        /// <param name="user"></param>
        public void RemoveUserCondition(ConditionUser user) {
            Remove(user);
            userConditions.Remove(user);
            DestroyImmediate(user);
        }

        /// <summary>
        /// Removes the internal user condition at a certain position.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveUserConditionAt(int index) {
            ConditionUser user = userConditions[index];
            if (user == null) {
                Debug.LogError("ActivationGraphSystem: item at index '" + index + "' is null'");
                return;
            }

            Remove(user);
            userConditions.Remove(user);
            DestroyImmediate(user);
        }

        /// <summary>
        /// Needed by the editor to determine the user condition is inside the task or outside a standalone condition.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="doShow"></param>
        public void ShowCondition(ConditionUser user, bool doShow) {
            if (doShow) {
                if (userConditions.Contains(user)) {
                    user.IsHidden = false;
                    userConditions.Remove(user);
                }
            } else {
                if (!userConditions.Contains(user)) {
                    user.IsHidden = true;
                    userConditions.Add(user);
                }
            }
        }

        /// <summary>
        /// Creates the actions.
        /// </summary>
        override protected void Awake() {
            base.Awake();

            // Create the success action.
            if (ScriptAtSuccess != null && !string.IsNullOrEmpty(MethodNameAtSuccess)) {
                MethodInfo mi = ScriptAtSuccess.GetType().GetMethod(MethodNameAtSuccess);
                if (mi == null) {
                    Debug.LogError("Method '" + MethodNameAtSuccess + "' cannot be called in " + ScriptAtSuccess + " on node: " + name);
                }
                successAction = (Action<BaseNode>)Delegate.CreateDelegate(typeof(Action<BaseNode>), ScriptAtSuccess, mi);
            }

            // Create the failure action.
            if (ScriptAtFailure != null && !string.IsNullOrEmpty(MethodNameAtFailure)) {
                MethodInfo mi = ScriptAtFailure.GetType().GetMethod(MethodNameAtFailure);
                if (mi == null) {
                    Debug.LogError("Method '" + MethodNameAtFailure + "' cannot be called in " + ScriptAtFailure + " on node: " + name);
                }
                failureAction = (Action<BaseNode>)Delegate.CreateDelegate(typeof(Action<BaseNode>), ScriptAtFailure, mi);
            }

            // Create the disable action.
            if (ScriptAtDisable != null && !string.IsNullOrEmpty(MethodNameAtDisable)) {
                MethodInfo mi = ScriptAtDisable.GetType().GetMethod(MethodNameAtDisable);
                if (mi == null) {
                    Debug.LogError("Method '" + MethodNameAtDisable + "' cannot be called in " + ScriptAtDisable + " on node: " + name);
                }
                disableAction = (Action<BaseNode>)Delegate.CreateDelegate(typeof(Action<BaseNode>), ScriptAtDisable, mi);
            }
        }

        /// <summary>
        /// Stops the task and its conditions.
        /// </summary>
        override public void StopTask() {
            if (checkCondCo != null) {
                StopCoroutine(checkCondCo);
                conCheckThreadRunning = false;
                conCheckRunning = false;
            }
            foreach (ConditionBase cond in Conditions) {
                cond.StopCondition();
            }
        }

        /// <summary>
        /// A clean start task method which can be called externaly without cousing unknown side effects.
        /// </summary>
        /// <returns></returns>
        public virtual bool StartTaskExternal() {
            AGM.CurrentTasks.Add(this);
            return StartTask();
        }

        /// <summary>
        /// Called by the ActivationGraphManager script to start the task.
        /// </summary>
        /// <returns></returns>
        public override bool StartTask(bool force = false) {
            if (!force) {

                if (Result != TaskResult.DisabledType) {
                    CurrentActivations++;
                    if (EnableActivateAfterNActivations && !(CurrentActivations >= ActivateAfterNActivations)) {
                        return false;
                    } else if (EnableActivationLimit && ActivationLimit < CurrentActivations) {
                        return false;
                    }
                }
            }

            ResetTask(TaskResult.RunningType);

            foreach (ConditionBase cond in Conditions) {
                cond.StartCondition(this);
            }

            // Chacking the condition is not needed here, they will be checked by their state changes.

            return true;
        }

        /// <summary>
        /// Process condition check.
        /// </summary>
        /// <param name="cond"></param>
        public void ConditionStateChanged(ConditionBase cond) {
            if (doCheckCondDebug)
                Debug.Log("CONDITION: " + cond.name);

            // Collect events in the frame, to have maximal one condition check in a
            // certain time (CheckPeriod).
            if (doCheckCondDebug)
                Debug.Log("checkCondCo: " + checkCondCo + " conCheckRunning: " + conCheckRunning + 
                    " conCheckThreadRunning: " + conCheckThreadRunning);

            if (checkCondCo == null) {
                // If the coroutine is null, then start it. This happens at the first time.
                checkCondCo = StartCoroutine(checkConditions(!doCheckPeriodically(cond)));
            } else if (conCheckThreadRunning && !conCheckRunning) {
                // If the coroutine already started, but it is still waiting for events, then
                // stop the coroutine and restart it with the latest changes.
                StopCoroutine(checkCondCo);
                conCheckThreadRunning = false;
                conCheckRunning = false;
                checkCondCo = StartCoroutine(checkConditions(!doCheckPeriodically(cond)));
            } else if (!conCheckThreadRunning) {
                // If the coroutine not running, then start it.
                checkCondCo = StartCoroutine(checkConditions(!doCheckPeriodically(cond)));
            }
        }

        /// <summary>
        /// Process a condition check.
        /// </summary>
        /// <param name="cond"></param>
        public void ConditionStateChanged() {
            // Collect events in the frame, to have maximal one condition check in a
            // certain time (CheckPeriod).
            if (checkCondCo == null) {
                checkCondCo = StartCoroutine(checkConditions(true));
            } else if (conCheckThreadRunning && !conCheckRunning) {
                StopCoroutine(checkCondCo);
                conCheckThreadRunning = false;
                conCheckRunning = false;
                checkCondCo = StartCoroutine(checkConditions(true));
            } else if (!conCheckThreadRunning) {
                checkCondCo = StartCoroutine(checkConditions(true));
            }
        }

        protected bool conCheckRunning = false;
        protected bool conCheckThreadRunning = false;

        /// <summary>
        /// The coroutine which checks the conditions in CheckPeriod period time.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator checkConditions(bool updateNonPeriodicallyCheckedConditions) {
            conCheckThreadRunning = true;
            while (true) {

                if (doCheckCondDebug)
                    Debug.Log("TRY UPDATE CONDITIONS: " + this.name);

                conCheckRunning = false;
                yield return new WaitForSeconds(CheckPeriod);
                conCheckRunning = true;

                if (doCheckCondDebug)
                    Debug.Log("UPDATE CONDITIONS: " + this.name);

                // Check for periodically checking.
                bool doCheckInPeriods = false;
                foreach (ConditionBase cond in Conditions) {
                    doCheckInPeriods = doCheckPeriodically(cond);
                    if (doCheckInPeriods) {
                        break;
                    }
                }
                
                if (forceSuccess) {
                    setState(TaskResult.SuccessType);
                    AGM.TaskSuccessed(this);

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                } else if (forceFailed) {
                    setState(TaskResult.FailureType);
                    AGM.TaskFailed(this);

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                } else if (forceDisable) {
                    setState(TaskResult.DisabledType);
                    AGM.TaskDisabled(this);

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                }

                // If no contition, then still wait "CheckPeriod" and pass through
                // the success state.
                if (Conditions.Count == 0) {
                    setState(TaskResult.SuccessType);
                    AGM.TaskSuccessed(this);

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                }

                bool isFailure = false;
                int succCounter = 0;
                foreach (ConditionBase cond in Conditions) {

                    if (!doCheckPeriodically(cond) && !updateNonPeriodicallyCheckedConditions)
                        continue;

                    switch (cond.CheckState(updateNonPeriodicallyCheckedConditions)) {
                        case ConditionBase.ConditionResult.InactiveType:
                            break;
                        case ConditionBase.ConditionResult.RunningType:
                            break;
                        case ConditionBase.ConditionResult.SuccessType:
                            succCounter++;
                            if (cond.IsTrigger)
                                cond.SetRunning();
                            break;
                        case ConditionBase.ConditionResult.FailureType:
                            isFailure = true;
                            if (cond.IsTrigger)
                                cond.SetRunning();
                            break;
                        default:
                            break;
                    }
                }

                if (succCounter == Conditions.Count) {
					setState(TaskResult.SuccessType);
                    if (ActivateOutgoingsManually) {
                        StopTask();
                    } else {
                        AGM.TaskSuccessed(this);
                    }

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                } else if (isFailure) {
					setState (TaskResult.FailureType);
                    if (ActivateOutgoingsManually) {
                        StopTask();
                    } else {
                        AGM.TaskFailed(this);
                    }

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                }

                if (!doCheckInPeriods) {
                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                }
            }
        }

        /// <summary>
        /// Activate the outgoings from user code. It is used, when ActivateOutgoingFlag has been enabled for the task.
        /// flag: true for success outgoings and false for failure outgoings.
        /// </summary>
        public override void ActivateOutgoingsExternally(bool flag) {
            if (flag) {
                foreach (TaskBase taskBase in TasksActivatedAfterSuccess) {
                    AGM.CurrentTasks.Add(taskBase);
                    taskBase.StartTask();
                }
            } else {
                foreach (TaskBase taskBase in TasksActivatedAfterFailed) {
                    AGM.CurrentTasks.Add(taskBase);
                    taskBase.StartTask();
                }
            }
        }

        /// <summary>
        /// Check for periodically checking.
        /// </summary>
        /// <param name="cond"></param>
        /// <returns></returns>
        protected bool doCheckPeriodically(ConditionBase cond) {
            if (cond is ConditionUser) {
                ConditionUser cu = (ConditionUser)cond;
                if (cu.EnableTimer) {
                    return true;
                }
            } else if (cond is ConditionTimer || cond is ConditionSurvive || cond is ConditionDefeat) {
                return true;
            } else if (cond is ConditionArrival) {
                ConditionArrival ca = (ConditionArrival)cond;
                if (ca.EnableTimer) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the state for the task.
        /// </summary>
        /// <param name="state"></param>
        protected virtual bool setState(TaskResult state) {
            
            if (Result == state || !Application.isPlaying)
                return true;

            switch (state) {
                case TaskResult.DisabledType:
                    if (disableAction != null) {
                        disableAction(this);
                    }
                    break;
                case TaskResult.InactiveType:
                    break;
                case TaskResult.RunningType:
                    if (activateAction != null) {
                        activateAction(this);
                    }
                    break;
				case TaskResult.SuccessType:

					// Finalize the condition.
					// Normaly for the user condition, which consumes the resources here.
					foreach (ConditionBase cond in Conditions) {
						cond.PostProcess();
					}

                    if (successAction != null) {
                        successAction(this);
                    }
                    break;
                case TaskResult.FailureType:

                    if (failureAction != null) {
                        failureAction(this);
                    }
                    break;
                default:
                    break;
            }

            Result = state;

            // If the state changes, then the outgoing nodes must be actualized,
            // if they state is not inactive. The outgoing nodes depends on its 
            // incomming node. To provide actively changing nodes, the outgoing
            // nodes must be activated. E.g. A building representing node has
            // been destroyed, so the building or units, which depends ont this,
            // must be re-evaluated.
            if (state == TaskResult.InactiveType) {
                foreach (TaskBase task in TasksActivatedAfterSuccess) {
                    if (task != this && task.Result != TaskResult.InactiveType) {
                        task.Actualize();
                    }
                }
            }

            if (state == TaskResult.InactiveType) {
                foreach (TaskBase task in TasksActivatedAfterFailed) {
                    if (task != this && task.Result != TaskResult.InactiveType) {
                        task.Actualize();
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the short description.
        /// </summary>
        /// <returns></returns>
        public override string GetShortDescription() {
            return "Base Complex Task Node.";
        }

        /// <summary>
        /// Adds a condition for the task.
        /// </summary>
        /// <param name="entry"></param>
        public virtual void Add(ConditionBase entry) {
            if (!Conditions.Contains(entry))
                Conditions.Add(entry);
        }

        /// <summary>
        /// Removes a condition from the task.
        /// </summary>
        /// <param name="entry"></param>
        public virtual void Remove(ConditionBase entry) {
            if (Conditions.Contains(entry))
                Conditions.Remove(entry);
        }

        /// <summary>
        /// Removes all conditions.
        /// </summary>
        public override void ClearConnections() {
            base.ClearConnections();
            Conditions.RemoveAll(elem => !elem.IsHidden);
        }
        
        /// <summary>
        /// Forces success state.
        /// </summary>
        public override void SetSuccessed() {
            forceSuccess = true;
            ConditionStateChanged();
        }

        /// <summary>
        /// Forces failed state.
        /// </summary>
        public override void SetFailed() {
            forceFailed = true;
            ConditionStateChanged();
        }

        /// <summary>
        /// Forces diasble state.
        /// </summary>
        public override void SetDisabled() {
            forceDisable = true;
            ConditionStateChanged();
        }

        /// <summary>
        /// Set state by user source code.
        /// </summary>
        /// <param name="state"></param>
        public override void SetStateExternal(Task.TaskResult state) {
            switch (state) {
                case TaskResult.InactiveType:
                    setState(TaskResult.InactiveType);
                    AGM.TaskDisabled(this);
                    break;
                case TaskResult.RunningType:
                    StartTask(true);
                    break;
                case TaskResult.SuccessType:
                    setState(TaskResult.SuccessType);
                    AGM.TaskSuccessed(this);
                    break;
                case TaskResult.FailureType:
                    setState(TaskResult.FailureType);
                    AGM.TaskFailed(this);
                    break;
                case TaskResult.DisabledType:
                    setState(TaskResult.DisabledType);
                    AGM.TaskDisabled(this);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            setState(TaskResult.InactiveType);
            foreach (ConditionBase cond in Conditions) {
                cond.Reset();
            }
            CurrentActivations = 0;
        }

        public override void ResetTask(TaskResult state) {
            setState(state);
            foreach (ConditionBase cond in Conditions) {
                cond.Reset();
            }
        }

        /// <summary>
        /// Some nodes must be actualized, that means e.g. and operator node must
        /// check its condition and its dependencies to be in the right state.
        /// In game situation: building destroyed and the buildings which depends on it
        /// cannot be build, so these nodes must be actualized to an inactive state.
        /// </summary>
        /// <param name="state"></param>
        public override void Actualize() {
            // No predecessor, no changes.
            if (TaskPredecessors.Count == 0)
                return;

            foreach (TaskBase task in TaskPredecessors) {
                if (task.Result == TaskResult.SuccessType || task.Result == TaskResult.SuccessType) {
                    // There is still one predecessor active, so we remain in our state.
                    return;
                }
            }

            // Non of the predecessors is active, so deactivate ourself.
            setState(TaskResult.InactiveType);
            AGM.TaskDisabled(this);
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the operator node, which is not listed in the UI dialog.
    /// This node is for allowing enhanced control flow in the task system.
    /// One of the most important features of this node is to act as an and node for incoming activations.
    /// </summary>
    public class OperatorNode : Task {

        [TextArea(3, 3)]
        public string ShortDescription = "Operator node.";

        public bool IsAndNode = true;
        public bool TriggerFailure = false;
        public bool TriggerFailureConcurrents = false;
        public bool TriggerSucccessConcurrents = false;
        public bool TriggerInactiveConcurrents = false;

        public bool IsSuccessOutRandom = false;
        public int MinActSuccOutgoing = 1;
        public int MaxActSuccOutgoing = 1;
        public List<float> SuccessedAncestorProbabbility = new List<float>();
        public bool IsFailureOutRandom = false;
        public int MinActFailOutgoing = 1;
        public int MaxActFailOutgoing = 1;
        public List<float> FailedAncestorProbabbility = new List<float>();


        /// <summary>
        /// Starts the operator node.
        /// </summary>
        /// <returns></returns>
        public override bool StartTask(bool force = false) {
            if (!force) {

                // Diasabled type will not be treated.
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

            // Check task preconditions.
            if (IsAndNode) {
                foreach (Task task in TaskPredecessors) {
                    if (task.Result == TaskResult.RunningType || task.Result == TaskResult.InactiveType || task.Result == TaskResult.DisabledType) {
                        return false;
                    }
                }
            }

            foreach (ConditionBase cond in Conditions) {
                cond.StartCondition(this);
            }

            if (checkCondCo != null)
                StopCoroutine(checkCondCo);
            checkCondCo = StartCoroutine(checkConditions(true));

            return true;
        }

        /// <summary>
        /// Check the condition nodes on the operator node.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator checkConditions(bool updateNonPeriodicallyCheckedConditions) {
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
                    if (!TriggerFailure) {
                        setState(TaskResult.SuccessType);
                        AGM.TaskSuccessed(this);
                    } else {
                        setState(TaskResult.FailureType);
                        AGM.TaskFailed(this);
                    }

                    if (TriggerSucccessConcurrents)
                        nonFinishedPredecessorsToSuccess();
                    else if (TriggerFailureConcurrents)
                        nonFinishedPredecessorsToFailure();
                    else if (TriggerInactiveConcurrents)
                        nonFinishedPredecessorsToDisabled();

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
                    AGM.TaskSuccessed(this);

                    if (TriggerSucccessConcurrents)
                        nonFinishedPredecessorsToSuccess();
                    else if (TriggerFailureConcurrents)
                        nonFinishedPredecessorsToFailure();
                    else if (TriggerInactiveConcurrents)
                        nonFinishedPredecessorsToDisabled();

                    conCheckRunning = false;
                    conCheckThreadRunning = false;
                    yield break;
                } else if (isFailure) {
                    setState(TaskResult.FailureType);
                    AGM.TaskFailed(this);

                    if (TriggerSucccessConcurrents)
                        nonFinishedPredecessorsToSuccess();
                    else if (TriggerFailureConcurrents)
                        nonFinishedPredecessorsToFailure();
                    else if (TriggerInactiveConcurrents)
                        nonFinishedPredecessorsToDisabled();

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
        /// Sets the non finished predecessors to success state.
        /// </summary>
        void nonFinishedPredecessorsToSuccess() {
            foreach (TaskBase b in TaskPredecessors) {
                if (!(b.Result == TaskResult.FailureType || b.Result == TaskResult.SuccessType))
                    b.SetSuccessed();
            }
        }

        /// <summary>
        /// Sets the non finished predecessors to failure state.
        /// </summary>
        void nonFinishedPredecessorsToFailure() {
            foreach (TaskBase b in TaskPredecessors) {
                if (!(b.Result == TaskResult.FailureType || b.Result == TaskResult.SuccessType))
                    b.SetFailed();
            }
        }

        /// <summary>
        /// Sets the non finished predecessors to disabled state.
        /// </summary>
        void nonFinishedPredecessorsToDisabled() {
            foreach (TaskBase b in TaskPredecessors) {
                if (!(b.Result == TaskResult.FailureType || b.Result == TaskResult.SuccessType))
                    b.SetDisabled();
            }
        }

        /// <summary>
        /// Returns the short description.
        /// </summary>
        public override string GetShortDescription() {
            return ShortDescription;
        }

        /// <summary>
        /// Removes all connections.
        /// </summary>
        public override void ClearConnections() {
            base.ClearConnections();
            SuccessedAncestorProbabbility.Clear();
            FailedAncestorProbabbility.Clear();
        }

        /// <summary>
        /// Adds a ancestor with a success edge. Additional adds the probability.
        /// </summary>
        /// <param name="entry"></param>
        public override void AddSuccess(TaskBase entry) {
            base.AddSuccess(entry);
            SuccessedAncestorProbabbility.Add(1);
        }

        /// <summary>
        /// Adds a ancestor with a failure edge. Additional adds the probability.
        /// </summary>
        /// <param name="entry"></param>
        public override void AddFailed(TaskBase entry) {
            base.AddFailed(entry);
            FailedAncestorProbabbility.Add(1);
        }

        /// <summary>
        /// Removes an ancestor. Additional removes the probability.
        /// </summary>
        /// <param name="entry"></param>
        public override void Remove(TaskBase entry) {
            if (TasksActivatedAfterSuccess.Contains(entry)) {
                if (TasksActivatedAfterSuccess.Count != SuccessedAncestorProbabbility.Count)
                    refeshProbablilityList();

                SuccessedAncestorProbabbility.RemoveAt(TasksActivatedAfterSuccess.IndexOf(entry));
            }
            if (TasksActivatedAfterFailed.Contains(entry)) {
                if (TasksActivatedAfterFailed.Count != FailedAncestorProbabbility.Count)
                    refeshProbablilityList();

                FailedAncestorProbabbility.RemoveAt(TasksActivatedAfterFailed.IndexOf(entry));
            }

            base.Remove(entry);
        }

        /// <summary>
        /// For compatibility reasons, recreate (override) the probabilities, 
        /// if they are not exist or not synchron to the outgoings.
        /// Ii is a fallback method to do not block the graph editor.
        /// </summary>
        void refeshProbablilityList() {
            SuccessedAncestorProbabbility.Clear();
            FailedAncestorProbabbility.Clear();

            for (int i = 0; i < TasksActivatedAfterSuccess.Count; i++) {
                SuccessedAncestorProbabbility.Add(1);
            }
            
            for (int i = 0; i < TasksActivatedAfterFailed.Count; i++) {
                FailedAncestorProbabbility.Add(1);
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

            if (IsAndNode) {
                foreach (Task task in TaskPredecessors) {
                    if (task.Result == TaskResult.RunningType || task.Result == TaskResult.InactiveType || task.Result == TaskResult.DisabledType) {
                        // One of the predecessors is not active, so deactivate ourself.
                        setState(TaskResult.InactiveType);
                        AGM.TaskDisabled(this);
                    }
                }
            } else {
                base.Actualize();
            }
        }

    }
}

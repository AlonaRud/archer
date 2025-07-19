using UnityEngine;
using System.Collections.Generic;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the arrival condition node.
    /// ArrivingObjects list contains the objectives, which must reach the destination arrea. 
    /// </summary>
    public class ConditionArrival : ConditionBase {

        [Header("This objects musst arrive in the collider area.")]
        public List<Transform> ArrivingObjects = new List<Transform>();
        [Header("Defines the arrea where the ArrivingObjects must arrive.")]
        public Collider ArrivalCollider;
        private List<Transform> ArrivedObjects = new List<Transform>();

        [Header("Count down from this time to 0, then trigger.")]
        public bool TimerSignalFailure = true;


        /// <summary>
        /// Starts the condition, normally the task calls this method.
        /// </summary>
        override public void StartCondition(Task task) {
            // Strat timer
            startTime = Time.time;
            ArrivedObjects.Clear();

            base.StartCondition(task);
        }

        /// <summary>
        /// Checks the condition, the task based instances call this method.
        /// </summary>
        override public ConditionResult CheckState(bool updateNonPeriodicallyCheckedConditions) {
            if (stopCondition || Result == ConditionResult.SuccessType || Result == ConditionResult.InactiveType) {
                return Result;
            }

            // Check timer.
            if (EnableTimer)
                CurrentTimerValue = TimerValue - (Time.time - startTime);
            if (EnableTimer && startTime + TimerValue <= Time.time) {
                if (!TimerSignalFailure) {
                    setState(ConditionResult.SuccessType);
                } else {
                    setState(ConditionResult.FailureType);
                }
            }

            return Result;
        }

        /// <summary>
        /// Sets the ArrivalCollider, which will be searched on this game object.
        /// </summary>
        override protected void Awake() {
            ArrivalCollider = GetComponent<Collider>();
            base.Awake();
        }

        /// <summary>
        /// The trigger collider acts to this method, if a collider with a rigidbody enters the ArrivalCollider.
        /// The game object of the collider will be compared with the ArrivingObjects and if it matches one of the entries,
        /// then the entry will be "marked" as arrived.
        /// </summary>
        /// <param name="col"></param>
        void OnTriggerEnter(Collider col) {
            if (stopCondition || Result == ConditionResult.SuccessType || Result == ConditionResult.InactiveType) {
                return;
            }

            // Check timer.
            if (EnableTimer && startTime + TimerValue <= Time.time) {
                if (!TimerSignalFailure) {
                    setState(ConditionResult.SuccessType);
                } else {
                    setState(ConditionResult.FailureType);
                }
            }

            if (!ArrivingObjects.Contains(col.transform)) {
                return;
            }

            Transform tr = col.transform;
            if (ArrivingObjects.Contains(tr) && !ArrivedObjects.Contains(tr)) {
                ArrivedObjects.Add(tr);
            }

            if (ArrivedObjects.Count == ArrivingObjects.Count) {
                if (!TriggerFailure) {
                    setState(ConditionResult.SuccessType);
                } else {
                    setState(ConditionResult.FailureType);
                }
            }
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            setState(ConditionResult.InactiveType);
        }
    }
}

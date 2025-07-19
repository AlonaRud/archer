using UnityEngine;
using System.Collections.Generic;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the defeat condition node. 
    /// ObjectsToDefeate list contains the objectives, which must be destroyed. 
    /// Timer can be set for this condition, if it elapses, then a failure signal will be emited. 
    /// </summary>
    public class ConditionDefeat : ConditionBase {

        [Header("These objects must be defeated.")]
        public List<Transform> ObjectsToDefeate = new List<Transform>();
        [Header("0 means defeate all.")]
        public int DefeateAtLeast = 0;
        private int defeatedCount = 0;


        /// <summary>
        /// Starts the condition, normally the task calls this method.
        /// </summary>
        override public void StartCondition(Task task) {
            // Strat timer
            startTime = Time.time;
            defeatedCount = 0;

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
            // Check timer.
            if (EnableTimer)
                CurrentTimerValue = TimerValue - (Time.time - startTime);
            if (EnableTimer && startTime + TimerValue <= Time.time) {
                setState(ConditionResult.FailureType);
            }

            // Look for defeated entries "null".
            for (int i = ObjectsToDefeate.Count - 1; i < 0; i--) {
                if (ObjectsToDefeate[i] == null) {
                    defeatedCount++;
                    ObjectsToDefeate.RemoveAt(i);
                }
            }

            // Check for DefeateAtLeast.
            if (DefeateAtLeast != 0 && DefeateAtLeast <= defeatedCount) {
                setState(ConditionResult.SuccessType);
            }

            // Check without DefeateAtLeast limit.
            if (ObjectsToDefeate.Count <= 0) {
                setState(ConditionResult.SuccessType);
            }

            return Result;
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            setState(ConditionResult.InactiveType);
        }
    }
}

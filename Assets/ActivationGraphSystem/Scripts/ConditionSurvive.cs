using UnityEngine;
using System.Collections.Generic;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the survive condition node.
    /// ObjectsToSurvive list contains the objectives, which must survive. 
    /// Timer can be set for this condition, if it elapses, then a success signal will be emited. 
    /// </summary>
    public class ConditionSurvive : ConditionBase {
        
        [Header("These objects must survive.")]
        public List<Transform> ObjectsToSurvive = new List<Transform>();
        [Header("0 means all survived.")]
        public int SurviveAtLeast = 0;
        private int notSurvivedCount = 0;
        private int baseObjectsToSurviveCount = 0;


        /// <summary>
        /// Starts the condition, normally the task calls this method.
        /// </summary>
        override public void StartCondition(Task task) {
            notSurvivedCount = 0;
            baseObjectsToSurviveCount = ObjectsToSurvive.Count;

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
                setState(ConditionResult.SuccessType);
            }

            // Look for lost objects "null".
	        for (int i = ObjectsToSurvive.Count - 1; i >= 0; i--) {
		        if (ObjectsToSurvive[i] == null) {
                    notSurvivedCount++;
                    ObjectsToSurvive.RemoveAt(i);
                }
            }

	        // Check for SurviveAtLeast.
	        if (SurviveAtLeast != 0 && SurviveAtLeast > baseObjectsToSurviveCount - notSurvivedCount) {
                setState(ConditionResult.FailureType);
            }

	        // Check without SurviveAtLeast limit.
            if (ObjectsToSurvive.Count < baseObjectsToSurviveCount) {
                setState(ConditionResult.FailureType);
            }

            return Result;
        }

        /// <summary>
        /// Resets the node in the initial state.
        /// </summary>
        public override void Reset() {
            setState(ConditionResult.InactiveType);
        }
    }
}

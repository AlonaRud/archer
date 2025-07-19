using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the timer condition node. 
    /// A timer value can be set, if the timer elapses, then a success or failure node will be emited.
    /// </summary>
    public class ConditionTimer : ConditionBase {

        [Range(0, 1)]
        public float ProbabilityOfSucc = 1;

        [Header("Random timer properties")]
        public bool IsRandom = false;
        public float MinTime = 1;
        public float MaxTime = 1;

        /// <summary>
        /// Starts the condition, normally the task calls this method.
        /// </summary>
        override public void StartCondition(Task task) {
            // Strat timer
            startTime = Time.time;
            if (IsRandom) {
                TimerValue = Random.Range(MinTime, MaxTime);
            }
            EnableTimer = true;

            base.StartCondition(task);
        }

        /// <summary>
        /// Checks the condition, the task based instances call this method.
        /// The timer update its time in this method, so it delays with the CheckTime period.
        /// </summary>
        override public ConditionResult CheckState(bool updateNonPeriodicallyCheckedConditions) {
            if (stopCondition || Result == ConditionResult.SuccessType || Result == ConditionResult.InactiveType) {
                return Result;
            }

            // Check timer.
            if (EnableTimer)
                CurrentTimerValue = TimerValue - (Time.time - startTime);
            if (startTime + TimerValue <= Time.time) {
                if (!TriggerFailure) {
                    setState(ConditionResult.SuccessType);
                } else {
                    setState(ConditionResult.FailureType);
                }
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

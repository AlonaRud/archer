using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ActivationGraphSystem {
    public class TechGuiController : MonoBehaviour {

        public static TechGuiController Instance;

        public ActivationGraphManager AGM;

        public List<Button> Buttons = new List<Button>();
        public List<ConditionUser> UserConditions = new List<ConditionUser>();
        public List<Task> Tasks = new List<Task>();

        protected void Awake() {
            Instance = this;

            AGM.initializeMulticast += Initialize;
            AGM.taskDataStateChangedMulticast += ActualizeTaskData;
        }

        public void Initialize() {
            foreach (TaskDataNode task in AGM.Tasks) {
                if (task.Type == Task.Types.TechNode) {
                    ActualizeTaskData(task);
                }
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeTaskData(TaskDataNode task) {
            if (task.Type == Task.Types.TechNode) {
                int index = Tasks.IndexOf(task);
                switch (task.Result) {
                    case TaskBase.TaskResult.InactiveType:
                        Buttons[index].image.color = Color.gray;
                        Buttons[index].interactable = false;
                        break;
                    case TaskBase.TaskResult.RunningType:
                        Buttons[index].image.color = Color.yellow;
                        Buttons[index].interactable = true;
                        break;
                    case TaskBase.TaskResult.SuccessType:
                        Buttons[index].image.color = Color.green;
                        Buttons[index].interactable = false;
                        break;
                    case TaskBase.TaskResult.FailureType:
                        Buttons[index].image.color = Color.red;
                        Buttons[index].interactable = false;
                        break;
                    default:
                        break;
                }
            }
        }

        public void ProcessR1() {
            UserConditions[0].SetSuccessful();
        }

        public void ProcessR2() {
            UserConditions[1].SetSuccessful();
        }

        public void ProcessR3() {
            UserConditions[2].SetSuccessful();
        }

        public void ProcessR4() {
            UserConditions[3].SetSuccessful();
        }

        public void ProcessR5() {
            UserConditions[4].SetSuccessful();
        }

        public void ProcessR6() {
            UserConditions[5].SetSuccessful();
        }
    }
}

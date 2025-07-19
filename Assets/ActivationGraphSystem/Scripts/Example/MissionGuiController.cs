using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// This class conrols all the UI relevant components, the dialog, the 
    /// text box in left upper corner and the text boxes in the right upper corner.
    /// </summary>
    public class MissionGuiController : MonoBehaviour {

        public static MissionGuiController Instance;

        public ActivationGraphManager AGM;

        public TypeWriter MissionDesc;
        [Header("Components in top left corner.")]
        public TypeWriter MissionDescOnScreen;
        [Header("Components in top right corner.")]
        public Text MissionShortDescTitle;
        public TypeWriter MissionShortDesc;

        [Header("Parent for the entry, the content panel of the scroll view.")]
        public RectTransform MissionEntryGrid;
        [Header("Entry prefab.")]
        public GameObject MissionEntryPrefab;

        [Header("Will be activated at the end of the mission system.")]
        public GameObject Success;
        public GameObject Failure;

        public bool IsShown = false;

        List<MissionItem> MissionItems = new List<MissionItem>();
        public Dictionary<TaskDataNode, MissionItem> TaskItemDict = new Dictionary<TaskDataNode, MissionItem>();


        protected void Awake() {
            Instance = this;
            
            AGM.initializeMulticast += Initialize;
            AGM.taskDataStateChangedMulticast += ActualizeTaskData;
        }

        /// <summary>
        /// Creates the mission entries for the dialog.
        /// </summary>
        public void Initialize() {
            MissionItems.Clear();
            TaskItemDict.Clear();
            foreach (TaskDataNode task in AGM.Tasks) {
                if (task.Type == BaseNode.Types.MissionNode) {
                    GameObject entry = Instantiate(MissionEntryPrefab) as GameObject;
                    entry.transform.SetParent(MissionEntryGrid);
                    MissionItem missItem = entry.GetComponent<MissionItem>();
                    missItem.MissionDesc = MissionDesc;
                    missItem.MissTask = task;

                    MissionItems.Add(missItem);
                    TaskItemDict.Add(task, missItem);
                }
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeTaskData(TaskDataNode task) {
            if (task.Type != BaseNode.Types.MissionNode) {
                return;
            }

            // One mission node were not added to the ActivationGraphManager order list.
            // Maybe it should be hidden.
            if (!TaskItemDict.ContainsKey(task))
                return;

            TaskItemDict[task].Actualize();

            // If 2 tasks are running, just write for the first one.
            bool alreadyWrote = false;

            switch (task.Result) {
                case TaskBase.TaskResult.InactiveType:
                    break;
                case TaskBase.TaskResult.RunningType:
                    if (!alreadyWrote) {
                        alreadyWrote = true;
                        WriteDescriptionOnScrren(task);
                    }
                    break;
                case Task.TaskResult.SuccessType:
                    break;
                case Task.TaskResult.FailureType:
                    break;
                default:
                    break;
            }

            reorderItems();
        }

        /// <summary>
        /// Sorts the mission entries in the mission dialog.
        /// Active missions to the top, finished to the middle, classified to the end.
        /// </summary>
        protected void reorderItems() {

            foreach (MissionItem item in MissionItems) {
                item.transform.SetParent(null);
            }

            foreach (MissionItem item in MissionItems) {
                switch (item.MissTask.Result) {
                    case TaskBase.TaskResult.InactiveType:
                        break;
                    case TaskBase.TaskResult.RunningType:
                        item.transform.SetParent(MissionEntryGrid);
                        break;
                    case TaskBase.TaskResult.SuccessType:
                        break;
                    case TaskBase.TaskResult.FailureType:
                        break;
                    default:
                        break;
                }
            }
            foreach (MissionItem item in MissionItems) {
                switch (item.MissTask.Result) {
                    case TaskBase.TaskResult.InactiveType:
                        break;
                    case TaskBase.TaskResult.RunningType:
                        break;
                    case TaskBase.TaskResult.SuccessType:
                        item.transform.SetParent(MissionEntryGrid);
                        break;
                    case TaskBase.TaskResult.FailureType:
                        break;
                    default:
                        break;
                }
            }
            foreach (MissionItem item in MissionItems) {
                switch (item.MissTask.Result) {
                    case TaskBase.TaskResult.InactiveType:
                        break;
                    case TaskBase.TaskResult.RunningType:
                        break;
                    case TaskBase.TaskResult.SuccessType:
                        break;
                    case TaskBase.TaskResult.FailureType:
                        item.transform.SetParent(MissionEntryGrid);
                        break;
                    default:
                        break;
                }
            }
            foreach (MissionItem item in MissionItems) {
                switch (item.MissTask.Result) {
                    case TaskBase.TaskResult.InactiveType:
                        item.transform.SetParent(MissionEntryGrid);
                        break;
                    case TaskBase.TaskResult.RunningType:
                        break;
                    case TaskBase.TaskResult.SuccessType:
                        break;
                    case TaskBase.TaskResult.FailureType:
                        break;
                    default:
                        break;
                }
            }
        }

        public void ResetUI() {
            Show();
            IsShown = false;
            gameObject.SetActive(false);
        }

        public void Show() {
            gameObject.SetActive(true);
            IsShown = true;
        }

        public void CloseUI() {
            IsShown = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Writes to the left upper and the right upper text components.
        /// </summary>
        /// <param name="task">The task node, which preserves the descriptions.</param>
        protected void WriteDescriptionOnScrren(TaskDataNode task) {
            MissionDescOnScreen.Clear();
            MissionDescOnScreen.SetText(task.TaskDesc);

            MissionShortDesc.Clear();
            MissionShortDesc.SetText(task.TaskDescShort);

            MissionShortDescTitle.text = task.TaskName;
        }

        /// <summary>
        /// Activate the mission completed panel.
        /// </summary>
        public void SetSuccess(BaseNode node) {
            Success.SetActive(true);
        }

        /// <summary>
        /// Activate the mission failed panel.
        /// </summary>
        public void SetFailure(BaseNode node) {
            Failure.SetActive(true);
        }
    }
}

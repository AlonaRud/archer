using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// This class conrols all the UI relevant components, the dialog, the 
    /// text box in left upper corner and the text boxes in the right upper corner.
    /// </summary>
    public class SkillTreeGuiController : MonoBehaviour {

        public static SkillTreeGuiController Instance;
        public ActivationGraphManager AGM;
        public TypeWriter Desc;
        [Header("Parent for the entry, the content panel of the scroll view for Skills.")]
        public RectTransform EntryGridSkill;
        [Header("Entry prefab.")]
        public ContainerBase Container;
        public bool IsShown = false;
        List<SkillTreeItem> SkillTreeItems = new List<SkillTreeItem>();
        // These conditions will be triggered, if the button will be clicked.
        public Dictionary<TaskDataNode, ConditionUser> TaskTriggerConditionDict = new Dictionary<TaskDataNode, ConditionUser>();
        // Resource independent conditions.
        public Dictionary<TaskDataNode, ConditionUser> TaskResConditionDict = new Dictionary<TaskDataNode, ConditionUser>();


        /// <summary>
        /// Initialize the singleton static variable and connect the graph manager notifications.
        /// </summary>
        protected void Awake() {
            Instance = this;

            SkillTreeItems.AddRange(EntryGridSkill.GetComponentsInChildren<SkillTreeItem>());

            AGM.initializeMulticast += Initialize;
            AGM.taskDataStateChangedMulticast += ActualizeTaskData;
            AGM.conditionStateChangedMulticast += ActualizeCondition;
        }

        /// <summary>
        /// Creates the SkillTree entries for the dialog.
        /// </summary>
        public void Initialize() {
            foreach (TaskDataNode task in AGM.Tasks) {

                if (task.Type == BaseNode.Types.SkillNode) {
                    // Map DataTaskNode by name to the UI item.
                    SkillTreeItem entry = SkillTreeItems.Find(elem => elem.name == task.name);
                    if (entry == null) {
                        Debug.LogError("SkillTreeGuiController: task '" + task.name + "' cannot mapped by name. Create a SkillTreePrefab with the name!");
                        continue;
                    }
                    entry.Desc = Desc;
                    entry.Task = task;
                }

                // Collect the conditions, which will be triggered by the buttons.
                // The trigger condition, which start the SkillTree, is the ConditionUser, which EnableItemRef is not set.
                // You can also match the name, if you set a unique name for the conditions under the task.
                TaskTriggerConditionDict.Add(task, 
                    (ConditionUser)task.Conditions.Find(item => (item is ConditionUser) && ((ConditionUser)item).EnableContainerAccess == false));

                // Collect the resource independent conditions.
                TaskResConditionDict.Add(task,
                    (ConditionUser)task.Conditions.Find(item => (item is ConditionUser) && ((ConditionUser)item).EnableContainerAccess == true));
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeTaskData(TaskDataNode node) {
            foreach (SkillTreeItem item in SkillTreeItems) {
                // This is needed to avoid flickering buttons. More information in SkillTechItem.
                StartCoroutine(item.Actualize());
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeCondition(ConditionBase condition) {
            foreach (SkillTreeItem item in SkillTreeItems) {
                // This is needed to avoid flickering buttons. More information in SkillTechItem.
                StartCoroutine(item.Actualize());
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
    }
}

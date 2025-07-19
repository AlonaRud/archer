using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// This class conrols all the UI relevant components, the dialog, the 
    /// text box in left upper corner and the text boxes in the right upper corner.
    /// </summary>
    public class CraftingGuiController : MonoBehaviour {

        public static CraftingGuiController Instance;

        public ActivationGraphManager AGM;

        public TypeWriter Desc;
        public Text ConatinerContent;

        [Header("Parent for the entry, the content panel of the scroll view.")]
        public RectTransform EntryGrid;
        [Header("Entry prefab.")]
        public GameObject EntryPrefab;

        public ContainerManager CM;

        public bool IsShown = false;

        List<CraftingItem> CraftingItems = new List<CraftingItem>();
        // These conditions will be triggered, if the button will be clicked.
        public Dictionary<TaskDataNode, ConditionUser> TaskTriggerConditionDict = new Dictionary<TaskDataNode, ConditionUser>();
        // Resource independent conditions.
        public Dictionary<TaskDataNode, ConditionUser> TaskResConditionDict = new Dictionary<TaskDataNode, ConditionUser>();


        protected void Awake() {
            Instance = this;

            AGM.initializeMulticast += Initialize;
            AGM.taskDataStateChangedMulticast += ActualizeTaskData;
            AGM.conditionStateChangedMulticast += ActualizeCondition;
        }

        /// <summary>
        /// Creates the crafting entries for the dialog.
        /// </summary>
        public void Initialize() {
            CraftingItems.Clear();
            foreach (TaskDataNode task in AGM.Tasks) {
                if (task.Type == Task.Types.CraftingNode) {
                    GameObject entry = Instantiate(EntryPrefab) as GameObject;
                    entry.transform.SetParent(EntryGrid);
                    CraftingItem craftingItem = entry.GetComponent<CraftingItem>();
                    craftingItem.Desc = Desc;
                    craftingItem.Task = task;
                    CraftingItems.Add(craftingItem);

                    // Collect the conditions, which will be triggered by the buttons.
                    // The trigger condition, which start the crafting, is the ConditionUser, which EnableItemRef is not set.
                    // You can also match the name, if you set a unique name for the conditions under the task.
                    TaskTriggerConditionDict.Add(task, 
                        (ConditionUser)task.Conditions.Find(item => (item is ConditionUser) && ((ConditionUser)item).EnableContainerAccess == false));

                    // Collect the resource independent conditions.
                    TaskResConditionDict.Add(task,
                        (ConditionUser)task.Conditions.Find(item => (item is ConditionUser) && ((ConditionUser)item).EnableContainerAccess == true));
                }
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeTaskData(TaskDataNode node) {
            foreach (CraftingItem item in CraftingItems) {
                // This is needed to avoid flickering buttons. More information in CraftingItem.
                StartCoroutine(item.Actualize());
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeCondition(ConditionBase condition) {
            foreach (CraftingItem item in CraftingItems) {
                // This is needed to avoid flickering buttons. More information in CraftingItem.
                StartCoroutine(item.Actualize());
            }

            // Dump the container content to the container window.
            string contContText = "Resources:\n\n";
            List<string> itemNames = CM.GetAllItemNames();
            foreach (string itemName in itemNames) {
                float value = CM.GetItemValue(itemName);
                contContText += "\t" + itemName + ": " + value + "\n";
            }
            ConatinerContent.text = contContText;
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

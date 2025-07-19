using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// This is an example script, which shows how you can use the activation graph to implement
    /// your own implementtion. The example is short and very powerful.
    /// 
    /// This class conrols all the UI relevant components for building a building and unit.
    /// This is just an example implementation for showing you how to control the graph externaly,
    /// how to use the graph for your advantages. The BuildTreeGuiController and BuildTreeItems are 
    /// written for procedural processing the graph components. That means, adding new products in the 
    /// graph needs no changes in these UI classes.
    /// The code itself is a bit more complex in BuildTreeItems, but the advantage is, the graph
    /// can be manipulated and the UI must not be changed.
    /// </summary>
    public class BuildTreeGuiController : MonoBehaviour {

        // The BuildTreeGuiController is a singleton.
        public static BuildTreeGuiController Instance;

        // This is the connected graph.
        public ActivationGraphManager AGM;

        // The description field at the bottom. Item informations will be shown here at hovering over the buttons.
        public TypeWriter Desc;

        [Header("Parent for the entry, the content panel of the scroll view for buildings.")]
        public RectTransform EntryGridBuilding;
        [Header("Parent for the entry, the content panel of the scroll view for Units.")]
        public RectTransform EntryGridUnit;
        [Header("Entry prefab. This will be instantiated.")]
        public GameObject EntryPrefab;

        // This is the main container base. In the graph all consumer user conditions references the same container,
        // the container manager. In your case you could have different containers, depends on the graph.
        public ContainerBase CM;

        // Show/hide flag. This must be done from a main UI (your main UI), that is not part of this example. 
        public bool IsShown = false;

        // The list off all the UI entries, which contains the build logic and the button.
        List<BuildTreeItem> BuildTreeItems = new List<BuildTreeItem>();
        // These conditions will be triggered, if the button will be clicked.
        public Dictionary<TaskDataNode, ConditionUser> TaskTriggerConditionDict = new Dictionary<TaskDataNode, ConditionUser>();
        // Resource independent conditions.
        public Dictionary<TaskDataNode, ConditionUser> TaskResConditionDict = new Dictionary<TaskDataNode, ConditionUser>();

        // Limit the process() method updates by this time period for better performance.
        public float CheckPeriod = 0.5f;
        float currentTime = 0;

        // Buildings and units are built differently. Bulding will be ready to place them
        // somewhere and units comes out of the buildings automatically.
        public List<BuildQueueEntry> buildingQueue = new List<BuildQueueEntry>();
        public List<BuildQueueEntry> unitQueue = new List<BuildQueueEntry>();

        public Transform BuildingSpawn;
        public Transform UnitSpawn;

        public AudioSource ClickAudio;


        /// <summary>
        /// Sigleton and callback initialization.
        /// </summary>
        protected void Awake() {
            Instance = this;

            // Register callback from ActivationGraphManager, will be called if the depending scripts should initialize itself.
            // This is the time, where the ActivationGraphManager is in correct state for initialization.
            AGM.initializeMulticast += Initialize;
            // Register callback to signalize that one task has changed its state.
            AGM.taskDataStateChangedMulticast += ActualizeTaskData;
            // Register callback to signalize that one condition has changed its state.
            // Normally this happens often then the the task state changed signal.
            AGM.conditionStateChangedMulticast += ActualizeCondition;

            if (ClickAudio == null)
                ClickAudio = GetComponent<AudioSource>();
        }

        /// <summary>
        /// The Update method is needed to process the building procedure.
        /// </summary>
        protected void Update() {
            if (currentTime < Time.time) {
                process();
                currentTime = Time.time + CheckPeriod;
            }
        }

        /// <summary>
        /// Creates the BuildTree entries for the dialog.
        /// </summary>
        public void Initialize() {
            BuildTreeItems.Clear();

            foreach (TaskDataNode task in AGM.Tasks) {
                if (task.Type == BaseNode.Types.BuildNode || task.Type == BaseNode.Types.UnitNode) {
                    GameObject entry = Instantiate(EntryPrefab) as GameObject;

                    if (task.Type == BaseNode.Types.BuildNode) {
                        //Buildings to EntryGridBuilding.
                        entry.transform.SetParent(EntryGridBuilding);
                        BuildTreeItem BuildTreeItem = entry.GetComponent<BuildTreeItem>();
                        BuildTreeItem.Desc = Desc;
                        BuildTreeItem.Task = task;
                        BuildTreeItem.ClickAudio = ClickAudio;
                        // Slow search for data, but just one time at the beginning.
                        BuildTreeItem.DataEntry = BuildTreeDataProvider.Instance.BuildTreeDataEntries.Find(elem => elem.Name == task.name);
                        BuildTreeItem.IconImage.sprite = BuildTreeItem.DataEntry.Image;
                        BuildTreeItems.Add(BuildTreeItem);
                    } else if (task.Type == BaseNode.Types.UnitNode) {
                        // Units to EntryGridUnit.
                        entry.transform.SetParent(EntryGridUnit);
                        BuildTreeItem BuildTreeItem = entry.GetComponent<BuildTreeItem>();
                        BuildTreeItem.Desc = Desc;
                        BuildTreeItem.Task = task;
                        BuildTreeItem.ClickAudio = ClickAudio;
                        // Slow search for data, but just one time at the beginning.
                        BuildTreeItem.DataEntry = BuildTreeDataProvider.Instance.BuildTreeDataEntries.Find(elem => elem.Name == task.name);
                        BuildTreeItem.IconImage.sprite = BuildTreeItem.DataEntry.Image;
                        BuildTreeItems.Add(BuildTreeItem);
                    }

                    // Collect the conditions, which will be triggered by the buttons.
                    // The trigger condition, which start the BuildTree, is the ConditionUser, which EnableItemRef is not set.
                    // You can also match the name, if you set a unique name for the conditions under the task.
                    TaskTriggerConditionDict.Add(task, 
                        (ConditionUser)task.Conditions.Find(item => (item is ConditionUser) && ((ConditionUser)item).EnableContainerAccess == false));

                    // Collect the consumer conditions.
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
            foreach (BuildTreeItem item in BuildTreeItems) {
                // This is needed to avoid flickering buttons. More information in BuildTechItem.
                StartCoroutine(item.Actualize());
            }
        }

        /// <summary>
        /// Called by the ActivationGraphManager instance, if the changes are
        /// UI relevant.
        /// </summary>
        public void ActualizeCondition(ConditionBase condition) {
            foreach (BuildTreeItem item in BuildTreeItems) {
                // This is needed to avoid flickering buttons. More information in BuildTechItem.
                StartCoroutine(item.Actualize());
            }
        }

        /// <summary>
        /// Should be controlled by your main UI.
        /// </summary>
        public void ResetUI() {
            Show();
            IsShown = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Should be controlled by your main UI, shows up this UI.
        /// </summary>
        public void Show() {
            gameObject.SetActive(true);
            IsShown = true;
        }

        /// <summary>
        /// Should be controlled by your main UI, hides this UI.
        /// </summary>
        public void CloseUI() {
            IsShown = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Process the queues for things, which should be created.
        /// Hint: This method must be canged for your own needs. Building one building at a time
        /// it acceptable, but building units should be assigned to producing building,
        /// and one producing building should not build two units the same time.
        /// </summary>
        void process() {

            // Building will be built in order, cannot build 2 buildings concurrently.
            // The buildings also depends on each other, that is defined by the graph.
            if (buildingQueue.Count > 0) {
                if (buildingQueue[0].Consumer.Result == ConditionBase.ConditionResult.SuccessType) {
                    build(buildingQueue[0]);
                }
            }

            // Units can be built concurrently in this example.
            foreach (BuildQueueEntry unit in unitQueue) {
                if (unit.Consumer.Result == ConditionBase.ConditionResult.SuccessType) {
                    build(unit);
                }
            }
        }

        void build(BuildQueueEntry entry) {
            entry.Item.Build(entry);
        }

    }

    /// <summary>
    /// One UI entry on the UI dialog.
    /// </summary>
    [System.Serializable]
    public class BuildQueueEntry {

        public BuildTreeItem Item;
        public ConditionUser Consumer;

        public BuildQueueEntry(BuildTreeItem item, ConditionUser consumer) {
            Consumer = consumer;
            Item = item;
        }
    }

}

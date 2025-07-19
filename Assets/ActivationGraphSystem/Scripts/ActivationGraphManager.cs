using UnityEngine;
using System.Collections.Generic;


namespace ActivationGraphSystem {
    /// <summary>
    /// The activation graph manager controls the activations to the ancestors.
    /// This is a singleton class, which can be called from everywhere.
    /// This class also notifies the MissionGuiManager, if it is necessary.
    /// </summary>
    public class ActivationGraphManager : MonoBehaviour {

        public string ShortDescription = "";

        // Coordinate for the node editor. Current position of the view.
        public Vector2 PosInNodeEditor = new Vector2(10000, 10000);

        [Header("Defines also the starting tasks.")]
        public List<TaskBase> CurrentTasks = new List<TaskBase>();
        public List<TaskDataNode> Tasks = new List<TaskDataNode>();

        // For container, if they exists they wil be collected onder this node.
        public Dictionary<string, ContainerBase> Containers = new Dictionary<string, ContainerBase>();
        public List<ConditionUser> ConsumerUserConditions = new List<ConditionUser>();

        // Changed by the editor. The user can change this too, for knowing what kind of graph is it.
        public BaseNode.Types DefaultType = BaseNode.Types.None;

        // E.g. initialize UI, create UI entries from nodes, etc...
        // Will be called once
        public delegate void InitializeMulticast();
        public InitializeMulticast initializeMulticast;
        // If one of the TaskDataNode changes its state.
        public delegate void TaskDataStateChangedMulticast(TaskDataNode task);
        public TaskDataStateChangedMulticast taskDataStateChangedMulticast;
        // If one of the conditions changes its state.
        public delegate void ConditionStateChangedMulticast(ConditionBase condition);
        public ConditionStateChangedMulticast conditionStateChangedMulticast;

        // The following list are colors in the order of the types.
        public List<Color32> TypeColors = new List<Color32>() {
            new Color32(0, 128, 255, 255),
            new Color32(33, 212, 212, 255),
            new Color32(90, 219, 90, 255),
            new Color32(255, 0, 0, 255),
            new Color32(167, 86, 0, 255), 
			new Color32(219, 207, 58, 255),
			new Color32(246, 128, 22, 255),
			new Color32(246, 200, 22, 255),
			new Color32(246, 64, 22, 255),
			new Color32(246, 128, 120, 255) };


        /// <summary>
        /// Sets the singleton attribute.
        /// </summary>
	    protected void Awake() {
			if (ActivationGraphsManager.Instance)
		    	ActivationGraphsManager.Instance.Managers.Add(gameObject.name, this);
        	
            BaseNode[] nodes = GetComponentsInChildren<BaseNode>();
            foreach (BaseNode node in nodes) {
                node.AGM = this;
            }

            ContainerBase[] containers = GetComponentsInChildren<ContainerBase>();
            foreach (ContainerBase container in containers) {
                if (!Containers.ContainsKey(container.gameObject.name)) {
                    Containers.Add(container.gameObject.name, container);
                } else {
                    Debug.LogError("ActivationGraphManager: Container with the name '" + container.gameObject.name +
                        "' already exists. The name must be unique under the ActivationGraphManager gameobject!");
                }
            }

            ConditionUser[] possibleConsumerUserConditions = GetComponentsInChildren<ConditionUser>();
            foreach (ConditionUser cond in possibleConsumerUserConditions) {
                if (cond.EnableContainerAccess) {
                    ConsumerUserConditions.Add(cond);
                }
            }
        }

        /// <summary>
        /// Returns a unique container name.
        /// </summary>
        /// <param name="baseName"></param>
        /// <returns></returns>
        public string CreateUniqueContainerName(string baseName) {
            string newName = baseName;
            int index = 0;
            bool isNameUnique = false;

            while (!isNameUnique && Containers.Count > 0) {
                foreach (KeyValuePair<string, ContainerBase> pair in Containers) {
                    if (newName == pair.Key) {
                        newName = baseName + "_" + index;
                        index++;
                        isNameUnique = false;
                        break;
                    } else {
                        isNameUnique = true;
                    }
                }
            }

            return newName;
        }

        /// <summary>
        /// Starts the current tasks and creates the task order if not already available.
        /// </summary>
        protected void Start() {

            CleanUpTasks();

            // We need a list of tasks for e.g. the mission dialog.
            // If the list were set manually, then the order won't be changed.
            // In this way nodes can be hidden.
            if (Tasks.Count == 0)
                CreateTaskOrder();
            
            if (initializeMulticast != null) {
                initializeMulticast();
            }

            foreach (TaskBase task in CurrentTasks) {
                task.StartTask();
            }
        }

        /// <summary>
        /// If a task is successfully, then its ancestors will be activated.
        /// E.g. the MissionGuiController will be actualized.
        /// </summary>
        /// <param name="taskBase"></param>
        public void TaskSuccessed(TaskBase taskBase) {

            taskBase.StopTask();
            if (CurrentTasks.Contains(taskBase))
                CurrentTasks.Remove(taskBase);

            OperatorNode op = null;
            if (taskBase is OperatorNode)
                op = (OperatorNode)taskBase;

            if (op && op.IsSuccessOutRandom) {
                int minOut = Mathf.Min(op.MinActSuccOutgoing, op.TasksActivatedAfterSuccess.Count);
                int maxOut = Mathf.Max(Mathf.Min(op.MaxActSuccOutgoing, op.TasksActivatedAfterSuccess.Count), minOut);
                // Roll the amount of outgoing activations.
                int count = Random.Range(minOut, maxOut + 1);

                List<TaskBase> rollOfthem = new List<TaskBase>(op.TasksActivatedAfterSuccess);
                for (int i = 0; i < count; i++) {

                    float probSum = 0;
                    foreach (TaskBase mtb in rollOfthem) {
                        probSum += op.SuccessedAncestorProbabbility[op.TasksActivatedAfterSuccess.IndexOf(mtb)];
                    }

                    float rolledValue = Random.Range(0, probSum);
                    float probStartValue = 0;
                    TaskBase selectedTask = null;
                    foreach (TaskBase mtb in rollOfthem) {

                        float probEndValue = probStartValue + op.SuccessedAncestorProbabbility[op.TasksActivatedAfterSuccess.IndexOf(mtb)];
                        if (rolledValue >= probStartValue && rolledValue <= probEndValue) {

                            // When starts, because of task preconditions.
                            if (mtb.StartTask() && !CurrentTasks.Contains(mtb)) {
                                CurrentTasks.Add(mtb);
                            }

                            selectedTask = mtb;
                            break;
                        }

                        probStartValue = probEndValue;
                    }

                    rollOfthem.Remove(selectedTask);
                }
            } else {
                foreach (TaskBase task in taskBase.TasksActivatedAfterSuccess) {
                    // When starts, because of task preconditions.
                    if (task.StartTask() && !CurrentTasks.Contains(task)) {
                        CurrentTasks.Add(task);
                    }
                }
            }
        }

        /// <summary>
        /// If a task is failed, then its ancestors will be activated.
        /// E.g. the MissionGuiController will be actualized.
        /// </summary>
        /// <param name="taskBase"></param>
        public void TaskFailed(TaskBase taskBase) {

            taskBase.StopTask();
            if (CurrentTasks.Contains(taskBase))
                CurrentTasks.Remove(taskBase);

            OperatorNode op = null;
            if (taskBase is OperatorNode)
                op = (OperatorNode)taskBase;

            if (op && op.IsFailureOutRandom) {
                int minOut = Mathf.Min(op.MinActFailOutgoing, op.TasksActivatedAfterFailed.Count);
                int maxOut = Mathf.Max(Mathf.Min(op.MaxActFailOutgoing, op.TasksActivatedAfterFailed.Count), minOut);
                // Roll the amount of outgoing activations.
                int count = Random.Range(minOut, maxOut + 1);

                List<TaskBase> rollOfthem = new List<TaskBase>(op.TasksActivatedAfterFailed);
                for (int i = 0; i < count; i++) {

                    float probSum = 0;
                    foreach (TaskBase mtb in rollOfthem) {
                        probSum += op.FailedAncestorProbabbility[op.TasksActivatedAfterFailed.IndexOf(mtb)];
                    }

                    float rolledValue = Random.Range(0, probSum);
                    float probStartValue = 0;
                    TaskBase selectedTask = null;
                    foreach (TaskBase mtb in rollOfthem) {

                        float probEndValue = probStartValue + op.FailedAncestorProbabbility[op.TasksActivatedAfterFailed.IndexOf(mtb)];
                        if (rolledValue >= probStartValue && rolledValue <= probEndValue) {

                            // When starts, because of task preconditions.
                            if (mtb.StartTask() && !CurrentTasks.Contains(mtb)) {
                                CurrentTasks.Add(mtb);
                            }

                            selectedTask = mtb;
                            break;
                        }

                        probStartValue = probEndValue;
                    }

                    rollOfthem.Remove(selectedTask);
                }
            } else {
                foreach (TaskBase task in taskBase.TasksActivatedAfterFailed) {
                    // When starts, because of task preconditions.
                    if (task.StartTask() && !CurrentTasks.Contains(task)) {
                        CurrentTasks.Add(task);
                    }
                }
            }
        }


        /// <summary>
        /// If a task has been disabled, then remove it from current tasks and stop the task.
        /// E.g. the MissionGuiController will be actualized.
        /// </summary>
        /// <param name="taskBase"></param>
        public void TaskDisabled(TaskBase taskBase) {

            taskBase.StopTask();
            if (CurrentTasks.Contains(taskBase))
                CurrentTasks.Remove(taskBase);
        }

        /// <summary>
        /// Returns true if the task starts at the beginning.
        /// At the beginnings the CurrentTasks contains the tasks, which should be started at the beginning.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public bool IsInActiveMode(TaskBase entry) {
            if (CurrentTasks.Contains(entry)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set the start at the beginning option for the task.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="startIt"></param>
        public void SetInActiveMode(TaskBase entry, bool startIt) {
            if (startIt) {
                if (!CurrentTasks.Contains(entry)) {
                    CurrentTasks.Add(entry);
                }
            } else {
                if (CurrentTasks.Contains(entry)) {
                    CurrentTasks.Remove(entry);
                }
            }
        }

        public void CleanUpTasks() {
            for (int i=Tasks.Count-1; i >= 0; i--) {
                if (Tasks[i] == null)
                    Tasks.RemoveAt(i);
            }
        }

        /// <summary>
        /// Creates an order just for the data task nodes with description.
        /// </summary>
        public void CreateTaskOrder() {

            // Needed for editor.
            CleanUpTasks();

            List<TaskBase> alreadySeen = new List<TaskBase>();
            foreach (TaskBase task in CurrentTasks) {
                alreadySeen.Add(task);
                createTaskOrderRecursive(task, alreadySeen);
            }

            // Add non reachable tasks at the end, the user can still activate the from source code.
            TaskDataNode[] nonReachableTasks = GetComponentsInChildren<TaskDataNode>();
            foreach (TaskDataNode task in nonReachableTasks) {
                if (!Tasks.Contains(task)) {
                    Tasks.Add(task);
                }
            }
        }

        /// <summary>
        /// Helper method for creating the task order.
        /// </summary>
        /// <param name="tasks"></param>
        void createTaskOrderRecursive(TaskBase task, List<TaskBase> alreadySeen) {
            alreadySeen.Add(task);
            if (task is TaskDataNode) {
                TaskDataNode missTask = (TaskDataNode)task;
                if (!Tasks.Contains(missTask)) {
                    Tasks.Add(missTask);
                }
            }

            foreach (TaskBase task2 in task.TasksActivatedAfterSuccess) {
                if (!alreadySeen.Contains(task2))
                    createTaskOrderRecursive(task2, alreadySeen);
            }

            foreach (TaskBase task2 in task.TasksActivatedAfterFailed) {
                if (!alreadySeen.Contains(task2))
                    createTaskOrderRecursive(task2, alreadySeen);
            }
        }

        /// <summary>
        /// Shuts down the ActivationGraphManager and all its nodes. Called at the final nodes (Victory and Failure nodes).
        /// </summary>
        public void ShutDown() {
            Task[] taskNodes = gameObject.GetComponentsInChildren<Task>();
            foreach (Task node in taskNodes) {
                node.StopTask();
            }
        }

        /// <summary>
        /// Resets the nodes to the initial state.
        /// </summary>
        public void Reset() {
            BaseNode[] nodes = gameObject.GetComponentsInChildren<BaseNode>();
            foreach (BaseNode node in nodes) {
                node.Reset();
            }
        }

        public void NotifyTaskStateChanged(TaskDataNode task) {
            if (taskDataStateChangedMulticast != null) {
                taskDataStateChangedMulticast(task);
            }
        }

        public void NotifyConditionStateChanged(ConditionBase condition) {
            if (conditionStateChangedMulticast != null) {
                conditionStateChangedMulticast(condition);
            }
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the user condition node. 
    /// It has a timer and a counter which ment to be controlled by the user.
    /// </summary>
    public class ConditionUser : ConditionBase {

        // Timer time can be set internally and externally.
        public bool TimerSignalFailure = false;

        // Counter which can be caunted externaly. If it reaches 0,
        // then it fires.
        [Header("Counter which can be counted externaly.")]
        public bool EnableCounter = false;
        [Header("Counts down to 0 and fires.")]
        public int CounterValue = 10;
        public int CurrentCounterValue = 0;
        [Header("Set the counter signal to failure")]
        public bool CounterSignalFailure = false;

        public bool EnableContainerAccess = false;
        public string ContainerName = "CM";
        public List<ItemRef> Items = new List<ItemRef>();
        // Speed up accessing the items by name.
        private Dictionary<string, ItemRef> NameItemDict = new Dictionary<string, ItemRef>();

        private bool result = false;
        // For the editor. Determines the expanded state in the task nodes.
        public bool IsExpanded = true;

        // Set to true for debug information.
        private bool doDebug = false;


        protected override void Awake() {
            base.Awake();

            ActualizeDictionary();
        }

        /// <summary>
        /// Dictionary cannot be serialized. It is needed for speed up the name/item search.
        /// Must be actualized at starting the game and in editor mode too.
        /// </summary>
        protected void ActualizeDictionary() {
            NameItemDict.Clear();
            foreach (ItemRef item in Items) {
                NameItemDict.Add(item.GetName(), item);
            }
        }
         
        /// <summary>
        /// Starts the condition, normally the task calls this method.
        /// </summary>
        override public void StartCondition(Task task) {
            // Strat timer
            startTime = Time.time;
            CurrentCounterValue = CounterValue;

            base.StartCondition(task);
        }

        /// <summary>
        /// Checks the condition, the task based instances call this method.
        /// </summary>
        override public ConditionResult CheckState(bool updateNonPeriodicallyCheckedConditions) {
            if (stopCondition || Result == ConditionResult.InactiveType) {
                return Result;
            }

            bool endStateReached = true;
            bool hasCheck = EnableTimer || EnableCounter || EnableContainerAccess;

            // Trigger based conditions never check the other requirements. Only PostProcess method will be called
            // after it had been triggered.
            //if (IsTrigger && Result == ConditionResult.SuccessType)
            //    hasCheck = true;

            // Check timer.
            if (EnableTimer) {
                CurrentTimerValue = TimerValue - (Time.time - startTime);
                if (startTime + TimerValue <= Time.time) {
                    if (TimerSignalFailure) {
                        setState(ConditionResult.FailureType);
                        // Result will be set indirectly with setState method.
                        return Result;
                    }

                    endStateReached &= true;
                } else {
                    endStateReached &= false;
                }
            }

            if (updateNonPeriodicallyCheckedConditions) {

                if (EnableCounter) {
                    if (CurrentCounterValue <= 0) {
                        if (CounterSignalFailure) {
                            setState(ConditionResult.FailureType);
                            // Result will be set indirectly with setState method.
                            return Result;
                        }

                        endStateReached &= true;
                    } else {
                        endStateReached &= false;
                    }
                }

                if (EnableContainerAccess) {
                    if (Items.Count == 0) {
                        // Skip if no items attached.
                        endStateReached &= true;

                    } else if (AGM.Containers.ContainsKey(ContainerName)) {
                        ContainerBase cont = AGM.Containers[ContainerName];
                        foreach (ItemRef item in Items) {
                            if (doDebug)
                                Debug.Log("check: " + cont.ModifyValue(item.GetName(), item.Value, 1, true) +
                                    " product: " + item.GetName() + " user condition: " + this.name);
                            if (cont.ModifyValue(item.GetName(), item.Value, 1, true) != 0) {
                                // Requirement not met!
                                result = false;
                                if (doDebug)
                                    Debug.Log("FAILED");
                                break;
                            } else {
                                if (doDebug)
                                    Debug.Log("OK");
                                result = true;
                            }
                        }

                        // We have all the items.
                        if (result) {
                            endStateReached &= true;
                        } else {
                            endStateReached &= false;
                            setState(ConditionResult.RunningType);
                            return Result;
                        }

                    } else {
                        Debug.LogError("ActivationGraphSystem, Container with the name '" + ContainerName + "' does not exist.");
                    }
                }
            }

            if (!hasCheck)
                endStateReached &= false;

            if (endStateReached) {
                setState(ConditionResult.SuccessType);
            }

            // Result will be set indirectly with setState method.
            return Result;
        }

        /// <summary>
        /// This method is called after the task reaches its end state (Success or Failure).
        /// </summary>
        public override bool PostProcess() {
            
            if (result) {
                if (EnableContainerAccess && AGM.Containers.ContainsKey(ContainerName)) {
                    ContainerBase cont = AGM.Containers[ContainerName];
                    // We have all the items.
                    // Remove the item value from the container.
                    foreach (ItemRef item in Items) {
                        if (item.DoConsume && !item.DoConsumeManually) {
                            cont.ModifyValue(item.GetName(), item.Value);
                        }
                    }
                }
                result = false;
            }

            return false;
        }

        /// <summary>
        /// Consumes manually consumable items.
        /// Needed to first remove the necessary items, then wait in the gui and animate creating the item,
        /// and then call this method to process consuming or insrting item values.
        /// </summary>
        /// <returns></returns>
        public void ConsumeExternallyMarkedItems() {
            if (EnableContainerAccess && AGM.Containers.ContainsKey(ContainerName)) {
                ContainerBase cont = AGM.Containers[ContainerName];
                // We have all the items.
                // Remove the item value from the container.
                foreach (ItemRef item in Items) {
                    if (item.DoConsume && item.DoConsumeManually) {
                        cont.ModifyValue(item.GetName(), item.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Timer time can be set internally and externally. Count down to 0, then fires.
        /// </summary>
        /// <param name="time"></param>
        public void SetTimerTime(float time) {
            TimerValue = time;

            // Set the condition to running state to re-evaulate the value.
            if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType))
                setState(ConditionResult.RunningType);
        }

        /// <summary>
        /// The counter can be set internally and externally. Count down to 0, then fires.
        /// </summary>
        /// <param name="counterValue"></param>
        public void SetCounter(int counterValue) {

            if (CounterValue == counterValue)
                return;

            CounterValue = counterValue;
            CurrentCounterValue = counterValue;

            // Set the condition to running state or force notification to re-evaulate the value.
            if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                setState(ConditionResult.RunningType);
                // If no state change, then force the notification.
                if (Result == ConditionResult.RunningType)
                    NotityConnectedTasks();
            }
        }

        /// <summary>
        /// The user can increase the counter with this method.
        /// </summary>
        /// <param name="value"></param>
        public void IncreaseCounter(int value = 1) {

            if (value == 0)
                return;

            CurrentCounterValue += value;

            // Set the condition to running state or force notification to re-evaulate the value.
            if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                setState(ConditionResult.RunningType);
                // If no state change, then force the notification.
                if (Result == ConditionResult.RunningType)
                    NotityConnectedTasks();
            }
        }

        /// <summary>
        /// The user can decrease the counter with this method.
        /// </summary>
        /// <param name="value"></param>
        public void DecreaseCounter(int value = 1) {

            if (value == 0)
                return;

            CurrentCounterValue -= value;

            // Set the condition to running state or force notification to re-evaulate the value.
            if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                setState(ConditionResult.RunningType);
                // If no state change, then force the notification.
                if (Result == ConditionResult.RunningType)
                    NotityConnectedTasks();
            }
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            setState(ConditionResult.InactiveType);
        }

        public void AddItem(string name, int value, bool doConsume, bool doConsumeManually) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (!NameItemDict.ContainsKey(name)) {
                ItemRef ir = new ItemRef(name, value, doConsume, doConsumeManually);
                Items.Add(ir);
                NameItemDict.Add(name, ir);

                // Set the condition to running state or force notification to re-evaulate the value.
                if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                    setState(ConditionResult.RunningType);
                    // If no state change, then force the notification.
                    if (Result == ConditionResult.RunningType)
                        NotityConnectedTasks();
                }
            } else {
                Debug.LogError("ActivationGraphSystem: This item already exists: " + name);
            }
        }

        public void RemoveItem(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(name)) {
                ItemRef thisItem = NameItemDict[name];
                Items.Remove(thisItem);
                NameItemDict.Remove(name);

                // Set the condition to running state or force notification to re-evaulate the value.
                if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                    setState(ConditionResult.RunningType);
                    // If no state change, then force the notification.
                    if (Result == ConditionResult.RunningType)
                        NotityConnectedTasks();
                }
            } else {
                Debug.LogError("ActivationGraphSystem: No such item available: " + name);
            }
        }

        public float GetItemValue(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(name)) {
                ItemRef thisItem = NameItemDict[name];
                return thisItem.Value;
            }

            Debug.LogError("ActivationGraphSystem: No such item available: " + name);
            return 0;
        }

        public void ChangeValue(string name, int value) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(name)) {
                ItemRef thisItem = NameItemDict[name];
                thisItem.Value = value;

                // Set the condition to running state or force notification to re-evaulate the value.
                if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                    setState(ConditionResult.RunningType);
                    // If no state change, then force the notification.
                    if (Result == ConditionResult.RunningType)
                        NotityConnectedTasks();
                }
            } else {
                Debug.LogError("ActivationGraphSystem: No such item available: " + name);
            }
        }

        public string GetUniqeItemName(string baseName = "Item") {
            string newName = baseName;
            int index = 0;
            bool isNameUnique = false;

            while (!isNameUnique && Items.Count > 0) {
                foreach (ItemRef item in Items) {
                    if (newName == item.GetName()) {
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

        public void SetItemName(ItemRef item, string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(item.GetName())) {
                // Synchronize the dictionary with the changed name too.
                NameItemDict.Remove(item.GetName());
                NameItemDict.Add(name, item);
                item.SetName(name);

                // Set the condition to running state or force notification to re-evaulate the value.
                if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                    setState(ConditionResult.RunningType);
                    // If no state change, then force the notification.
                    if (Result == ConditionResult.RunningType)
                        NotityConnectedTasks();
                }
            } else {
                Debug.LogError("ActivationGraphManager: Internal error, no item with the name '" + item.GetName() +
                    "' available. " + "Dictionary is asynchron to the list.");
            }
        }

        public void InsertItem(int index, ItemRef item) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (!NameItemDict.ContainsKey(item.GetName())) {
                // Synchronize the dictionary with the changed name too.
                NameItemDict.Add(item.GetName(), item);
                Items.Insert(index, item);

                // Set the condition to running state or force notification to re-evaulate the value.
                if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                    setState(ConditionResult.RunningType);
                    // If no state change, then force the notification.
                    if (Result == ConditionResult.RunningType)
                        NotityConnectedTasks();
                }
            } else {
                Debug.LogError("ActivationGraphManager: Internal error, item with the name '" + item.GetName() +
                    "' already exists. " + "Dictionary is asynchron to the list.");
            }
        }

        public void RemoveItemAt(int index) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (index >= Items.Count) {
                Debug.LogError("ActivationGraphSystem: index out of range: " + index);
            }

            ItemRef item = Items[index];
            if (item == null) {
                Debug.LogError("ActivationGraphSystem: item at iondex '" + index + "' is null'");
            }

            string name = item.GetName();

            if (NameItemDict.ContainsKey(name)) {
                NameItemDict.Remove(name);
                Items.Remove(item);

                // Set the condition to running state or force notification to re-evaulate the value.
                if (!(stopCondition || Result == ConditionResult.RunningType || Result == ConditionResult.InactiveType)) {
                    setState(ConditionResult.RunningType);
                    // If no state change, then force the notification.
                    if (Result == ConditionResult.RunningType)
                        NotityConnectedTasks();
                }
            } else {
                Debug.LogError("ActivationGraphSystem: No such item available: " + name);
            }
        }

        public bool ContainsItemName(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            return NameItemDict.ContainsKey(name);
        }

        public ItemRef GetItemByName(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            return NameItemDict[name];
        }
    }

    [System.Serializable]
    public class ItemRef {
        [SerializeField]
        private string Name;
        public int Value;
        public bool DoConsume;
        public bool DoConsumeManually;

        public ItemRef(string name, int value, bool doConsume, bool doConsumeManually) {
            Name = name;
            Value = value;
            DoConsume = doConsume;
            DoConsumeManually = doConsumeManually;
        }

        public string GetName() {
            return Name;
        }

        public void SetName(string name) {
            Name = name;
        }
    }
}

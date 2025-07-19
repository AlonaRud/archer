using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represents the union of Containers, which change values of items
	/// in Containers in a defined order.
    /// </summary>
    public class ContainerManager : ContainerBase {
		
        public string ShortDescription = "Container manager node.";

        // Containers in a order. Item values will be added descending.
		// Items values will be removed ascending.
		// This means, the last Container in the list has the highest priority,
		// it will be filled at first and the item values will be removed at last.
		public List<ContainerBase> Containers = new List<ContainerBase>();
        
        /// Higher number, lower priority.
        public List<KeyValuePair<int, ContainerBase>> ValueIncreasingPrioDict = 
            new List<KeyValuePair<int, ContainerBase>>();
        /// Higher number, lower priority.
        public List<KeyValuePair<int, ContainerBase>> ValueDecreasingPrioDict = 
            new List<KeyValuePair<int, ContainerBase>>();

        // For the editor.
        public List<ContainerInfo> ContainerInfos = new List<ContainerInfo>();

        public bool WeightBasedFillingForSamePriorities = false;

        public bool doDebug = false;


        /// <summary>
        /// Unity awake method.
        /// </summary>
        protected override void Awake() {
			base.Awake();
        }

        /// <summary>
        /// Unity start method.
        /// </summary>
        protected void Start() {
            foreach (ContainerInfo info in ContainerInfos) {
				if (AGM.Containers.ContainsKey(info.Name)) {
					ContainerBase container = AGM.Containers[info.Name];
                    Containers.Add(container);
					container.IncreasingPriotity = info.IncreasingPriority;
					container.DecreasingPriotity = info.DecreasingPriority;
					container.IsIncreasingLocked = info.IsIncreasingLocked;
					container.IsDecreasingLocked = info.IsDecreasingLocked;
                    container.IncreasingWeight = info.IncreasingWeight;
                    container.DecreasingWeight = info.DecreasingWeight;
                } else {
					Debug.LogError("Container not found with the name: " + info.Name + 
                        ". Maybe you have errors about unique name issues before this message?");
                }
            }

            UpdatePriorities();
        }

        /// <summary>
        /// Generates a unique priority for the new container.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public int GenerateUniqueIncreasingPriority(int priority) {
            foreach (ContainerInfo info in ContainerInfos) {
                if (info.IncreasingPriority == priority) {
                    priority++;
                }
            }
            return priority;
        }

        /// <summary>
        /// Generates a unique priority for the new container.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public int GenerateUniqueDecreasingPriority(int priority) {
            foreach (ContainerInfo info in ContainerInfos) {
                if (info.DecreasingPriority == priority) {
                    priority++;
                }
            }
            return priority;
        }

        /// <summary>
        /// Update the priorities, if necessary. E.g. at inserting runtime a new container,
        /// or at initialization (in Start() method).
        /// </summary>
        public void UpdatePriorities() {
            ValueIncreasingPrioDict.Clear();
            ValueDecreasingPrioDict.Clear();
            foreach (ContainerBase cont in Containers) {
                ValueIncreasingPrioDict.Add(new KeyValuePair<int, ContainerBase>(cont.IncreasingPriotity, cont));
                ValueDecreasingPrioDict.Add(new KeyValuePair<int, ContainerBase>(cont.DecreasingPriotity, cont));
            }

            ValueIncreasingPrioDict.Sort(new MultipleKeyComparer<int, ContainerBase>());
            ValueDecreasingPrioDict.Sort(new MultipleKeyComparer<int, ContainerBase>());
        }

        /// <summary>
        /// Return the sum of the item values from the the conencted container.
        /// </summary>
        /// <returns>The item value.</returns>
        /// <param name="name">Name.</param>
        public override int GetItemValue(string name) {
			int value = 0;
			bool itemExistsInOneContainer = false;
			foreach (ContainerBase c in Containers) {
                if (c is Container) {
                    Container cont = (Container)c;
                    if (cont.ContainsItemName(name)) {
                        ContainerItem thisItem = cont.GetItemByName(name);
                        value += thisItem.Value;
                        itemExistsInOneContainer = true;
                    }
                } else if (c is ContainerManager) {
                    ContainerManager cont = (ContainerManager)c;
                    cont.GetItemValue(name);
                }
			}

			if (!itemExistsInOneContainer) {
				Debug.LogError("ActivationGraphSystem: No such item available: " + name);
				return 0;
			}

			return value;
        }

        /// <summary>
        /// Return the sum of the item max weights from the the conencted container.
        /// </summary>
        /// <returns>The item max weight.</returns>
        /// <param name="name">Name.</param>
        public override float GetItemMaxWeight(string name) {
            int value = 0;
            bool itemExistsInOneContainer = false;
            foreach (ContainerBase c in Containers) {
                if (c is Container) {
                    Container cont = (Container)c;
                    if (cont.ContainsItemName(name)) {
                        ContainerItem thisItem = cont.GetItemByName(name);
                        value += thisItem.Value;
                        itemExistsInOneContainer = true;
                    }
                } else if (c is ContainerManager) {
                    ContainerManager cont = (ContainerManager)c;
                    cont.GetItemMaxWeight(name);
                }
            }

            if (!itemExistsInOneContainer) {
                Debug.LogError("ActivationGraphSystem: No such item available: " + name);
                return 0;
            }

            return value;
        }

        /// <summary>
        /// Return the items by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override List<ContainerItem> GetItems(string name) {
            List<ContainerItem> items = new List<ContainerItem>();
            foreach (ContainerBase c in Containers) {
                if (c is Container) {
                    Container cont = (Container)c;
                    if (cont.ContainsItemName(name)) {
                        ContainerItem thisItem = cont.GetItemByName(name);
                        items.Add(thisItem);
                    }
                } else if (c is ContainerManager) {
                    ContainerManager cont = (ContainerManager)c;
                    items.AddRange(cont.GetItems(name));
                }
            }

            return items;
        }

        /// <summary>
        /// Returns all items
        /// </summary>
        /// <returns></returns>
        public override List<ContainerItem> GetAllItems() {
            List<ContainerItem> items = new List<ContainerItem>();
            foreach (ContainerBase cont in Containers) {
                items.AddRange(cont.GetAllItems());
            }
            return items;
        }

        /// <summary>
        /// Needed to get all item types.
        /// </summary>
        /// <returns></returns>
        public override List<string> GetAllItemNames() {
            List<string> items = new List<string>();
            foreach (ContainerBase cont in Containers) {
                List<string> items2 = cont.GetAllItemNames();
                foreach (string item in items2) {
                    if (!items.Contains(item)) {
                        items.Add(item);
                    }
                }
            }
            return items;
        }

        /// <summary>
        /// Needed to get all item names once, but with value sum.
        /// </summary>
        /// <returns></returns>
        public override Dictionary<string, int> GetAllItemNamesWithValue() {
            Dictionary<string, int> items = new Dictionary<string, int>();
            foreach (ContainerBase cont in Containers) {
                Dictionary<string, int> items2 = cont.GetAllItemNamesWithValue();
                foreach (KeyValuePair<string, int> item in items2) {
                    if (!items.ContainsKey(item.Key)) {
                        items.Add(item.Key, item.Value);
                    } else {
                        items[item.Key] += item.Value;
                    }
                }
            }
            return items;
        }

        /// <summary>
        /// Modifies the value. Additionally it checks the value if dryRun is activated.
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="value"></param>
        /// <param name="dryRun"></param>
        /// <returns></returns>
        public override int ModifyValue(string name, int value, int resourceDistributionDivider = 1, bool dryRun = false, float maxIncWeight = 1, float maxDecWeight = 1) {

            // If this container manager does not set to weight based distribution, then ignore the resourceDistributionDivider.
            if (WeightBasedFillingForSamePriorities) {
                if (resourceDistributionDivider > 1)
                    value = Mathf.CeilToInt((float)value / (float)resourceDistributionDivider);
            }

            // Check what we are doing, increasing or decreasing the value.
            // Iterate the correct priority dictionary.
            List<KeyValuePair<int, ContainerBase>> PrioDict = new List<KeyValuePair<int, ContainerBase>>();
            if (value > 0) {
                PrioDict = ValueIncreasingPrioDict;
            } else if (value < 0) {
                PrioDict = ValueDecreasingPrioDict;
            } else {
                return 0;
            }

            bool itemChanged = false;
			int rest = value;
            int index = 0;

            if (!dryRun) {
                foreach (KeyValuePair<int, ContainerBase> pair in PrioDict) {
                    ContainerBase cont = pair.Value;

                    int indexAsResDivider = 1;
                    float currentMaxIncWeight = 1;
                    float currentMaxDecWeight = 1;
                    if (WeightBasedFillingForSamePriorities) {
                        indexAsResDivider = countSameKeysAfter(PrioDict, pair.Key, index, ref currentMaxIncWeight, ref currentMaxDecWeight);
                    }

                    if (doDebug)
                        Debug.Log("Divider: " + indexAsResDivider + " rest: " + rest);

                    int newRest = cont.ModifyValue(name, rest, indexAsResDivider, dryRun, currentMaxIncWeight, currentMaxDecWeight);
                    // To recognize changes.
                    if (newRest != rest)
                        itemChanged = true;
                    rest = newRest;

                    if (rest == 0) {
                        // No more value to distribute and value already distributed.
                        break;
                    }
                    index++;
                }

                // Now redistribute again without weight, maybe one of the container can take more.
                if (WeightBasedFillingForSamePriorities && rest != 0) {
                    foreach (KeyValuePair<int, ContainerBase> pair in PrioDict) {
                        ContainerBase cont = pair.Value;

                        int newRest = cont.ModifyValue(name, rest, 1, dryRun);
                        // To recognize changes.
                        if (newRest != rest)
                            itemChanged = true;
                        rest = newRest;

                        if (rest == 0) {
                            // No more value to distribute and value already distributed.
                            break;
                        }
                    }
                }
            } else {

                // For just checking the values this is the fastest. No weight check is needed!
                foreach (KeyValuePair<int, ContainerBase> pair in PrioDict) {
                    ContainerBase cont = pair.Value;

                    int newRest = cont.ModifyValue(name, value, 1, dryRun);
                    // To recognize changes.
                    if (newRest != rest)
                        itemChanged = true;
                    rest = newRest;

                    if (rest == 0) {
                        // No more value to distribute and value already distributed.
                        break;
                    }
                }

            }

            if (activateAction != null && itemChanged) {
                valueChangedAction(this, name, value);
            }

            return rest;
        }

        /// <summary>
        /// Count the same keys from the current index.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        protected int countSameKeysAfter(List<KeyValuePair<int, ContainerBase>> dict, int key, int pos, ref float maxIncWeight, ref float maxDecWeight) {
            int countSameKeyAfter = 0;
            maxIncWeight = 0;
            maxDecWeight = 0;

            for (int i = pos; i < dict.Count; i++) {
                if (dict[i].Key != key) {
                    break;
                }
                maxIncWeight += dict[i].Value.IncreasingWeight;
                maxDecWeight += dict[i].Value.DecreasingWeight;
                countSameKeyAfter++;
            }
            return countSameKeyAfter;
        }

        public override float GetMaxGlobalWeight() {
			float weight = 0;
			foreach (ContainerBase cont in Containers) {
				weight += cont.GetMaxGlobalWeight();
			}
			return weight;
		}

        /// <summary>
        /// Returns the short description.
        /// </summary>
        /// <returns></returns>
        public override string GetShortDescription() {
            return ShortDescription;
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            // Do nothing.
        }

        /// <summary>
        /// Re-distribute the items based on the inserting priority.
        /// Higher number, lower priority.
        /// </summary>
        public override void ReDistributeItemVaues(Dictionary<string, int> items) {
            // 1. Values for items are already collected. (Done)
            // 2. Remove all the values from the items.
            List<ContainerItem> allItems = GetAllItems();
            foreach (ContainerItem cont in allItems) {
                cont.Value = 0;
            }

            // 3. Add them all again to the container.
            foreach (KeyValuePair<string, int> pair in items) {
                ModifyValue(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Re-distribute all items based on the inserting priority.
        /// </summary>
        public override void ReDistributeAllItemVaues() {
            Dictionary<string, int> items = GetAllItemNamesWithValue();
            ReDistributeItemVaues(items);
        }

        /// <summary>
        /// Re-distribute one item based on the inserting priority.
        /// </summary>
        public override void ReDistributeOneItemVaues(string name) {
            Dictionary<string, int> items = GetAllItemNamesWithValue();
            // Filter just for the one item.
            Dictionary<string, int> items2 = new Dictionary<string, int>();
            foreach (KeyValuePair<string, int> item in items) {
                if (item.Key == name)
                    items2.Add(item.Key, item.Value);
            }
            ReDistributeItemVaues(items2);
        }

        /// <summary>
        /// References a new container for the container manager.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ContainerBase AddContainerRef(string name) {
            if (!AGM.Containers.ContainsKey(name)) {
                Debug.LogError("ActivationGraphSystem: No container available with the name: " + name);
                return null;
            }

            ContainerBase cont = AGM.Containers[name];

            if (Containers.Contains(cont)) {
                Debug.LogError("ActivationGraphSystem: Already contains the container with the name: " + name);
                return null;
            }

            Containers.Add(cont);

            // Update all user conditions for the included items.
            List<string> items = cont.GetAllItemNames();
            foreach (string item in items) {
                UpdateConsumerConditions(item);
            }

            UpdatePriorities();

            return cont;
        }

        /// <summary>
        /// Add a ContainerInfo to provide a better debug ability in the graph at running mode.
        /// The dynamically referenced container will be visible.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public ContainerBase AddContainerRef(ContainerInfo info) {
            ContainerBase container = null;
            if (AGM.Containers.ContainsKey(info.Name)) {
                ContainerInfos.Add(info);
                container = AGM.Containers[info.Name];
                Containers.Add(container);
                container.IncreasingPriotity = info.IncreasingPriority;
                container.DecreasingPriotity = info.DecreasingPriority;
                container.IsIncreasingLocked = info.IsIncreasingLocked;
                container.IsDecreasingLocked = info.IsDecreasingLocked;
                container.IncreasingWeight = info.IncreasingWeight;
                container.DecreasingWeight = info.DecreasingWeight;
                // Set AGM for dynamically added container.
                container.AGM = AGM;

                // Update all user conditions for the included items.
                List<string> items = container.GetAllItemNames();
                foreach (string item in items) {
                    UpdateConsumerConditions(item);
                }
            } else {
                Debug.LogError("Container not found with the name: " + info.Name +
                    ". Maybe you have errors about unique name issues before this message?");
            }

            UpdatePriorities();

            return container;
        }

        /// <summary>
        /// Removes the container reference.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ContainerBase RemoveContainerRef(string name) {
            if (!AGM.Containers.ContainsKey(name)) {
                Debug.LogError("ActivationGraphSystem: No container available with the name: " + name);
                return null;
            }

            ContainerBase cont = AGM.Containers[name];

            if (!Containers.Contains(cont)) {
                Debug.LogError("ActivationGraphSystem: Container does not exists with the name: " + name);
                return null;
            }

            Containers.Remove(cont);

            // Update all user conditions for the included items.
            List<string> items = cont.GetAllItemNames();
            foreach (string item in items) {
                UpdateConsumerConditions(item);
            }

            UpdatePriorities();

            return cont;
        }

        /// <summary>
        /// Check the condition of the cunsumer user conditions again.
        /// Needed if the item value has been changed in the container,
        /// to actualize the item value dependent consumer user condition
        /// nodes.
        /// </summary>
        public override void UpdateConsumerConditions(string itemName) {
            foreach (ContainerBase cont in Containers) {
                cont.UpdateConsumerConditions(itemName);
            }
        }
    }

    /// <summary>
    /// Sort the list by key, which is in the KeyValuePair.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class MultipleKeyComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>> where TKey : IComparable {
        public int Compare(KeyValuePair<TKey, TValue> key1, KeyValuePair<TKey, TValue> key2) {
            int res = key1.Key.CompareTo(key2.Key);
            if (res == 0)
                return 1;
            else
                return res;
        }
    }

    /// <summary>
    /// Data class for preconfigured container information. Used by the editor.
    /// </summary>
	[System.Serializable]
	public class ContainerInfo {
		[SerializeField]
		public string Name;
        // Priorities for adding and removing values in items.
        public int IncreasingPriority = 0;
		public int DecreasingPriority = 0;
		// Locking the containers.
		public bool IsIncreasingLocked = false;
		public bool IsDecreasingLocked = false;
        // For the containers with the same priorities, the weight can be used for distribution.
        public int IncreasingWeight = 1;
        public int DecreasingWeight = 1;

        public ContainerInfo(string name, int increasingPriotity, int decreasingPriotity, int increasingWeight = 1, int decreasingWeight = 1) {
			Name = name;
            IncreasingPriority = increasingPriotity;
            DecreasingPriority = decreasingPriotity;
            increasingWeight = IncreasingWeight;
            decreasingWeight = DecreasingWeight;
        }
    }

}

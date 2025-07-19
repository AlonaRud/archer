using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represent the container for items, which can be consumed by the UserCondition.
    /// </summary>
    public class Container : ContainerBase {
		
        public string ShortDescription = "Container node.";

		public float GlobalMaxWeight = 10;

        // The items with amount.
        public List<ContainerItem> Items = new List<ContainerItem>();
        // Speed up accessing the items by name.
        private Dictionary<string, ContainerItem> NameItemDict = new Dictionary<string, ContainerItem>();

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
            foreach (ContainerItem item in Items) {
                NameItemDict.Add(item.GetName(), item);
            }
        }

        public virtual void AddItem(string name, int value, float wight, float maxWeightLimit) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (!NameItemDict.ContainsKey(name)) {
                ContainerItem ci = new ContainerItem(name, value, wight, maxWeightLimit);
                Items.Add(ci);
                NameItemDict.Add(name, ci);

                UpdateConsumerConditions(name);
            } else {
                Debug.LogError("ActivationGraphSystem: This item already exists: " + name);
            }
        }

		public virtual void RemoveItem(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(name)) {
                ContainerItem thisItem = NameItemDict[name];
                Items.Remove(thisItem);
                NameItemDict.Remove(name);

                UpdateConsumerConditions(name);
            } else {
                Debug.LogError("ActivationGraphSystem: No such item available: " + name);
            }
        }

		public override int GetItemValue(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(name)) {
                ContainerItem thisItem = NameItemDict[name];
                return thisItem.Value;
            }
            
            return 0;
        }

        public override float GetItemMaxWeight(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(name)) {
                ContainerItem thisItem = NameItemDict[name];
                return thisItem.MaxWeightLimit;
            }
            
            return 0;
        }

        /// <summary>
        /// Always returns one or nothing.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override List<ContainerItem> GetItems(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            List<ContainerItem> items = new List<ContainerItem>();

            if (NameItemDict.ContainsKey(name)) {
                ContainerItem thisItem = NameItemDict[name];
                items.Add(thisItem);
            }

            return items;
        }

        /// <summary>
        /// Returns all items
        /// </summary>
        /// <returns></returns>
        public override List<ContainerItem> GetAllItems() {
            return Items;
        }

        /// <summary>
        /// Needed to get all item types.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override List<string> GetAllItemNames() {
            List<string> items = new List<string>();
            foreach (ContainerItem item in Items) {
                items.Add(item.GetName());
            }
            return items;
        }

        /// <summary>
        /// Needed to get all item names once, but with value sum.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Dictionary<string, int> GetAllItemNamesWithValue() {
            Dictionary<string, int> items = new Dictionary<string, int>();
            foreach (ContainerItem item in Items) {
                items.Add(item.GetName(), item.Value);
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
        public override int ModifyValue(string itemName, int value, int resourceDistributionDivider = 1, 
                bool dryRun = false, float maxIncWeight = 1, float maxDecWeight = 1) {

            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (!NameItemDict.ContainsKey(itemName)) {
                return value;
            }

            // The last one in the same ordered container must consume the all rest.
            int nonConsumableValue = 0;
            int consumableValue = value;
            // If the resourceDistributionDivider is 1, then the last item in the same priority group has been reached,
            // this must consume all the rest, else we could lost values.
            if (resourceDistributionDivider > 1) {
                consumableValue = value;

                // Weight based increasing decreasing for same priorities.
                // E.g. put less resources in silos, which are more far away from the main building.
                float weightFactor = 0;
                if (consumableValue < 0) {
                    if (maxDecWeight != 0)
                        // Weighted distribution if possible.
                        weightFactor = this.DecreasingWeight / maxDecWeight;
                    else
                        // Equal distribution.
                        weightFactor = 1 / resourceDistributionDivider;

                    if (doDebug)
                        Debug.Log("name: " + name + " weightFactor: " + weightFactor + " DecreasingWeight: " + 
                            DecreasingWeight + " maxDecWeight: " + maxDecWeight);
                } else {
                    if (maxIncWeight != 0)
                        // Weighted distribution if possible.
                        weightFactor = this.IncreasingWeight / maxIncWeight;
                    else
                        // Equal distribution.
                        weightFactor = 1 / resourceDistributionDivider;

                    if (doDebug)
                        Debug.Log("name: " + name + " weightFactor: " + weightFactor + " IncreasingWeight: " + 
                            IncreasingWeight + " maxIncWeight: " + maxIncWeight);
                }


                // The consumableValue will be weight based.
                consumableValue = Mathf.RoundToInt(consumableValue * weightFactor);

                // That what shouldn't be consumed by this container must be returned additionally to the rest.
                nonConsumableValue = value - consumableValue;
            }

            if (consumableValue == 0)
                return value;

            int rest = consumableValue;

            if (doDebug)
                Debug.Log("itemName: " + itemName + " rest: " + rest + " containerName: " + name);
            
            if (NameItemDict.ContainsKey(itemName)) {
                ContainerItem thisItem = NameItemDict[itemName];
                if (rest < 0) {

                    // Container can be locked.
                    if (IsDecreasingLocked) {
                        return rest;
                    }

                    // Remove value;
                    rest = Mathf.Abs(rest);
                    int maxDec = Mathf.Min(thisItem.Value, rest);

                    if (doDebug)
                        Debug.Log("maxDec: " + maxDec);

                    if (!dryRun)
                        thisItem.Value -= maxDec;

                    rest = rest - maxDec;
                    rest *= -1;
                } else {

                    // Container can be locked.
                    if (IsIncreasingLocked) {
                        return rest;
                    }

                    // Add more value.
                    // Current item weight.
                    float valueWeight = thisItem.Value * thisItem.Weight;
                    // Max weight limit.
                    float maxItemWeightLimit = thisItem.MaxWeightLimit;
                    // Containers max weight limit.
                    float contMaxWeightLimit = GlobalMaxWeight;

                    // Weight of all items in the container.
                    float allWeight = GetWeight();
                    // Rest weight in this container.
                    float maxFreeWeight1 = Mathf.Max(contMaxWeightLimit - allWeight, 0);
                    // Rest weuight for this item.
                    float maxFreeWeight2 = Mathf.Max(maxItemWeightLimit - valueWeight, 0);
                    // calculate the lowest weight.
                    float maxFreeWeight = Mathf.Min(maxFreeWeight1, maxFreeWeight2);

                    int maxValue = Mathf.FloorToInt(maxFreeWeight / thisItem.Weight);

                    int maxInc = Mathf.Min(maxValue, rest);

                    if (doDebug)
                        Debug.Log("maxInc: " + maxInc);

                    if (!dryRun)
                        thisItem.Value += maxInc;

                    rest = rest - maxInc;
                }
            }
            
            if (rest != consumableValue && !dryRun) {
                UpdateConsumerConditions(itemName);
            }

            return rest += nonConsumableValue;
        }

        /// <summary>
        /// Check the condition of the cunsumer user conditions again.
        /// Needed if the item value has been changed in the container,
        /// to actualize the item value dependent consumer user condition
        /// nodes.
        /// </summary>
        public override void UpdateConsumerConditions(string itemName) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            // In editor mode AGM is null.
            if (AGM == null)
                return;

            foreach (ConditionUser cond in AGM.ConsumerUserConditions) {

                // Inactive type won't be updated.
                if (cond.Result == ConditionBase.ConditionResult.InactiveType)
                    continue;

                if (cond.ContainsItemName(itemName)) {
                    if (doDebug)
                        Debug.Log("Update user condition: " + cond.name + " for: " + itemName);

                    // Notify the task about the item changes.
                    cond.NotityConnectedTasks();
                }

            }
        }

        /// <summary>
        /// Returns teh unique item name for the new item.
        /// </summary>
        /// <param name="baseName"></param>
        /// <returns></returns>
        public string GetUniqeItemName(string baseName = "Item") {
            string newName = baseName;
            int index = 0;
            bool isNameUnique = false;

            while (!isNameUnique && Items.Count > 0) {
                foreach (ContainerItem item in Items) {
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

		public float GetWeight() {
			float weight = 0;
			foreach (ContainerItem item in Items) {
				weight += item.Value * item.Weight;
			}
			return weight;
        }

        /// <summary>
        /// Returns the maximal container global weight limit.
        /// </summary>
        /// <returns></returns>
        public override float GetMaxGlobalWeight() {
            return GlobalMaxWeight;
        }

        /// <summary>
        /// Renames the container item.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="name"></param>
        public void SetItemName(ContainerItem item, string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (NameItemDict.ContainsKey(item.GetName())) {
                // Synchronize the dictionary with the changed name too.
                string oldName = item.GetName();

                NameItemDict.Remove(item.GetName());
                NameItemDict.Add(name, item);
                item.SetName(name);

                UpdateConsumerConditions(name);
                UpdateConsumerConditions(oldName);
            } else {
                Debug.LogError("ActivationGraphManager: Internal error, no item with the name '" + item.GetName() + 
                    "' available. " + "Dictionary is asynchron to the list.");
            }
        }

        /// <summary>
        /// Inserts a container item.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void InsertItem(int index, ContainerItem item) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (!NameItemDict.ContainsKey(item.GetName())) {
                // Synchronize the dictionary with the changed name too.
                NameItemDict.Add(item.GetName(), item);
                Items.Insert(index, item);

                UpdateConsumerConditions(item.GetName());
            } else {
                Debug.LogError("ActivationGraphManager: Internal error, item with the name '" + item.GetName() +
                    "' already exists. " + "Dictionary is asynchron to the list.");
            }
        }

        /// <summary>
        /// Removes a container item at a certain position.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveItemAt(int index) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            if (index >= Items.Count) {
                Debug.LogError("ActivationGraphSystem: index out of range: " + index);
            }

            ContainerItem item = Items[index];
            if (item == null) {
                Debug.LogError("ActivationGraphSystem: item at index '" + index + "' is null'");
            }

            string name = item.GetName();

            if (NameItemDict.ContainsKey(name)) {
                NameItemDict.Remove(name);
                Items.Remove(item);

                UpdateConsumerConditions(name);
            } else {
                Debug.LogError("ActivationGraphSystem: No such item available: " + name);
            }
        }

        /// <summary>
        /// Checks the item for existence by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ContainsItemName(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            return NameItemDict.ContainsKey(name);
        }

        /// <summary>
        /// Returns the container item by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ContainerItem GetItemByName(string name) {
            // Dictionary cannot be serialized, so it must be refreshed in editor mode.
            if (NameItemDict.Count != Items.Count) {
                ActualizeDictionary();
            }

            return NameItemDict[name];
        }

        /// <summary>
        /// Re-distribute the items based on the inserting priority.
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
        /// Re-distribute one items based on the inserting priority.
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

    }

    /// <summary>
    /// Data class for a container items.
    /// </summary>
    [System.Serializable]
    public class ContainerItem {
        // The name cannot be changed by the attribute itself, because the name/item dictionary
        // must be updated in this case too.
        [SerializeField]
        private string Name;
        public int Value;
		public float Weight;
		public float MaxWeightLimit;

        public ContainerItem(string name, int value, float weight, float maxWeightLimit) {
            Name = name;
            Value = value;
            Weight = weight;
            MaxWeightLimit = maxWeightLimit;
        }

        public string GetName() {
            return Name;
        }

        public void SetName(string name) {
            Name = name;
        }
    }

}

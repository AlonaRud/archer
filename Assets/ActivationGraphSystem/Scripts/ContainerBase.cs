using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;


namespace ActivationGraphSystem {
    /// <summary>
    /// Represent the container for items, which can be consumed by the UserCondition.
    /// </summary>
	public class ContainerBase : BaseNode {

        // Select the instantiated script.
        [Header("Select the instantiated script.")]
        public MonoBehaviour ScriptAtValueChanged;
        [Header("Enter the method name that should be called.")]
        public string MethodNameAtValueChanged;
        protected Action<ContainerBase, string, float> valueChangedAction;

        // Priorities for adding and removing values in items.
        public int IncreasingPriotity = 0;
        public int DecreasingPriotity = 0;
        // Locking the containers.
        public bool IsIncreasingLocked = false;
        public bool IsDecreasingLocked = false;
        // For the containers with the same priorities, the weight can be used for distribution.
        public float IncreasingWeight = 1;
        public float DecreasingWeight = 1;


        /// <summary>
        /// Return the sum of the item values from the the conencted container.
        /// </summary>
        /// <returns>The item value.</returns>
        /// <param name="name">Name.</param>
        public virtual int GetItemValue(string name) {
            return 0;
        }

        /// <summary>
        /// Return the sum of the item max weights from the the conencted container.
        /// </summary>
        /// <returns>The item max weight.</returns>
        /// <param name="name">Name.</param>
        public virtual float GetItemMaxWeight(string name) {
            return 0;
        }

        /// <summary>
        /// Return the items by name.
        /// </summary>
        /// <returns></returns>
        public virtual List<ContainerItem> GetItems(string name) {
            return null;
        }

        /// <summary>
        /// Returns all items
        /// </summary>
        /// <returns></returns>
        public virtual List<ContainerItem> GetAllItems() {
            return null;
        }

        /// <summary>
        /// Needed to get all item types.
        /// </summary>
        /// <returns></returns>
        public virtual List<string> GetAllItemNames() {
            return null;
        }

        /// <summary>
        /// Needed to get all item names once, but with value sum.
        /// </summary>
        /// <returns></returns>
        public virtual Dictionary<string, int> GetAllItemNamesWithValue() {
            Dictionary<string, int> items = new Dictionary<string, int>();
            return items;
        }

        /// <summary>
        /// Add a value to the item.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="resourceDistributionDivider"></param>
        /// <param name="dryRun"></param>
        /// <returns></returns>
        public int AddValue(string name, int value, int resourceDistributionDivider = 1, bool dryRun = false) {
            int oldValue = GetItemValue(name);
            value = oldValue + value;
            return ModifyValue(name, value, resourceDistributionDivider, dryRun);
        }

        public virtual int ModifyValue(string name, int value, int resourceDistributionDivider = 1, bool dryRun = false, float maxIncWeight = 1, float maxDecWeight = 1) {
            return 0;
        }

        public virtual float GetMaxGlobalWeight() {
            return 0;
        }

        /// <summary>
        /// Creates the actions.
        /// </summary>
        protected override void Awake() {
            base.Awake();
        }

        /// <summary>
        /// Resets the node to the initial state.
        /// </summary>
        public override void Reset() {
            // Do nothing.
        }

        /// <summary>
        /// Re-distribute the items based on the inserting priority.
        /// </summary>
        public virtual void ReDistributeItemVaues(Dictionary<string, int> items) {
        }

        /// <summary>
        /// Re-distribute all items based on the inserting priority.
        /// </summary>
        public virtual void ReDistributeAllItemVaues() {
        }

        /// <summary>
        /// Re-distribute one items based on the inserting priority.
        /// </summary>
        public virtual void ReDistributeOneItemVaues(string name) {
        }

        /// <summary>
        /// Check the condition of the cunsumer user conditions again.
        /// Needed if the item value has been changed in the container,
        /// to actualize the item value dependent consumer user condition
        /// nodes.
        /// </summary>
        public virtual void UpdateConsumerConditions(string itemName) {
        }
    }
}

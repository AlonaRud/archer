using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    public class BuildTreeDataProvider : MonoBehaviour {

        // The BuildTreeDataProvider is a singleton.
        public static BuildTreeDataProvider Instance;

        public List<BuildTreeDataEntry> BuildTreeDataEntries = new List<BuildTreeDataEntry>();
        public Dictionary<string, BuildTreeDataEntry> BuildTreeDataEntryDict = new Dictionary<string, BuildTreeDataEntry>();

        /// <summary>
        /// Initialize the singleton class.
        /// </summary>
        protected void Awake() {
            Instance = this;
        }
    }

    /// <summary>
    /// One entry on the build queue.
    /// There you can insert the building/unit releted data class,
    /// which contains detailed information, like HP, the prefab
    /// that should be created, is it a unit or a building and so on.
    /// </summary>
    [System.Serializable]
    public class BuildTreeDataEntry {

        [Header("The name of the thing tat can be created.")]
        public string Name = "";
        [Header("Image/icon in the dialog for buildings and units.")]
        public Sprite Image;
        [Header("The build time, which counts after clicking the icon.")]
        public float BuildTime = 1;
        [Header("This prefab will be instantiated after the build time elapses.")]
        public GameObject Prefab;
        // Hint: add more configuraton attributes, if needed. These attributes can be
        // used for for creating the item and after instantiating the prefab for 
        // configuring the prefab attributes.

        public BuildTreeDataEntry(string name, Sprite image, float buildTime, GameObject prefab) {
            Name = name;
            Image = image;
            BuildTime = buildTime;
            Prefab = prefab;
        }
    }

}

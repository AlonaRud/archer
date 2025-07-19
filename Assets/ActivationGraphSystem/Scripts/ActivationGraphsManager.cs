using UnityEngine;
using System.Collections.Generic;


namespace ActivationGraphSystem {

    /// <summary>
    /// This class collects the graph manager in the scene, which then can be get by name.
    /// Simply add it to a gameObject if you need this.
    /// </summary>
    public class ActivationGraphsManager : MonoBehaviour {

        public static ActivationGraphsManager Instance;
        public Dictionary<string, ActivationGraphManager> Managers = new Dictionary<string, ActivationGraphManager>(); 

        void Awake() {
            Instance = this;
        }
    }
}

using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// The example scene triggers this methods at task activations.
    /// </summary>
    public class TriggerTest : MonoBehaviour {

        public AudioSource Sound;


        public void PrintNodeName(BaseNode node) {
            if (Sound) {
                Sound.Play();
            }
            Debug.Log("Node '" + node.name + "' in success state.");
        }
    }
}

using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Plays a sound at activating the gameobject. 
    /// </summary>
    public class RunSoundOnEnable : MonoBehaviour {

        public AudioSource Sound;


        void OnEnable() {
            if (Sound) {
                Sound.Play();
            }
        }
    }
}

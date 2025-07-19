using UnityEngine;


namespace ActivationGraphSystem {
    /// <summary>
    /// Moves the player sphere in z direction, just for the example scene.
    /// </summary>
    public class CubeMover : MonoBehaviour {

        public float SlowDownFactor = 1000;
        public float StopAtZValue = -150;

        void Update() {
            if (transform.position.z <= StopAtZValue)
                return;
            transform.localPosition += -transform.forward / SlowDownFactor * Time.deltaTime;
        }
    }
}

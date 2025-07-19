using UnityEngine;
using System.Collections;

namespace ActivationGraphSystem {
    public class RotateObject : MonoBehaviour {

        [Header("This object will be rotated.")]
        public GameObject TheObject = null;
        public float Speed = 1;
        public Vector3 RotatingAxis;

        void Start () {
            if (TheObject == null)
                TheObject = gameObject;
        }
	
	    void Update () {
            TheObject.transform.Rotate(RotatingAxis, Time.deltaTime * Speed);
        }
    }
}

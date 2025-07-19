using UnityEngine;
using System.Collections;


namespace ActivationGraphSystem {
    public class TimerManagerTest : MonoBehaviour {

        // Use this for initialization
        void Start() {
            Debug.Log("START1");
            TimerManager.Instance.Add(this, 10, TriggerMe1, 3, TickMe1);
            TimerManager.Instance.Add(this, 5, TriggerMe2);
        }

        void TickMe1(Timer timer) {
            if (timer.TickCounter == 0) {
                Debug.Log("      START1");
            } else {
                Debug.Log("      TICK1: " + timer.TickCounter);
            }
        }

        void TriggerMe1(Timer timer) {
            Debug.Log("FINISH1");
        }

        void TriggerMe2(Timer timer) {
            Debug.Log("TriggerMe2");
            TimerManager.Instance.Add(this, 3, TriggerMe3);
        }

        void TriggerMe3(Timer timer) {
            Debug.Log("TriggerMe3");
            TimerManager.Instance.Remove(timer);
        }
    }
}

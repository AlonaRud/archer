using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ActivationGraphSystem {
    public class TimerManager : MonoBehaviour {

        // Singleton static entry function for the TimerManager.
        public static TimerManager Instance;

        // Contains the instantiated timers.
        public List<Timer> timers = new List<Timer>();
        Coroutine mainCoroitine = null;


        /// <summary>
        /// Instantiate the singleton TimerManager.
        /// </summary>
        void Awake() {
            Instance = this;
        }

        /// <summary>
        /// Disables the TimerManager, stops the main coroutine.
        /// </summary>
	    void OnDisable() {
	    	doStop = true;
            if (mainCoroitine != null)
                StopCoroutine(mainCoroitine);
        }

        /// <summary>
        /// Enables the TimerManager, starts the main coroutine.
        /// </summary>
        void OnEnable() {
            mainCoroitine = StartCoroutine(UpdateTimers());
        }
        
	    void OnDestroy() {
	    	doStop = true;
		    if (mainCoroitine != null)
			    StopCoroutine(mainCoroitine);
	    }

	    bool doStop = false;

        /// <summary>
        /// Timers are checked in a thread, to do not use the main thread.
        /// </summary>
        /// <returns></returns>
        IEnumerator UpdateTimers() {
            while (true) {
	            yield return new WaitForEndOfFrame();
	            if (doStop)
		            yield return null;
                // Every tick can trigger a callback method.
                UpdateTimer();
            }
        }

        /// <summary>
        /// Add a timer.
        /// </summary>
        /// <param name="go">The gameobject, where the timer call the method.</param>
        /// <param name="delay">Lifetime of the timer.</param>
        /// <param name="callback">The callback, which will be called at timer end.</param>
        /// <returns></returns>
        public Timer Add(System.Object go, float delay, Action<Timer> callback) {
            return Add(go, delay, callback, 0, null);
        }

        /// <summary>
        /// Add a new timer with defining a tick.
        /// </summary>
        /// <param name="go">The gameobject, where the timer call the method.</param>
        /// <param name="delay">Lifetime of the timer.</param>
        /// <param name="callbackDelay">The callback, which will be called at timer end.</param>
        /// <param name="ticks">Tick period.</param>
        /// <param name="callbackTicks">The callback, which will be called at each tick.</param>
        /// <returns></returns>
        public Timer Add(System.Object go, float delay, Action<Timer> callbackDelay, int ticks, Action<Timer> callbackTicks) {
            return Add(go, delay, callbackDelay, go, ticks, callbackTicks);
        }

        /// <summary>
        /// Add a new timer with defining a tick.
        /// </summary>
        /// <param name="go">The gameobject, where the timer call the method.</param>
        /// <param name="delay">Lifetime of the timer.</param>
        /// <param name="callbackDelay">The callback, which will be called at timer end.</param>
        /// <param name="ticks">Tick period.</param>
        /// <param name="callbackTicks">The callback, which will be called at each tick.</param>
        /// <returns></returns>
        public Timer Add(System.Object go, float delay, Action<Timer> callbackDelay, System.Object goTicks, 
                int ticks, Action<Timer> callbackTicks) {
            Timer timer = new Timer(go, delay, callbackDelay, goTicks, ticks, callbackTicks);

            // Removing could collide with the main coroutine.
            if (mainCoroitine != null)
                StopCoroutine(mainCoroitine);

            timers.Add(timer);

            // Restart the coroutine.
            mainCoroitine = StartCoroutine(UpdateTimers());

            return timer;
        }

        /// <summary>
        /// Add a new timer.
        /// </summary>
        /// <param name="timer"></param>
        /// <returns></returns>
        public Timer Add(Timer timer) {
            timer.IsFinished = false;

            // Removing could collide with the main coroutine.
            if (mainCoroitine != null)
                StopCoroutine(mainCoroitine);

            if (!timers.Contains(timer))
                timers.Add(timer);

            // Restart the coroutine.
            mainCoroitine = StartCoroutine(UpdateTimers());

            return timer;
        }

        /// <summary>
        /// Remove the timer directly.
        /// </summary>
        /// <param name="timer"></param>
	    public void Remove(Timer timer) {
		    if (doStop)
		    	return;
		    	
            // Removing could collide with the main coroutine.
            if (mainCoroitine != null)
                StopCoroutine(mainCoroitine);

            // Remove the timer. Safe now, because the coroutine is stopped.
            if (timers.Contains(timer))
                timers.Remove(timer);

            // Restart the coroutine.
            mainCoroitine = StartCoroutine(UpdateTimers());
        }

        /// <summary>
        /// Timer can be checked for relationship.
        /// </summary>
        /// <param name="timer"></param>
        /// <returns></returns>
        public bool Contains(Timer timer) {
            if (timers.Contains(timer))
                return true;
            return false;
        }

        /// <summary>
        /// Update all timer, which are attached to the TimerManager.
        /// </summary>
        void UpdateTimer() {
            if (timers == null)
                Debug.LogError("Timers null");

            for (int i = timers.Count - 1; i >= 0; i--) {
                if (timers[i].Paused)
                    continue;
                // Clean up the timer which has been removed indirectly.
                if (timers[i].CallerClass == null) {
                    timers.RemoveAt(i);
                    continue;
                }
                // Update the real timer instance.
                timers[i].UpdateTimer();
            }
        }
    }

    /// <summary>
    /// The timer class for the TimerManager.
    /// </summary>
    [System.Serializable]
    public class Timer {
        public float Delay;
        public int Ticks;
        public float TicksTime;
        public int TickCounter;
        public bool IsFinished = false;
        public Action<Timer> CallBackAtFinish;
        public Action<Timer> CallBackAtTick;
        public System.Object CallerClass;
        public System.Object CallerClassTicks;
        public bool Paused = false;
        public float consumedDelay = Time.time;
        float consumedTick = Time.time;

        const bool doDebug = false;


        /// <summary>
        /// The constructor for the timer.
        /// </summary>
        /// <param name="obj">The instance itself, on which the timer activate a method.</param>
        /// <param name="delay">Time until the timer finish counting.</param>
        /// <param name="callbackDelay">Callback for finish.</param>
        /// <param name="ticks">One tick period.</param>
        /// <param name="callbackTicks">Callback for one tick.</param>
        public Timer(System.Object obj, float delay, Action<Timer> callbackDelay, System.Object goTicks, 
                int ticks, Action<Timer> callbackTicks) {
            if (delay < 0) {
                Debug.LogError("TimerManager, delay must be greater or equal to 0.");
                return;
            } else if (ticks < 0) {
                Debug.LogError("TimerManager, ticks must be greater or equal to 0.");
                return;
            } else if (obj == null) {
                Debug.LogError("TimerManager, obj must not be null.");
                return;
            } else if (callbackDelay == null) {
                Debug.LogError("TimerManager, callbackDelay must not be null.");
                return;
            } else if (ticks > 0 && callbackTicks == null) {
                Debug.LogError("TimerManager, callbackTicks must not be null.");
                return;
            }
            
            Delay = delay;
            consumedDelay = delay;
            TicksTime = delay / ticks;
            consumedTick = TicksTime;

            CallerClass = obj;
            CallerClassTicks = goTicks;
            CallBackAtFinish = callbackDelay;
            Ticks = ticks;
            CallBackAtTick = callbackTicks;
            TickCounter = 0;
        }

        /// <summary>
        /// Calls the callbacks, if conditions met, and removes the timer at finishing.
        /// </summary>
        public void UpdateTimer() {
            consumedDelay -= Time.deltaTime;
            consumedTick -= Time.deltaTime;
            
            // If the tick callback is null, then ignore this, just process the finish callback then.
            // This happens if the gameobject had been destroyed.
            if (CallerClassTicks != null && consumedTick <= 0) {
                // Call the tick callback.
                CallBackAtTick.Invoke(this);
                TickCounter = Mathf.FloorToInt((Delay - consumedDelay) /TicksTime);
                consumedTick = TicksTime;
                if (doDebug)
                    Debug.Log("consumedTick: " + consumedTick + " consumedDelay: " + consumedDelay + " next-tick-time: " + 
                        TickCounter * TicksTime + " TickCounter: " + TickCounter + " TicksTime: " + TicksTime);
            }

            // End reached.
            if (consumedDelay <= 0) {
                if (doDebug)
                    Debug.Log("ConsumedDelay: " + consumedDelay);
                // Mark the timer itself. (IsFinished = true) means, finished and detached from manager.
                IsFinished = true;
                // Remove the timer.
                TimerManager.Instance.Remove(this);
	            // Call the finish callback, if the timer finished.
	            if (CallerClass != null) {
		            CallBackAtFinish.Invoke(this);
	            }
            }
        }

    }
}

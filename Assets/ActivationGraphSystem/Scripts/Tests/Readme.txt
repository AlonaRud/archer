Test script for the Timer manager.

How to use it?

Drop the script on a gameObject. The script starts autimatically after playing the scene.

What does the script check?

Starting and removing timer. At this operations the TimerManager must stop its main coroutine to
avoid undefined behavoir at checking the timer. Also checks the timer behavior for both Add (with tick, without tick)
methods.

What should be the output?

START1
   START1
   TICK1
TriggerMe2
   TICK2
TriggerMe3
   TICK3
FINISH1
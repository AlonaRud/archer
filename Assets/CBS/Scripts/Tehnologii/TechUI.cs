#if false
using CBS.Models;
using CBS.Scriptable;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBS.UI
{
    public class TechUI : MonoBehaviour
    {
        private TechSlot[] Slots { get; set; }

        private void Awake()
        {
            Slots = GetComponentsInChildren<TechSlot>();
        }

        public void Initialize(Dictionary<string, CBSTask> tasks, string category)
        {
            foreach (var slot in Slots)
            {
                if (tasks.ContainsKey(slot.TaskID) && tasks[slot.TaskID].Tag == category)
                {
                    slot.Init(tasks[slot.TaskID], task =>
                    {
                        var prefabs = CBSScriptable.Get<TasksCategoryPrefabs>();
                        var uiObject = UIView.ShowWindow(prefabs.TechInfo);
                        if (uiObject != null)
                        {
                            var infoComponent = uiObject.GetComponent<TechInfo>();
                            if (infoComponent != null)
                            {
                                infoComponent.Display(task);
                            }
                        }
                    });
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }
    }
}
#endif
   
   

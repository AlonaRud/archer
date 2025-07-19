using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// This class is pretty long compared to the another classes, but it combines the UI logic with the 
    /// production logic (build/build finished/refund...).
    /// 
    /// An instance of this class is attached on the UI entries (on the buildable things), which 
    /// can be clicked. The method ClickedOn(...) will be called, the UI entry is clicked.
    /// </summary>
    public class BuildTreeItem : MonoBehaviour {

        // The first 2 charactes of the task will be shown on the button.
        // This should be an image/icon for your product.
        // No title, for this example icons are used. 
        public Text Title;

        // The clockwise animated building animation.
        public Image IconImage;

        // So much products has been built.
        public Text Count;

        // Shows how much products will be built, they are in the queue.
        public Text InQueue;

        // The green, orange and red images, which marks the state of the user condition.
        public Image SuccSprite;
        public Image FailSprite;
        public Image RunsSprite;

        // The clockwise animated building animation.
        public Image BuildAnimImage;

        // The button, which builds the related product.
        public Button TheButton;

        [Header("Currently running image.")]
        public Image SelectSprite;
        public float SelectionAlpfa = 0.3f;
        [Header("Hidden dark image.")]
        public Image HideSprite;
        public Text HideSpriteText;

        // The task for the product, this is connected to the button.
        public TaskDataNode Task;

        // The tooltip text will show up here.
        public TypeWriter Desc;

        // In this are the building/unit relevan data, like image. 
        // (Attributes like HP, the instantiated prefab belongs in this too.)
        public BuildTreeDataEntry DataEntry;

        // The item can be built multiple times, this container contains the build entries.
        public List<BuildQueueEntry> entries = new List<BuildQueueEntry>();
        // Building can be paused.
        bool pauseBuilding = false;

        // It is true, if something will be built.
        bool buildingInProgress = false;
        // The timer must be aglobal attribute, refund must remove the timer.
        // If the product has been built, the timer will be detached from the 
        // Timer manager automatically.
        Timer buildTimer = null;

        public AudioSource ClickAudio;


        /// <summary>
        /// Just used for initialization.
        /// </summary>
        private void Awake() {
            SetAlpha(HideSprite, 1);
            SetAlpha(HideSpriteText, 1);
            SetAlpha(SelectSprite, 0);

            InQueue.text = "0";
        }

        /// <summary>
        /// Actualize the content of this entry.
        /// If the BuildTreeManager recognizes a change, which could be
        /// important for the visialization, then it notifies the 
        /// BuildTreeGuiController instance, which then calls this
        /// method for each item entries.
        /// </summary>
        public IEnumerator Actualize() {
            
            // This is needed to avoid flickering buttons.
            // The consumer condition switches for a short time (0.1f) in
            // running mode, until it checks its resource conditions. We don't want to 
            // change the apparance for that small time window.
            yield return new WaitForSeconds(0.2f);

            BuildTreeGuiController cgc = BuildTreeGuiController.Instance;

            // Set the visual apparence for different states.
            if (Task.Result == TaskBase.TaskResult.InactiveType) {
                SetAlpha(HideSprite, 1);
                SetAlpha(HideSpriteText, 1);
                SetAlpha(SelectSprite, 0);
            } else {

                switch (cgc.TaskResConditionDict[Task].Result) {
                    case ConditionBase.ConditionResult.InactiveType:
                        // Hide entry. Shouldn't be called.
                        SetAlpha(HideSprite, 1);
                        SetAlpha(HideSpriteText, 1);
                        SetAlpha(SelectSprite, 0);
                        break;
                    case ConditionBase.ConditionResult.RunningType:
                        SetAlpha(HideSprite, 0);
                        SetAlpha(HideSpriteText, 0);
                        SetAlpha(SelectSprite, 0);

                        SetAlpha(SuccSprite, 0);
                        SetAlpha(FailSprite, 0);
                        SetAlpha(RunsSprite, 1);
                        break;
                    case ConditionBase.ConditionResult.SuccessType:
                        SetAlpha(HideSprite, 0);
                        SetAlpha(HideSpriteText, 0);
                        SetAlpha(SelectSprite, 0);

                        SetAlpha(SuccSprite, 1);
                        SetAlpha(FailSprite, 0);
                        SetAlpha(RunsSprite, 0);
                        break;
                    case ConditionBase.ConditionResult.FailureType:
                        SetAlpha(HideSprite, 0);
                        SetAlpha(HideSpriteText, 0);
                        SetAlpha(SelectSprite, 0);

                        SetAlpha(SuccSprite, 0);
                        SetAlpha(FailSprite, 1);
                        SetAlpha(RunsSprite, 0);
                        break;
                    default:
                        break;
                }
            }

            // Show how many items exists in the containers.
            ConditionUser consumerCondition = cgc.TaskResConditionDict[Task];
            // Find the only one item, which will be increased (created).
            // The item which will be produced.
            if (consumerCondition != null && consumerCondition.Items.Count > 0) {
                ItemRef itemRef = consumerCondition.Items.Find(elem => elem.Value > 0);
                float value = 0;
                if (itemRef != null) {
                    value = cgc.CM.GetItemValue(itemRef.GetName());
                }
                Count.text = "" + value;
            } else {
                Count.text = "N/A";
            }

            // Disable buttons, if cannot build anymore the according item.
            if (consumerCondition != null) {
                switch (consumerCondition.Result) {
                    case ConditionBase.ConditionResult.InactiveType:
                        TheButton.interactable = false;
                        break;
                    case ConditionBase.ConditionResult.RunningType:
                        TheButton.interactable = true;
                        break;
                    case ConditionBase.ConditionResult.SuccessType:
                        TheButton.interactable = true;
                        break;
                    case ConditionBase.ConditionResult.FailureType:
                        TheButton.interactable = false;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Set the alpha value for an image.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="alpha"></param>
        private void SetAlpha(Image img, float alpha) {
            Color col = img.color;
            col.a = alpha;
            img.color = col;
        }

        /// <summary>
        /// Set the alpha value for text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="alpha"></param>
        private void SetAlpha(Text text, float alpha) {
            Color col = text.color;
            col.a = alpha;
            text.color = col;
        }

        /// <summary>
        /// This method shows the item description in the dialog.
        /// On the game object must also be a button script attached,
        /// which calls this method. Left click is used for build or continue build, if the 
        /// production has been paused. Right click pauses the production and a second right click then 
        /// abort the production and refund the materials used for the product.
        /// </summary>
        public void ClickedOn(BaseEventData baseData) {

            if (!(baseData is PointerEventData))
                return;
            PointerEventData data = (PointerEventData)baseData;

            ClickAudio.Play();

            // Do not accept clicks in certain circumstances.
            BuildTreeGuiController cgc = BuildTreeGuiController.Instance;
            ConditionUser consumerCondition = cgc.TaskResConditionDict[Task];
            if (consumerCondition != null) {
                switch (consumerCondition.Result) {
                    case ConditionBase.ConditionResult.InactiveType:
                        return;
                    case ConditionBase.ConditionResult.RunningType:
                        break;
                    case ConditionBase.ConditionResult.SuccessType:
                        break;
                    case ConditionBase.ConditionResult.FailureType:
                        return;
                    default:
                        break;
                }
            }

            // Left/Right click logic.
            if (data.button == PointerEventData.InputButton.Left) {
                if (pauseBuilding) {
                    pauseBuilding = false;
                    if (buildTimer != null)
                        buildTimer.Paused = false;
                    return;
                }
            } else if (data.button == PointerEventData.InputButton.Right) {
				if (pauseBuilding) {
                    refund();
                    return;
                } else if (entries.Count != 0) {
                    pauseBuilding = true;
                    if (buildTimer != null)
                        buildTimer.Paused = true;
                    return;
                } else {
                    // Ignore right click, no entries.
                    return;
                }
            } else {
                return;
            }

            // Create the build entry and add it to the entries queue and to the 
            // main queue in the BuildingQueue of the BuildTreeGuiController.
            // The BuildingQueue and the queue must be synchron.
            switch (consumerCondition.Type) {
                case BaseNode.Types.BuildNode:
                    BuildQueueEntry entryB = new BuildQueueEntry(this, consumerCondition);
                    cgc.buildingQueue.Add(entryB);
                    entries.Add(entryB);
                    break;
                case BaseNode.Types.UnitNode:
                    BuildQueueEntry entryU = new BuildQueueEntry(this, consumerCondition);
                    cgc.unitQueue.Add(entryU);
                    entries.Add(entryU);
                    break;
                default:
                    break;
            }

            // Queue changed, refresh the value in th eUI.
            InQueue.text = "" + entries.Count;
        }

        void refund() {
            // For safety, no entries no refund.
            if (entries.Count == 0)
                return;

            BuildTreeGuiController cgc = BuildTreeGuiController.Instance;
            ConditionUser consumerCondition = cgc.TaskResConditionDict[Task];
            // The refund must be applied on the referenced container. 
            ContainerBase cont = cgc.AGM.Containers[consumerCondition.ContainerName];

            // Restart the task, to be ready to build the product again.
            Task.StartTaskExternal();

            // This entry must be refunded.
            BuildQueueEntry entry = entries[entries.Count - 1];

            // Remove the entry.
            switch (consumerCondition.Type) {
                case BaseNode.Types.BuildNode:
                    cgc.buildingQueue.Remove(entry);
                    break;
                case BaseNode.Types.UnitNode:
                    cgc.unitQueue.Remove(entry);
                    break;
                default:
                    break;
            }

            if (buildingInProgress) {
                foreach (ItemRef item in entry.Consumer.Items) {
                    if (!item.DoConsumeManually) {
                        // Item values, which were automatically removed by the graph, will be refunded.
                        cont.ModifyValue(item.GetName(), -item.Value);
                    } else if (item.DoConsumeManually && entry != null && buildingInProgress) {
                        // Would remove the built item too, if it would exists, but e.g. the building wouldn't
                        // be placed yet. Depends on your implementation, you can allow this or not.
                        cont.ModifyValue(item.GetName(), -item.Value);
						TimerManager.Instance.Remove(buildTimer);
                    }
                }
            }

            // Bring the UI in the correct state.
            entries.Remove(entry);
            InQueue.text = "" + entries.Count;

            // Remove the pause mode, if no more entries in the queue.
            // Else, at building after we would click twice, first for unpause,
            // second for building.
            if (entries.Count == 0)
                pauseBuilding = false;

            buildingInProgress = false;
            buildTimer = null;

            // Update the clockwise animating build image.
            BuildAnimImage.fillAmount = 0;
        }

        /// <summary>
        /// This method shows the item description in the dialog.
        /// On the game object must also be a button script attached,
        /// which calls this method.
        /// </summary>
        public bool Build(BuildQueueEntry entry) {
            if (buildingInProgress || pauseBuilding)
                return false;

            // Mark that we building.
            buildingInProgress = true;

            BuildTreeGuiController cgc = BuildTreeGuiController.Instance;

            // Consume needed resources. Do not let the graph restart the task, do not make a connection to the task itself.
            if (HideSprite.color.a == 0) {
                cgc.TaskTriggerConditionDict[Task].SetSuccessful();
            }

            // Wait the build time and animate. This means, in 1 second the timer ticks 30 times.
            // At least, it tries to tick 30 times. Each tick calls the animateBuildingAtTimerTick method.
            buildTimer = TimerManager.Instance.Add(this, DataEntry.BuildTime, ItemBuilt, 
                Mathf.RoundToInt(30 * DataEntry.BuildTime), animateBuildingAtTimerTick);

            return true;
        }

        /// <summary>
        /// Called after the build time elapses.
        /// </summary>
        public void ItemBuilt(Timer timer) {
            // Add the built product.
            BuildTreeGuiController cgc = BuildTreeGuiController.Instance;
            ConditionUser consumerCondition = cgc.TaskResConditionDict[Task];
            consumerCondition.ConsumeExternallyMarkedItems();
            // Activate the outgoings now. This stopps the current task, so process the 
            // self activation after this call.
            Task.ActivateOutgoingsExternally(true);
            // Restart the task. (self activation)
            Task.StartTaskExternal();

            // The products will be built in order to maintain the global order in the BuildTreeController.
            // We always want to remove the last added entry at first.
            BuildQueueEntry entry = entries[entries.Count - 1];

            // Instantiate the attached prefab for fun.
            Quaternion quat = Quaternion.Euler(UnityEngine.Random.Range(0, 360),
                UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360));

            switch (consumerCondition.Type) {
                case BaseNode.Types.BuildNode: {
                    Vector3 pos = cgc.BuildingSpawn.position;
                    pos = new Vector3(pos.x + UnityEngine.Random.Range(-1f, 1f), pos.y, pos.z + UnityEngine.Random.Range(-1f, 1f));
                    Instantiate(DataEntry.Prefab, pos, quat);
                    }
                    break;
                case BaseNode.Types.UnitNode: {
                    Vector3 pos = cgc.UnitSpawn.position;
                    pos = new Vector3(pos.x + UnityEngine.Random.Range(-1f, 1f), pos.y, pos.z + UnityEngine.Random.Range(-1f, 1f));
                    Instantiate(DataEntry.Prefab, pos, quat);
                    }
                    break;
                default:
                    break;
            }

            // For the visual representation, paint the 100% of clockwise animated image.
            BuildAnimImage.fillAmount = 1;
            // Remove the built product from the queue.
            switch (consumerCondition.Type) {
                case BaseNode.Types.BuildNode:
                    cgc.buildingQueue.Remove(entry);
                    entries.Remove(entry);
                    break;
                case BaseNode.Types.UnitNode:
                    cgc.unitQueue.Remove(entry);
                    entries.Remove(entry);
                    break;
                default:
                    break;
            }
            // Queue changed, update the UI text.
            InQueue.text = "" + entries.Count;

            // Set flags in correct states.
            buildingInProgress = false;
            buildTimer = null;

            // Visually set back the build animation.
            TimerManager.Instance.Add(this, 0.1f, setBackAnimation);
        }

        /// <summary>
        /// // Visually set back the build animation.
        /// </summary>
        /// <param name="timer"></param>
        private void setBackAnimation(Timer timer) {
            BuildAnimImage.fillAmount = 0;
        }

        /// <summary>
        /// Animate at each tick the build progress.
        /// </summary>
        /// <param name="timer"></param>
        private void animateBuildingAtTimerTick(Timer timer) {
            BuildAnimImage.fillAmount = (float)timer.TickCounter/(float)timer.Ticks;
        }

        /// <summary>
        /// Begin mouse hover mode. This paints the tooltip in the description text.
        /// </summary>
        public void PointerEnter(BaseEventData data) {

            BuildTreeGuiController cgc = BuildTreeGuiController.Instance;
            ConditionUser consumerCondition = cgc.TaskResConditionDict[Task];

            if (HideSprite.color.a == 0) {
                Desc.Clear();

                string text = Task.TaskDesc + "\n\n";
                foreach (ItemRef item in consumerCondition.Items) {
                    if (item.DoConsume && item.Value < 0) {
                        text += item.GetName() + ": " + -item.Value + "\n";
                    }
                }

                Desc.SetText(text);

            } else {
                Desc.Clear();
                Desc.SetText("Unknown.");
            }
        }

        /// <summary>
        /// End mouse hover mode. Cleans up the description text.
        /// </summary>
        public void PointerExit(BaseEventData data) {
            Desc.Clear();
        }
    }
}

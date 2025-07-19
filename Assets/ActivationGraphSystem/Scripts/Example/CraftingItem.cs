using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace ActivationGraphSystem {
    /// <summary>
    /// This class is attached on one mission item in the mission dialog.
    /// It controls the title and the images for state visualization.
    /// </summary>
    public class CraftingItem : MonoBehaviour {

        public Text Title;

        public Text Count;

        public Image SuccSprite;
        public Image FailSprite;
        public Image RunsSprite;

        public Button TheButton;

        [Header("Currently running image.")]
        public Image SelectSprite;
        public float SelectionAlpfa = 0.3f;
        [Header("Hidden dark image.")]
        public Image HideSprite;
        public Text HideSpriteText;

        public TaskDataNode Task;

        public TypeWriter Desc;

        private void Awake() {
            SetAlpha(HideSprite, 1);
            SetAlpha(HideSpriteText, 1);
            SetAlpha(SelectSprite, 0);
        }

        /// <summary>
        /// Actualize the content of this entry.
        /// If the CraftingManager recognizes a change, which could be
        /// important for the visialization, then it notifies the 
        /// CraftingGuiController instance, which then calls this
        /// method for each item entries.
        /// </summary>
        public IEnumerator Actualize() {

            // This is needed to avoid flickering buttons.
            // The consumer condition switches for a short time (0.1f) in
            // running mode, until it checks its resource conditions. We don't want to 
            // change the apparance for that small time window.
            yield return new WaitForSeconds(0.2f);

            CraftingGuiController cgc = CraftingGuiController.Instance;

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
                        SetAlpha(SelectSprite, SelectionAlpfa);

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
            ItemRef itemRef = consumerCondition.Items.Find(elem => elem.Value >= 0);
            float value = cgc.CM.GetItemValue(itemRef.GetName());
            Count.text = "" + value;
            Title.text = Task.TaskName;
            
            // Disable buttons, if cannot build anymore the according item.
            switch (consumerCondition.Result) {
                case ConditionBase.ConditionResult.InactiveType:
                    TheButton.interactable = false;
                    break;
                case ConditionBase.ConditionResult.RunningType:
                    TheButton.interactable = false;
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

        private void SetAlpha(Image img, float alpha) {
            Color col = img.color;
            col.a = alpha;
            img.color = col;
        }

        private void SetAlpha(Text text, float alpha) {
            Color col = text.color;
            col.a = alpha;
            text.color = col;
        }

        /// <summary>
        /// This method shows the item description in the dialog.
        /// On the game object must also be a button script attached,
        /// which calls this method.
        /// </summary>
        public void ClickedOn() {

            CraftingGuiController cgc = CraftingGuiController.Instance;

            if (HideSprite.color.a == 0) {
                cgc.TaskTriggerConditionDict[Task].SetSuccessful();
            }
        }

        /// <summary>
        /// Begin mouse hover mode.
        /// </summary>
        public void PointerEnter() {

            CraftingGuiController cgc = CraftingGuiController.Instance;
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
        /// End mouse hover mode.
        /// </summary>
        public void PointerExit() {
            Desc.Clear();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// This class is attached on one mission item in the mission dialog.
    /// It controls the title and the images for state visualization.
    /// </summary>
    public class MissionItem : MonoBehaviour {

        public Text Title;

        public Image SuccSprite;
        public Image FailSprite;
        public Image RunsSprite;

        [Header("Currently running quest image.")]
        public Image SelectSprite;
        public float SelectionAlpfa = 0.3f;
        [Header("Hidden quests dark image.")]
        public Image HideSprite;
        public Text HideSpriteText;

        public TaskDataNode MissTask;

        public TypeWriter MissionDesc;

        private void Awake() {
            SetAlpha(HideSprite, 1);
            SetAlpha(HideSpriteText, 1);
            SetAlpha(SelectSprite, 0);
        }

        /// <summary>
        /// Actualize the content of this entry.
        /// If the MissionManager recognizes a change, which could be
        /// important for the visialization, then it notifies the 
        /// MissionGuiController instance, which then calls this
        /// method for each mission entries.
        /// </summary>
        public void Actualize() {

            // If 2 tasks are running, just write for the first one.
            bool alreadyWrote = false;

            switch (MissTask.Result) {
                case Task.TaskResult.InactiveType:
                    // Hide entry. Shouldn't be called.
                    SetAlpha(HideSprite, 1);
                    SetAlpha(HideSpriteText, 1);
                    SetAlpha(SelectSprite, 0);
                    break;
                case Task.TaskResult.RunningType:
                    SetAlpha(HideSprite, 0);
                    SetAlpha(HideSpriteText, 0);
                    SetAlpha(SelectSprite, SelectionAlpfa);

                    SetAlpha(SuccSprite, 0);
                    SetAlpha(FailSprite, 0);
                    SetAlpha(RunsSprite, 1);

                    if (!alreadyWrote) {
                        alreadyWrote = true;
                        MissionDesc.Clear();
                        MissionDesc.SetText(MissTask.TaskDesc);
                    }
                    break;
                case Task.TaskResult.SuccessType:
                    SetAlpha(HideSprite, 0);
                    SetAlpha(HideSpriteText, 0);
                    SetAlpha(SelectSprite, 0);

                    SetAlpha(SuccSprite, 1);
                    SetAlpha(FailSprite, 0);
                    SetAlpha(RunsSprite, 0);
                    break;
                case Task.TaskResult.FailureType:
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

            Title.text = MissTask.TaskName;
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
        /// This method shows the mission description in the dialog.
        /// On the game object must also be a button script attached,
        /// which calls this method.
        /// </summary>
        public void ClickedOn() {
            if (HideSprite.color.a == 0) {
                MissionDesc.Clear();
                MissionDesc.SetText(MissTask.TaskDesc);
            } else {
                MissionDesc.Clear();
                MissionDesc.SetText("Classified.");
            }
        }
    }
}

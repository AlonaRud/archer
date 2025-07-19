using UnityEngine;
using System.Collections;
using UnityEngine.UI;


namespace ActivationGraphSystem {
    /// <summary>
    /// Writes a text delayed on a text label. Must be attached to a game object,
    /// which has also a Text component.
    /// </summary>
    public class TypeWriter : MonoBehaviour {

        [Header("Print so much letter in one second.")]
        public int WrittenTextPerSec = 150;
        [Header("Type every UpdatePriod time.")]
        public float UpdatePriod = 0.1f;
        [Header("Limit the time for one letter, that can be influenced by FallingFactor.")]
        public AudioSource SoundLetterTyping;
        public ScrollRect RefScrollView;
        public bool DoDiasable = true;
        public float FadeOutStart = 5;
        public float FadeOutTime = 5;

        private string writeThis;
        private Coroutine cr;
        private CanvasRenderer RefScrollViewCR;
        Text label;


        private void Awake() {
            label = GetComponent<Text>();
            if (label == null) {
                Debug.LogError("Attach to the TypeWriter a Text UI component.");
            }
            label.text = "";
            RefScrollViewCR = RefScrollView.GetComponent<CanvasRenderer>();
        }

        // Use this for initialization
        private void Start() {
            label.text = "";
            if (DoDiasable) {
                RefScrollViewCR.SetAlpha(0);
                SetAlpha(label, 0);
                RefScrollView.gameObject.SetActive(false);
            }
        }

        IEnumerator StartTyping(string text) {
            // End character for anymation.
            label.text += "_";
            float elapsedTime = 0;

            while (text.Length > 0) {
                elapsedTime += UpdatePriod;
                // Check how much characers we can write in elapsedTime time window.
                int currentMaxWrittenText = Mathf.RoundToInt(WrittenTextPerSec * elapsedTime);

                string writeThis = "";
                if (currentMaxWrittenText > 0) {
                    // This text has to be written at this time.
                    writeThis = text.Substring(0, Mathf.Min(currentMaxWrittenText, text.Length));
                    // Shorten the main text.
                    text = text.Remove(0, writeThis.Length);
                }

                // Wait for the calculated time.
                yield return new WaitForSeconds(UpdatePriod);

                // Don not do anything if we have nothing to write.
                // Next time the elapsedTime will be greater and we have more chance to get a writable string.
                if (currentMaxWrittenText == 0)
                    continue;

                if (SoundLetterTyping) {
                    SoundLetterTyping.Stop();
                    SoundLetterTyping.Play();
                }

                if (label.text.Length > 0)
                    label.text = label.text.Insert(label.text.Length - 1, writeThis.ToString());
                elapsedTime = 0;
            }

            // Remove the "_" from the end.
            if (label.text.Length > 0)
                label.text = label.text.Remove(label.text.Length - 1);

            if (DoDiasable) {

                yield return new WaitForSeconds(FadeOutStart);

                while (RefScrollViewCR.GetAlpha() != 0) {
                    // 5 * 51, so 5 alpha steps.
                    float alphaStep = 0.02f;
                    yield return new WaitForSeconds(FadeOutTime / (1 / alphaStep));
                    float alpha = Mathf.Max(RefScrollViewCR.GetAlpha() - alphaStep, 0);
                    RefScrollViewCR.SetAlpha(alpha);
                    SetAlpha(label, alpha);
                }

                RefScrollView.gameObject.SetActive(false);
            }

            yield break;
        }

        private void SetAlpha(Text text, float alpha) {
            Color col = text.color;
            col.a = alpha;
            text.color = col;
        }

        /// <summary>
        /// Set the text, which will be typed delayed to the Text component.
        /// </summary>
        /// <param name="text">This text will be written.</param>
        public void SetText(string text) {
            if (cr != null) {
                StopCoroutine(cr);
            }
            
            if (DoDiasable) {
                RefScrollView.gameObject.SetActive(true);
                RefScrollViewCR.SetAlpha(1);
                SetAlpha(label, 1);
            }

            cr = StartCoroutine(StartTyping(text));
        }

        /// <summary>
        /// Clears the text on the Text component.
        /// </summary>
        public void Clear() {
            label.text = "";
            if (DoDiasable) {
                RefScrollViewCR.SetAlpha(0);
                SetAlpha(label, 0);
                RefScrollView.gameObject.SetActive(false);
            }
        }
    }
}

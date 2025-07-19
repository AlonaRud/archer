using UnityEngine;
using System.Collections.Generic;
using System;

namespace ActivationGraphSystem {
    public class FlickeringLight : MonoBehaviour {

        [Header("Will be activated/deactivated for flickering.")]
        public GameObject TheObject;
        [Header("Flicker period, rolled between min and max.")]
        public float MinFlickerTime = 10;
        public float MaxFlickerTime = 25;
        [Header("After the flickering is triggered the flickering chain will be activated.")]
        public float MinFlickerChainTime = 0.2f;
        public float MaxFlickerCainTime = 0.7f;
        [Header("Amount of maximal flickering changes. One on/off pair is 2.")]
        public int FlickerChainCount = 7;
        [Range(0f, 1f)]
        [Header("Flickering chance, max. 1.")]
        public float FlickerChance = 0.3f;
        public AudioSource FlickerAudio;

        Renderer rend = null;
        [Header("These material names will adapt the flickering changes.")]
        public List<string> LampMaterials = new List<string>();
        [Header("These materials were found by name and will be used.")]
        public List<Material> lampMaterials = new List<Material>();
        // Remember the original emission color.
        Dictionary<Material, Color32> lampMaterialEmissionColors = new Dictionary<Material, Color32>();


        void Start() {
            if (TheObject == null)
                TheObject = gameObject;

            if (FlickerAudio == null)
                FlickerAudio = GetComponent<AudioSource>();

            rend = GetComponent<Renderer>();
            foreach (string matName in LampMaterials) {
                Material mat = Array.Find(rend.materials, elem => elem.name.StartsWith(matName + " (Instance)"));
                if (mat == null)
                    continue;
                lampMaterials.Add(mat);
                lampMaterialEmissionColors.Add(mat, mat.GetColor("_EmissionColor"));
            }

            objectOn(null);
        }

        void objectOn(Timer timer) {
            TheObject.SetActive(true);
            foreach (Material mat in lampMaterials) {
                mat.SetColor("_EmissionColor", lampMaterialEmissionColors[mat]);
            }
            TimerManager.Instance.Add(this, UnityEngine.Random.Range(MinFlickerTime, MaxFlickerTime), objectOff);
        }

        void objectOff(Timer timer) {
            TheObject.SetActive(false);
            foreach (Material mat in lampMaterials) {
                mat.SetColor("_EmissionColor", Color.black);
            }
            TimerManager.Instance.Add(this, UnityEngine.Random.Range(MinFlickerChainTime, MaxFlickerCainTime), 
                objectOn, FlickerChainCount, flicker);
            FlickerAudio.Play();
        }

        void flicker(Timer timer) {
            if (TheObject.activeSelf && UnityEngine.Random.value < FlickerChance) {
                TheObject.SetActive(false);
                foreach (Material mat in lampMaterials) {
                    mat.SetColor("_EmissionColor", Color.black);
                }
                FlickerAudio.Play();
            } else {
                TheObject.SetActive(true);
                foreach (Material mat in lampMaterials) {
                    mat.SetColor("_EmissionColor", lampMaterialEmissionColors[mat]);
                }
            }
        }
    }
}

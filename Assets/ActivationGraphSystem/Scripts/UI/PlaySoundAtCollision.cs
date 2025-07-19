using UnityEngine;
using System.Collections;

public class PlaySoundAtCollision : MonoBehaviour {

    public AudioSource TheAudioSource;
    public AudioClip CollisionAudioClip;


    void Awake() {
        if (TheAudioSource == null)
            TheAudioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter (Collision col) {
        if (TheAudioSource.clip != CollisionAudioClip)
            TheAudioSource.clip = CollisionAudioClip;
        TheAudioSource.Play();
    }
}

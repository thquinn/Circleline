using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioRandomizerScript : MonoBehaviour
{
    public AudioSource audioSource;

    void Start() {
        audioSource.time = audioSource.clip.length * UnityEngine.Random.value;
    }
}

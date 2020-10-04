using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class FadeScript : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public GameObject musicScript;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup.alpha = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (canvasGroup.alpha > 0) {
            canvasGroup.alpha -= .005f;
            AudioListener.volume = Mathf.Lerp(1, 0, Mathf.Pow(canvasGroup.alpha, 2));
            if (canvasGroup.alpha <= 0) {
                musicScript.SetActive(true);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class FadeScript : MonoBehaviour
{
    public CanvasGroup canvasGroup, logoCanvasGroup;
    public GameObject musicScript;

    int downtime;

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
        } else {
            downtime++;
            if (downtime > 30 && downtime < 330) {
                float t = Mathf.InverseLerp(30, 330, downtime);
                if (t > .5f) {
                    t = 1 - t;
                }
                t *= 6;
                logoCanvasGroup.alpha = t;
            }
        }
    }
}

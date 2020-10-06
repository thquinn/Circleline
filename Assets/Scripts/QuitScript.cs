using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitScript : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    int timer = 0;

    void Update() {
        if (Input.GetKey(KeyCode.Escape) && Application.platform != RuntimePlatform.WebGLPlayer) {
            timer++;
            if (timer >= 90) {
                Application.Quit();
            }
        } else {
            timer = 0;
        }
        float targetAlpha = timer / 30f;
        if (canvasGroup.alpha < targetAlpha) {
            canvasGroup.alpha = targetAlpha;
        } else {
            canvasGroup.alpha -= .1f;
        }
    }
}

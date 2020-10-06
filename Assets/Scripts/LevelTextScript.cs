using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelTextScript : MonoBehaviour
{
    public GameScript gameScript;

    public TextMeshProUGUI tmp;
    public CanvasGroup canvasGroup;

    int prevLevel;
    int timer;

    void Update() {
        if (gameScript.levelIndex != prevLevel) {
            prevLevel = gameScript.levelIndex;
            tmp.text = string.Format("puzzle {0} of {1}", prevLevel, gameScript.levelTexts.Length - 1);
            timer = 540;
        }
        timer--;
        if (timer > 0 && timer < 360) {
            float t = 1 - Mathf.Abs(180 - timer) / 180f;
            canvasGroup.alpha = Mathf.Clamp01(t * 6);
        } else {
            canvasGroup.alpha = 0;
        }
    }
}

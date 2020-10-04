using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RestartPromptScript : MonoBehaviour
{
    public GameScript gameScript;
    public TextMeshProUGUI tmp;
    public CanvasGroup canvasGroup;

    int level3Timer;

    void Update() {
        if (gameScript.levelIndex == 3) {
            level3Timer++;
        }
        if (gameScript.victoryText != "") {
            tmp.fontSize = 36;
            tmp.text = gameScript.victoryText;
        }

        bool showLevel3Timer = gameScript.levelIndex == 3 && level3Timer > 15 * 60;
        if (gameScript.lost || showLevel3Timer || gameScript.victoryText != "") {
            canvasGroup.alpha += .01f;
        } else {
            canvasGroup.alpha -= .02f;
        }
    }
}

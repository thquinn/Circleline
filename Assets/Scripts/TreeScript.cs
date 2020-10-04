using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeScript : MonoBehaviour
{
    float frequency, offset, strength;

    public void Init() {
        float offX = (Random.value - .5f) * .5f;
        float offY = (Random.value - .5f) * .5f;
        float treeTheta = Random.Range(-10f, 20f);
        transform.localRotation = Quaternion.Euler(0, treeTheta, 0);
        float leavesTheta = Random.value * 360;
        int active = Random.Range(0, 2);
        transform.GetChild((active + 1) % 2).gameObject.SetActive(false);
        transform.GetChild(active).localRotation = Quaternion.Euler(0, leavesTheta, 0);

        frequency = Random.Range(.6f, 1f);
        offset = Random.value * 2 * Mathf.PI;
        strength = Random.Range(5, 8);
        Update();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 eulers = transform.localRotation.eulerAngles;
        eulers.x = Mathf.Cos(frequency * Time.time + offset) * strength + strength;
        transform.localRotation = Quaternion.Euler(eulers);
    }
}

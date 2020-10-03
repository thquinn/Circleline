using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridScript : MonoBehaviour
{
    static int DIMENSION = 20;
    static Quaternion QUATERNION_ROTATED = Quaternion.Euler(90, 0, 90);

    public GameObject prefabLine;

    void Start() {
        for (int i = -DIMENSION; i <= DIMENSION; i++) {
            GameObject line = Instantiate(prefabLine, transform);
            line.transform.localPosition = new Vector3(i + .5f, 0, 0);
            line = Instantiate(prefabLine, transform);
            line.transform.localPosition = new Vector3(0, 0, i + .5f);
            line.transform.localRotation = QUATERNION_ROTATED;
        }
    }

    void Update() {
        
    }
}

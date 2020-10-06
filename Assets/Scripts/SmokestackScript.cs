using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokestackScript : MonoBehaviour
{
    public GameObject prefabSmoke;

    GameObject smokesObject;
    int frames;

    // Start is called before the first frame update
    void Start()
    {
        smokesObject = GameObject.Find("Smokes");
    }

    // Update is called once per frame
    void Update()
    {
        if (GameScript.speed < 0.001 || (GameScript.transitionT > 0 && GameScript.transitionT < 1)) {
            return;
        }
        frames++;
        if (frames % 80 == 0) {
            Instantiate(prefabSmoke, smokesObject.transform).transform.position = transform.position + new Vector3(0, .2f, 0);
        }
    }
}

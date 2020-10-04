using Assets.Code;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListenerScript : MonoBehaviour
{
    static Vector3 ABOVE = new Vector3(0, 1, 0);

    public LayerMask layerMaskGround;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = Util.GetMouseCollisionPoint(layerMaskGround) + ABOVE;
    }
}

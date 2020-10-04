using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderSignScript : MonoBehaviour
{
    static float UP = 3;
    static float T = .033f;

    public SpriteRenderer spriteRenderer;
    public MeshRenderer meshRenderer;

    Vector3 originalPosition;

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = transform.localPosition;
        transform.localPosition = new Vector3(originalPosition.x, originalPosition.y + UP, originalPosition.z);
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, T);
        Color c = new Color(1, 1, 1, 1 - transform.localPosition.y / UP);
        spriteRenderer.color = c;
        meshRenderer.material.color = c;
    }
}

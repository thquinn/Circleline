using Assets.Code;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeScript : MonoBehaviour
{
    static Vector3 SHADOW_DIRECTION = new Vector3(2, -1, -2);

    public MeshRenderer meshRenderer;
    public SpriteRenderer shadowRenderer;
    public LayerMask layerMaskGround;

    int frames;
    float origScale;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer.material = Instantiate(meshRenderer.material);
        origScale = transform.localScale.x;
    }

    // Update is called once per frame
    void Update()
    {
        frames++;
        float vt = Mathf.Min(1, frames / 120f);
        float v = EasingFunction.EaseOutQuad(.02f, .01f, vt);
        float vz = frames / 4800f;
        transform.Translate(0, v, vz);
        float fadeFactor = transform.position.y < 5 ? 1 : 6 - transform.position.y;
        if (fadeFactor <= 0) {
            Destroy(gameObject);
            return;
        }
        float vs = Mathf.Min(1, frames / 240f);
        float scale = EasingFunction.EaseOutQuad(.1f, origScale, vs);
        transform.localScale = new Vector3(scale, scale, scale);

        Color c = meshRenderer.material.color;
        float at = Mathf.Min(1, frames / 15f);
        c.a = EasingFunction.EaseOutQuad(0, .1f, at) * fadeFactor;
        meshRenderer.material.color = c;

        // Shadow.
        Ray ray = new Ray(transform.position, SHADOW_DIRECTION);
        RaycastHit hit;
        Physics.Raycast(ray, out hit, 1000000, layerMaskGround);
        shadowRenderer.transform.position = hit.point + new Vector3(0, .011f, 0);
        c = shadowRenderer.color;
        c.a = meshRenderer.material.color.a / 2;
        if (transform.localPosition.y > 1) {
            c.a -= (transform.localPosition.y - 1) * .01f * fadeFactor;
        }
        shadowRenderer.color = c;
    }
}

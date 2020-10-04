using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraScript : MonoBehaviour
{
    public Camera cam;
    public Material material;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        var temporaryTexture = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, temporaryTexture, material, 0);
        Graphics.Blit(temporaryTexture, destination, material, 1);
        RenderTexture.ReleaseTemporary(temporaryTexture);
    }
}

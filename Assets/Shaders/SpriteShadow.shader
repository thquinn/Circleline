Shader "Custom/SpriteShadow" {
    Properties {
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
    _Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.5
    }
        SubShader {
        Tags 
    { 
        "Queue"="Geometry"
        "RenderType"="TransparentCutout"
    }
        LOD 200

        Cull Off

        CGPROGRAM
        // Lambert lighting model, and enable shadows on all light types
#pragma surface surf Lambert vertex:vert addshadow fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

        sampler2D _MainTex;
    fixed _Cutoff;

    struct Input
    {
        float2 uv_MainTex;
        fixed4 color;
    };

    void vert (inout appdata_full v, out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input, o);
        o.color = v.color;
    }

    void surf (Input IN, inout SurfaceOutput o) {
        fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * IN.color;
        o.Albedo = c.rgb;
        o.Alpha = c.a;
        clip(o.Alpha - _Cutoff);
    }
    ENDCG
    }
        FallBack "Diffuse"
}
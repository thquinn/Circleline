Shader "Custom/Grass"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _SparsenessTex ("Sparseness (RGB)", 2D) = "white" {}
        _WindTex ("Wind (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex, _SparsenessTex, _WindTex;

        struct Input
        {
            float2 uv_MainTex, uv_SparsenessTex, uv_WindTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float2 rotateUV(float2 uv, float rotation)
        {
            float mid = 0.5;
            return float2(
                cos(rotation) * (uv.x - mid) + sin(rotation) * (uv.y - mid) + mid,
                cos(rotation) * (uv.y - mid) - sin(rotation) * (uv.x - mid) + mid
            );
        }
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 sparsenessUV = rotateUV(IN.uv_SparsenessTex, 20);
            float sparseness = tex2D (_SparsenessTex, sparsenessUV);
            sparseness = lerp(.7, .9, sparseness);
            float2 mainUV = IN.uv_MainTex;
            float2 windUV = IN.uv_WindTex;
            windUV.x += _Time.y * .005;
            windUV.y -= _Time.y * .015;
            float wind = tex2D (_WindTex, windUV);
            mainUV.xy += wind * .066;
            fixed4 texCol = lerp(tex2D (_MainTex, mainUV), fixed4(1, 1, 1, 1), sparseness);
            texCol = lerp(texCol, fixed4(1, 1, 1, 1), wind * 3);
            fixed4 c = texCol * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

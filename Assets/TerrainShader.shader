Shader "Custom/TerrainShader"
{
    Properties
    {
        grassTexture("Texture", 2D) = "white"{}
        grassScale("Scale", Float) = 1
        rockTexture("Texture", 2D) = "white"{}
        rockScale("Scale", Float) = 1
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

        float PI = 3.1415926;
        sampler2D grassTexture;
        float grassScale;
        sampler2D rockTexture;
        float rockScale;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float angleBetween(float3 v1, float3 v2) {
            return acos(dot(normalize(v1), normalize(v2)));
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float heightPercentage = saturate(IN.worldPos.y / 100);
            float3 up = float3(0, 1, 0);
            float angle = angleBetween(up, IN.worldNormal);
            float percent = saturate(1.6 * angle - 0.1);
            o.Albedo = percent * tex2D(rockTexture, IN.worldPos.xz / rockScale)
                + (1 - percent) * tex2D(grassTexture, IN.worldPos.xz / grassScale);
        }
        ENDCG
    }
    FallBack "Diffuse"
}

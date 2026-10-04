Shader "Drift/SoftParticles"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 position : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.position = TransformObjectToHClip(v.position.xyz);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            half4 frag(Output i) : SV_Target
            {
                float radius = length(i.uv * 2.0 - 1.0);
                float alpha = saturate(1.0 - radius);
                return half4(i.color.rgb, i.color.a * alpha * alpha);
            }
            ENDHLSL
        }
    }
}

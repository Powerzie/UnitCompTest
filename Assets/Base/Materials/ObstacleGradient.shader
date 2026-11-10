Shader "Base/ObstacleGradient"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (0.75, 0.28, 0.08, 1)
        _TopColor ("Top Color", Color) = (1, 0.86, 0.35, 1)
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ObstacleGradient"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BottomColor;
                float4 _TopColor;
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float height : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.height = saturate(input.positionOS.y * 0.5 + 0.5);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = lerp(_BottomColor.rgb, _TopColor.rgb, input.height) * _BaseColor.rgb;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}

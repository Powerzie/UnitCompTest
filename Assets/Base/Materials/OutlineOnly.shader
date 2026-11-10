Shader "Custom/OutlineOnly"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Hard edges store a different normal on each face, so extruding
                // along that normal splits the outline. Vertices that share a
                // corner still share a position, so the direction from the pivot
                // is the same for all of them and the shell stays closed.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 centerWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 smoothWS = positionWS - centerWS;
                float smoothLength = length(smoothWS);
                float3 faceWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                smoothWS = smoothLength > 1e-5 ? smoothWS / smoothLength : faceWS;

                // Stretch the corner so the outline stays as thick as the faces.
                float facing = max(dot(faceWS, smoothWS), 0.2);
                positionWS += smoothWS * (_OutlineWidth / facing);

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Edges on the faces facing the camera. A flat face (a cube side)
        // has one normal on every vertex, so the diagonal of a quad is the
        // long edge and is skipped. Smooth surfaces differ on every vertex
        // and are left to the silhouette pass.
        Pass
        {
            Name "FrontEdges"
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex vertFront
            #pragma geometry geom
            #pragma fragment fragFront
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct VertData
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            VertData vertFront(Attributes input)
            {
                VertData output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            void EmitEdge(float3 a, float3 b, float3 faceNormal, inout TriangleStream<VertData> stream)
            {
                float3 edge = b - a;
                float edgeLength = length(edge);
                if (edgeLength < 1e-6)
                    return;

                float3 mid = (a + b) * 0.5;
                float3 viewDir = GetCameraPositionWS() - mid;
                float viewLength = length(viewDir);
                viewDir = viewLength > 1e-5 ? viewDir / viewLength : float3(0, 0, 1);

                float3 side = cross(edge / edgeLength, viewDir);
                float sideLength = length(side);
                side = (sideLength > 1e-5 ? side / sideLength : faceNormal) * (_OutlineWidth * 0.5);
                float3 bias = faceNormal * 0.001 + viewDir * 0.001;

                float3 positions[4] = { a - side + bias, b - side + bias, a + side + bias, b + side + bias };
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    VertData output;
                    output.positionWS = positions[i];
                    output.normalWS = faceNormal;
                    output.positionCS = TransformWorldToHClip(positions[i]);
                    stream.Append(output);
                }
                stream.RestartStrip();
            }

            [maxvertexcount(12)]
            void geom(triangle VertData input[3], inout TriangleStream<VertData> stream)
            {
                float3 p0 = input[0].positionWS;
                float3 p1 = input[1].positionWS;
                float3 p2 = input[2].positionWS;
                float3 n0 = input[0].normalWS;
                float3 n1 = input[1].normalWS;
                float3 n2 = input[2].normalWS;

                float normalSpread = max(distance(n0, n1), max(distance(n1, n2), distance(n2, n0)));
                bool flatFace = normalSpread < 0.02;

                float length01 = distance(p0, p1);
                float length12 = distance(p1, p2);
                float length20 = distance(p2, p0);
                float maxLength = max(length01, max(length12, length20));
                float minLength = min(length01, min(length12, length20));
                bool skipLongest = maxLength > minLength * 1.2 + 1e-5;

                float3 faceNormal = cross(p1 - p0, p2 - p0);
                float faceLength = length(faceNormal);
                faceNormal = faceLength > 1e-6 ? faceNormal / faceLength : n0;
                if (dot(faceNormal, n0) < 0)
                    faceNormal = -faceNormal;

                if (flatFace ? !(skipLongest && length01 >= maxLength - 1e-4) : distance(n0, n1) > 0.7)
                    EmitEdge(p0, p1, faceNormal, stream);
                if (flatFace ? !(skipLongest && length12 >= maxLength - 1e-4) : distance(n1, n2) > 0.7)
                    EmitEdge(p1, p2, faceNormal, stream);
                if (flatFace ? !(skipLongest && length20 >= maxLength - 1e-4) : distance(n2, n0) > 0.7)
                    EmitEdge(p2, p0, faceNormal, stream);
            }

            float4 fragFront(VertData input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}

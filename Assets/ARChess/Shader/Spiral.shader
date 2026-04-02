Shader "Shade/Spiral"
{
    // Made with Shade Pro by Two Lives Left
    Properties
    {
        _lineColor  ("Line Color", Color) = (1.0, 0.57257229089737, 0.0, 1.0)
        _scale  ("Scale", Float) = 10.00
        _distance  ("Distance", Float) = 0.50
        _blur  ("Blur", Float) = 0.10
        _width  ("Width", Float) = 0.38
    }

    SubShader
    {

        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        Blend One One
        ZWrite On
        LOD 200

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"


            // To make the Unity shader SRP Batcher compatible, declare all
            // properties related to a Material in a a single CBUFFER block with
            // the name UnityPerMaterial.
            CBUFFER_START(UnityPerMaterial)
                uniform float4 _lineColor;
                uniform float _scale;
                uniform float _distance;
                uniform float _blur;
                uniform float _width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float4 color        : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float4 positionWSAndFogFactor   : TEXCOORD2; // xyz: positionWS, w: vertex fog factor
                float4 color        : COLOR0;
                half3  normalWS     : TEXCOORD3;
                half3 tangentWS     : TEXCOORD4;
                half3 bitangentWS   : TEXCOORD5;
    #ifdef _MAIN_LIGHT_SHADOWS
                float4 shadowCoord  : TEXCOORD6; // compute shadow coord per-vertex for the main light
    #endif
            };

            

            Varyings vert(Attributes input)
            {
                Varyings output;

                // VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
                // Our compiler will strip all unused references (say you don't use view space).
                // Therefore there is more flexibility at no additional cost with this struct.
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                // Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
                // in world space. If not used it will be stripped.
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                // Computes fog factor per-vertex.
                float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                output.positionCS = vertexInput.positionCS;
                output.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);
                output.normalWS = vertexNormalInput.normalWS;
                output.tangentWS = vertexNormalInput.tangentWS;
                output.bitangentWS = vertexNormalInput.bitangentWS;
                output.uv0 = input.uv0;
                output.uv1 = input.uv1;
                output.color = input.color;

    #ifdef _MAIN_LIGHT_SHADOWS
                // shadow coord for the main light is computed in vertex.
                // If cascades are enabled, LWRP will resolve shadows in screen space
                // and this coord will be the uv coord of the screen space shadow texture.
                // Otherwise LWRP will resolve shadows in light space (no depth pre-pass and shadow collect pass)
                // In this case shadowCoord will be the position in light space.
                output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWSAndFogFactor.xyz;
                half3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);
                half3x3 tangentToWorld = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);

                SurfaceData surfaceData;

                surfaceData.emission = float3(0.0, 0.0, 0.0);
                float localVar_L = length(((input.uv0 * float2(_scale, _scale)) + (_scale * float2(-0.5, -0.5))));
                float localVar_Angle = atan2(((input.uv0 * float2(_scale, _scale)) + (_scale * float2(-0.5, -0.5))).x, ((input.uv0 * float2(_scale, _scale)) + (_scale * float2(-0.5, -0.5))).y);
                float localVar_Offset = ((log(localVar_L) / (2.7183*5.0)) + ((localVar_Angle / (PI*2.0)) * _distance));
                float localVar_DistanceCircle = _distance;
                float localVar_Circles = fmod((localVar_Offset-_Time.y), localVar_DistanceCircle);
                float4 multiply_38 = (_lineColor * (smoothstep((localVar_Circles-_blur), localVar_Circles, _width) - smoothstep(localVar_Circles, (localVar_Circles+_blur), _width)));
                surfaceData.albedo = multiply_38.rgb;
                surfaceData.alpha = 1.0;
                

                half3 normalWS = input.normalWS;
                normalWS = normalize(normalWS);

                half3 color = surfaceData.albedo;

                // Emission
                color += surfaceData.emission;

                float fogFactor = input.positionWSAndFogFactor.w;

                // Mix the pixel color with fogColor. You can optionaly use MixFogColor to override the fogColor
                // with a custom one.
                color = MixFog(color, fogFactor);
                return half4(color, surfaceData.alpha);
            }
            ENDHLSL
        }

        // Used for rendering shadowmaps
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        // Used for depth prepass
        // If shadows cascade are enabled we need to perform a depth prepass.
        // We also need to use a depth prepass in some cases camera require depth texture
        // (e.g, MSAA is enabled and we can't resolve with Texture2DMS
        UsePass "Universal Render Pipeline/Lit/DepthOnly"

        // Used for Baking GI. This pass is stripped from build.
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}

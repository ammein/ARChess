Shader "Shade/Harry Potter Start"
{
    // Made with Shade Pro by Two Lives Left
    Properties
    {
        _edgeColor  ("Edge Color", Color) = (0.0, 0.27863919734955, 1.0, 1.0)
        _edgeGlow  ("Edge Glow", Float) = 20.00
        _edgeWidth  ("Edge Width", Float) = 0.03
        _appear  ("Appear", Range (0.00, 1.00)) = 0.00
        [NoScaleOffset] _gradient  ("Gradient", 2D) = "white" {}
        _noiseScale  ("Noise Scale", Float) = 2.00
    }

    SubShader
    {

        Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline" = "UniversalRenderPipeline" }
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
                uniform float4 _edgeColor;
                uniform float _edgeGlow;
                uniform float _edgeWidth;
                uniform float _appear;
                uniform sampler2D _gradient;
                uniform float _noiseScale;
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

            float remap(float value, float minA, float maxA, float minB, float maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            float2 remap(float2 value, float2 minA, float2 maxA, float2 minB, float2 maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            float3 remap(float3 value, float3 minA, float3 maxA, float3 minB, float3 maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            float4 remap(float4 value, float4 minA, float4 maxA, float4 minB, float4 maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            float2 remap(float2 value, float minA, float maxA, float minB, float maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            float3 remap(float3 value, float minA, float maxA, float minB, float maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            float4 remap(float4 value, float minA, float maxA, float minB, float maxB)
            {
                return minB + (value - minA) * (maxB - minB) / (maxA - minA);
            }
            
            
            float mod289(float x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float4 mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            
            float permute(float x) { return mod289(((x*34.0)+1.0)*x); }
            float2 permute(float2 x) { return mod289(((x*34.0)+1.0)*x); }
            float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
            float4 permute(float4 x) { return mod289(((x*34.0)+1.0)*x); }
            
            float taylorInvSqrt(float r) { return 1.79284291400159 - 0.85373472095314 * r; }
            float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }
            
            float2 fade(float2 t) { return t*t*t*(t*(t*6.0-15.0)+10.0); }
            float3 fade(float3 t) { return t*t*t*(t*(t*6.0-15.0)+10.0); }
            float4 fade(float4 t) { return t*t*t*(t*(t*6.0-15.0)+10.0); }
            
            //
            // Description : Array and tex2Dless GLSL 2D/3D/4D simplex
            //               noise functions.
            //      Author : Ian McEwan, Ashima Arts.
            //  Maintainer : ijm
            //     Lastmod : 20110822 (ijm)
            //     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
            //               Distributed under the MIT License. See LICENSE file.
            //               https://github.com/ashima/webgl-noise
            //
            
            float snoise3D(float3 v)
            {
                const float2  C = float2(1.0/6.0, 1.0/3.0) ;
                const float4  D = float4(0.0, 0.5, 1.0, 2.0);
            
                // First corner
                float3 i  = floor(v + dot(v, C.yyy) );
                float3 x0 =   v - i + dot(i, C.xxx) ;
            
                // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0 - g;
                float3 i1 = min( g.xyz, l.zxy );
                float3 i2 = max( g.xyz, l.zxy );
            
                //   x0 = x0 - 0.0 + 0.0 * C.xxx;
                //   x1 = x0 - i1  + 1.0 * C.xxx;
                //   x2 = x0 - i2  + 2.0 * C.xxx;
                //   x3 = x0 - 1.0 + 3.0 * C.xxx;
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
                float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y
            
                // Permutations
                i = mod289(i);
                float4 p = permute( permute( permute(
                         i.z + float4(0.0, i1.z, i2.z, 1.0 ))
                       + i.y + float4(0.0, i1.y, i2.y, 1.0 ))
                       + i.x + float4(0.0, i1.x, i2.x, 1.0 ));
            
                // Gradients: 7x7 points over a square, mapped onto an octahedron.
                // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
                float n_ = 0.142857142857; // 1.0/7.0
                float3  ns = n_ * D.wyz - D.xzx;
            
                float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  ffmod(p,7*7)
            
                float4 x_ = floor(j * ns.z);
                float4 y_ = floor(j - 7.0 * x_ );    // ffmod(j,N)
            
                float4 x = x_ *ns.x + ns.yyyy;
                float4 y = y_ *ns.x + ns.yyyy;
                float4 h = 1.0 - abs(x) - abs(y);
            
                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);
            
                //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
                //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
                float4 s0 = floor(b0)*2.0 + 1.0;
                float4 s1 = floor(b1)*2.0 + 1.0;
                float4 sh = -step(h, float4(0.0, 0.0, 0.0, 0.0));
            
                float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
                float4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;
            
                float3 p0 = float3(a0.xy, h.x);
                float3 p1 = float3(a0.zw, h.y);
                float3 p2 = float3(a1.xy, h.z);
                float3 p3 = float3(a1.zw, h.w);
            
                //Normalise gradients
                float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
                p0 *= norm.x;
                p1 *= norm.y;
                p2 *= norm.z;
                p3 *= norm.w;
            
                // Mix final noise value
                float4 m = max(0.6 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
                m = m * m;
                return 42.0 * dot( m*m, float4( dot(p0,x0), dot(p1,x1),
                                   dot(p2,x2), dot(p3,x3) ) );
            }
            
            float turb_snoise3D (in float3 st, in int octaves, in float lacunarity, in float gain)
            {
                // Initial values
                float value = 0.0;
                float amplitude = .5;
                float frequency = 0.;
                //
                // Loop of octaves
                for (int i = 0; i < octaves; i++) {
                    value += amplitude * abs(snoise3D(st));
                    st *= lacunarity;
                    amplitude *= gain;
                }
                return value;
            }
            

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

                float localVar_Progression = (1.0 - _appear);
                float3 remap_12 = remap(input.positionWSAndFogFactor.xyz, float2(-5.0312, 2.125).x, float2(-5.0312, 2.125).y, float2(0.0, 1.0).x, float2(0.0, 1.0).y);
                float3 oneminus_16 = (1.0 - tex2Dlod(_gradient, float4(float2(remap_12.g, 0.0), 0.0, 0.0)).rgb);
                float temp_14 = turb_snoise3D((input.positionWSAndFogFactor.xyz*float3(_noiseScale, _noiseScale, _noiseScale)), 2, 3.000000, 0.500000);
                float multiply_19 = (oneminus_16.r * remap(temp_14, float2(-1.0, 1.0).x, float2(-1.0, 1.0).y, float2(0.0, 1.0).x, float2(0.0, 1.0).y));
                surfaceData.emission = ((_edgeColor.rgb*float3(_edgeGlow, _edgeGlow, _edgeGlow)) * (1.0 - step((_edgeWidth+localVar_Progression), float3(multiply_19, multiply_19, multiply_19))));
                surfaceData.albedo = float3(0.5, 0.5, 0.5);
                
                surfaceData.smoothness = 1.0 - 0.32237;
                surfaceData.metallic = 1.0;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = multiply_19;
                

                half3 normalWS = input.normalWS;
                normalWS = normalize(normalWS);

    #ifdef LIGHTMAP_ON
                // Normal is required in case Directional lightmaps are baked
                half3 bakedGI = SampleLightmap(input.uvLM, normalWS);
    #else
                // Samples SH fully per-pixel. SampleSHVertex and SampleSHPixel functions
                // are also defined in case you want to sample some terms per-vertex.
                half3 bakedGI = SampleSH(normalWS);
    #endif

                // BRDFData holds energy conserving diffuse and specular material reflections and its roughness.
                // It's easy to plugin your own shading fuction. You just need replace LightingPhysicallyBased function
                // below with your own.
                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                // Light struct is provide by LWRP to abstract light shader variables.
                // It contains light direction, color, distanceAttenuation and shadowAttenuation.
                // LWRP take different shading approaches depending on light and platform.
                // You should never reference light shader variables in your shader, instead use the GetLight
                // funcitons to fill this Light struct.
    #ifdef _MAIN_LIGHT_SHADOWS
                // Main light is the brightest directional light.
                // It is shaded outside the light loop and it has a specific set of variables and shading path
                // so we can be as fast as possible in the case when there's only a single directional light
                // You can pass optionally a shadowCoord (computed per-vertex). If so, shadowAttenuation will be
                // computed.
                Light mainLight = GetMainLight(input.shadowCoord);
    #else
                Light mainLight = GetMainLight();
    #endif

                // Mix diffuse GI with environment reflections.
                half3 color = GlobalIllumination(brdfData, bakedGI, surfaceData.occlusion, normalWS, viewDirectionWS);

                // LightingPhysicallyBased computes direct light contribution.
                color += LightingPhysicallyBased(brdfData, mainLight, normalWS, viewDirectionWS);

                // Additional lights loop
    #ifdef _ADDITIONAL_LIGHTS

                // Returns the amount of lights affecting the object being renderer.
                // These lights are culled per-object in the forward renderer
                int additionalLightsCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightsCount; ++i)
                {
                    // Similar to GetMainLight, but it takes a for-loop index. This figures out the
                    // per-object light index and samples the light buffer accordingly to initialized the
                    // Light struct. If _ADDITIONAL_LIGHT_SHADOWS is defined it will also compute shadows.
                    Light light = GetAdditionalLight(i, positionWS);

                    // Same functions used to shade the main light.
                    color += LightingPhysicallyBased(brdfData, light, normalWS, viewDirectionWS);
                }
    #endif

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
